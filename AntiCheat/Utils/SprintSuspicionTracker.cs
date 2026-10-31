using System;
using System.Collections.Generic;

namespace AntiCheat.Utils
{
    internal struct SprintObservation
    {
        internal double Time, X, Y, Z, SprintTime, MovementSpeed;
        internal bool Eligible, Sprinting, PositionFresh;
    }

    internal sealed class SprintSuspicionTracker
    {
        private const double RestSeconds = 2;
        private const double MaximumSampleGap = 0.75;
        private const double EvidenceSeconds = 300;
        private const double NotificationCooldown = 120;
        private const double SprintMultiplier = 2.25;
        private readonly Queue<double> confirmedEpisodes = new Queue<double>();
        private SprintObservation previous;
        private bool hasPrevious, armed, running, pendingCandidate;
        private double restStarted = double.NaN;
        private double sprintStarted, sprintDistance;
        private double nextNotification = double.NegativeInfinity;

        // The caller excludes game-specific uncertainty (damage, TZP, parent changes, etc.).
        // A true result is only a host-visible suspicion, never proof of a modified client.
        internal bool Observe(SprintObservation sample)
        {
            double durationBudget = sample.SprintTime * 2;
            double distanceBudget = durationBudget * sample.MovementSpeed * SprintMultiplier;
            if (!sample.Eligible || !Finite(sample.Time) || !Finite(sample.X) ||
                !Finite(sample.Y) || !Finite(sample.Z) || !Finite(sample.SprintTime) ||
                !Finite(sample.MovementSpeed) || sample.SprintTime <= 0 ||
                sample.MovementSpeed <= 0 || !Finite(durationBudget) || !Finite(distanceBudget) ||
                (sample.Sprinting && !sample.PositionFresh) || sample.Time < nextNotification)
            {
                ResetEvidence();
                return false;
            }

            if (!hasPrevious)
            {
                previous = sample;
                hasPrevious = true;
                if (!sample.Sprinting) restStarted = sample.Time;
                return false;
            }

            double elapsed = sample.Time - previous.Time;
            double dx = sample.X - previous.X;
            double dz = sample.Z - previous.Z;
            double distance = Math.Sqrt(dx * dx + dz * dz);
            double vertical = Math.Abs(sample.Y - previous.Y);
            double expectedStep = sample.MovementSpeed * SprintMultiplier * elapsed;
            if (!Finite(elapsed) || elapsed <= 0 || elapsed > MaximumSampleGap ||
                sample.SprintTime != previous.SprintTime || sample.MovementSpeed != previous.MovementSpeed ||
                !Finite(distance) || !Finite(vertical) || !Finite(expectedStep) ||
                distance > Math.Max(5, expectedStep * 3) || vertical > Math.Max(2, expectedStep))
            {
                ResetEvidence();
                return false;
            }
            previous = sample;

            while (confirmedEpisodes.Count > 0 && sample.Time - confirmedEpisodes.Peek() > EvidenceSeconds)
                confirmedEpisodes.Dequeue();

            if (!sample.Sprinting)
            {
                if (running)
                {
                    running = false;
                    armed = false;
                    restStarted = sample.Time;
                }
                if (double.IsNaN(restStarted)) restStarted = sample.Time;
                if (sample.Time - restStarted < RestSeconds) return false;

                armed = true;
                if (!pendingCandidate) return false;
                // Waiting for rest also lets late damage/TZP observations cancel the candidate.
                pendingCandidate = false;
                confirmedEpisodes.Enqueue(sample.Time);
                if (confirmedEpisodes.Count < 3) return false;

                nextNotification = sample.Time + NotificationCooldown;
                ResetEvidence();
                return true;
            }

            restStarted = double.NaN;
            if (!armed)
            {
                // A brief stop neither confirms a candidate nor starts an independent episode.
                pendingCandidate = false;
                return false;
            }
            if (!running)
            {
                running = true;
                sprintStarted = sample.Time;
                sprintDistance = 0;
                return false; // Ignore movement before the first observed sprint sample.
            }

            sprintDistance += distance;
            if (!Finite(sprintDistance))
            {
                ResetEvidence();
                return false;
            }
            if (sample.Time - sprintStarted > durationBudget && sprintDistance > distanceBudget)
                pendingCandidate = true;
            return false;
        }

        internal void ResetEvidence()
        {
            confirmedEpisodes.Clear();
            hasPrevious = armed = running = pendingCandidate = false;
            restStarted = double.NaN;
            sprintStarted = sprintDistance = 0;
            // Exemptions must not let a player bypass the notification cooldown.
        }

        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
