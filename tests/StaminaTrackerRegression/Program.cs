using System;
using AntiCheat.Utils;

static class Program
{
    static int count, failures;
    static void Assert(bool value, string message) { if (!value) throw new Exception(message); }
    static void Run(string name, Action action)
    {
        count++;
        try { action(); Console.WriteLine("PASS " + name); }
        catch (Exception exception) { failures++; Console.WriteLine("FAIL " + name + ": " + exception.Message); }
    }

    sealed class Scenario
    {
        internal readonly SprintSuspicionTracker Tracker = new();
        internal SprintObservation Sample = new() { SprintTime = 5, MovementSpeed = 2, Eligible = true, PositionFresh = true };
        internal int Alerts;
        internal double LastAlert;
        internal Scenario() { Send(); }
        internal bool Send()
        {
            bool alert = Tracker.Observe(Sample);
            if (alert) { Alerts++; LastAlert = Sample.Time; }
            return alert;
        }
        internal bool Tick(bool sprint, double dt = .25, double dx = 0, double dy = 0, double dz = 0, bool fresh = true, bool eligible = true)
        {
            Sample.Time += dt; Sample.X += dx; Sample.Y += dy; Sample.Z += dz;
            Sample.Sprinting = sprint; Sample.PositionFresh = fresh; Sample.Eligible = eligible;
            return Send();
        }
        internal void Idle(double seconds, bool fresh = false)
        {
            for (double t = 0; t < seconds; t += .25) Tick(false, fresh: fresh);
        }
        internal void Warmup() => Idle(2);
        internal void Sprint(double duration = 11, double speed = 4.5)
        {
            Tick(true); // The first sprint sample is a baseline, not a measured interval.
            for (double t = 0; t < duration; t += .25) Tick(true, dx: speed * .25);
        }
        internal void Finish(double rest = 2)
        {
            Tick(false, fresh: false);
            Idle(rest);
        }
        internal void Episode(double duration = 11, double speed = 4.5) { Sprint(duration, speed); Finish(); }
        internal void TwoCandidates() { Warmup(); Episode(); Episode(); Assert(Alerts == 0, "alert before third independent bout"); }
        internal void VerifyReset()
        {
            Idle(2.25); Episode();
            Assert(Alerts == 0, "uncertainty retained previous candidates");
            Episode(); Episode();
            Assert(Alerts == 1, "new clean evidence cannot recover after reset");
        }
    }

    static int Main()
    {
        Run("three separate prolonged bouts notify once after rest", () => {
            var s = new Scenario(); s.TwoCandidates(); s.Sprint();
            Assert(s.Alerts == 0, "alert during unconfirmed bout");
            s.Finish(1.75); Assert(s.Alerts == 0, "alert before two seconds rest");
            s.Idle(.25); Assert(s.Alerts == 1, "third confirmed bout did not notify");
        });
        Run("ordinary native duration remains silent", () => {
            var s = new Scenario(); s.Warmup();
            for (int i = 0; i < 12; i++) s.Episode(4.5);
            Assert(s.Alerts == 0, "normal sprint counted as suspicious");
        });
        Run("one endless sprint cannot become three incidents", () => {
            var s = new Scenario(); s.Warmup(); s.Sprint(180);
            Assert(s.Alerts == 0, "single bout repeatedly counted");
            s.Finish(); Assert(s.Alerts == 0, "one confirmed incident notified");
            s.Episode(); Assert(s.Alerts == 0, "second incident notified");
            s.Episode(); Assert(s.Alerts == 1, "third independent incident missing");
        });
        Run("startup sprint requires clean rest before evidence", () => {
            var s = new Scenario(); s.Sprint(); s.Finish(); s.Episode(); s.Episode();
            Assert(s.Alerts == 0, "unarmed startup bout counted");
            s.Episode(); Assert(s.Alerts == 1, "safe rest failed to arm");
        });
        Run("duration alone cannot trigger suspicion", () => {
            var s = new Scenario(); s.Warmup(); for (int i = 0; i < 4; i++) s.Episode(11, 4);
            Assert(s.Alerts == 0, "insufficient distance counted");
        });
        Run("distance alone cannot trigger suspicion", () => {
            var s = new Scenario(); s.Warmup(); for (int i = 0; i < 4; i++) s.Episode(6, 9);
            Assert(s.Alerts == 0, "insufficient duration counted");
        });
        Run("duration threshold is strict", () => {
            var s = new Scenario(); s.Warmup(); for (int i = 0; i < 4; i++) s.Episode(10, 5);
            Assert(s.Alerts == 0, "exact duration threshold counted");
        });
        Run("distance threshold is strict", () => {
            var s = new Scenario(); s.Warmup(); for (int i = 0; i < 4; i++) s.Episode(12, 3.75);
            Assert(s.Alerts == 0, "exact distance threshold counted");
        });
        Run("just above both thresholds counts", () => {
            var s = new Scenario(); s.Warmup(); for (int i = 0; i < 3; i++) s.Episode(10.25, 4.5);
            Assert(s.Alerts == 1, "valid boundary episode lost");
        });
        Run("runtime stamina duration changes the safe budget", () => {
            var s = new Scenario(); s.Sample.SprintTime = 10; s.Tick(false); s.Idle(2.25);
            for (int i = 0; i < 4; i++) s.Episode(11, 4.5);
            Assert(s.Alerts == 0, "hardcoded duration caused accusation");
            for (int i = 0; i < 3; i++) s.Episode(21, 4.5);
            Assert(s.Alerts == 1, "runtime duration not supported");
        });
        Run("runtime movement speed changes the distance budget", () => {
            var s = new Scenario(); s.Sample.MovementSpeed = 4; s.Tick(false); s.Idle(2.25);
            for (int i = 0; i < 4; i++) s.Episode(11, 4.5);
            Assert(s.Alerts == 0, "hardcoded movement speed caused accusation");
            for (int i = 0; i < 3; i++) s.Episode(11, 9);
            Assert(s.Alerts == 1, "runtime movement speed not supported");
        });
        Run("stale idle positions preserve independent candidates", () => {
            var s = new Scenario(); s.TwoCandidates(); s.Idle(10, fresh: false); s.Episode();
            Assert(s.Alerts == 1, "normal stationary rest erased candidates");
        });
        Run("sprinting with stale positions resets all evidence", () => {
            var s = new Scenario(); s.TwoCandidates(); s.Tick(true, fresh: false); s.VerifyReset();
        });
        Run("ineligible state discards current and prior evidence", () => {
            var s = new Scenario(); s.TwoCandidates(); s.Sprint(); s.Tick(false, eligible: false); s.VerifyReset();
        });
        Run("late exemption cancels a candidate during confirmation rest", () => {
            var s = new Scenario(); s.TwoCandidates(); s.Sprint(); s.Finish(1);
            s.Tick(false, eligible: false); s.VerifyReset();
        });
        Run("short stop cannot confirm or arm the next episode", () => {
            var s = new Scenario(); s.Warmup(); s.Sprint(); s.Finish(1);
            s.Sprint(); s.Finish(); s.Episode(); s.Episode();
            Assert(s.Alerts == 0, "short rest created extra candidates");
            s.Episode(); Assert(s.Alerts == 1, "subsequent clean bouts cannot recover");
        });
        Run("sampling gap discards evidence", () => {
            var s = new Scenario(); s.TwoCandidates(); s.Tick(false, dt: 1); s.VerifyReset();
        });
        Run("maximum permitted sample interval is inclusive", () => {
            var s = new Scenario(); s.TwoCandidates(); s.Tick(false, dt: .75); s.Episode();
            Assert(s.Alerts == 1, "valid .75 second interval reset evidence");
        });
        foreach (double dt in new[] { 0d, -.25d })
            Run("nonmonotonic observation resets evidence: " + dt, () => {
                var s = new Scenario(); s.TwoCandidates(); s.Tick(false, dt); s.VerifyReset();
            });
        Run("changed movement parameters reset evidence", () => {
            var s = new Scenario(); s.TwoCandidates(); s.Sample.SprintTime = 6; s.Tick(false);
            s.Sample.SprintTime = 5; s.Tick(false); s.VerifyReset();
        });
        Run("teleport displacement resets evidence", () => {
            var s = new Scenario(); s.TwoCandidates(); s.Tick(false, dx: 100); s.VerifyReset();
        });
        Run("large vertical displacement resets evidence", () => {
            var s = new Scenario(); s.TwoCandidates(); s.Tick(false, dy: 3); s.VerifyReset();
        });
        Run("horizontal and vertical step limits are inclusive", () => {
            var s = new Scenario(); s.TwoCandidates(); s.Tick(false, dx: 5, dy: 2); s.Episode();
            Assert(s.Alerts == 1, "permitted displacement boundary erased candidates");
        });
        Run("runtime speed changes discard prior candidates", () => {
            var s = new Scenario(); s.TwoCandidates(); s.Sample.MovementSpeed = 3; s.Tick(false);
            s.Sample.MovementSpeed = 2; s.Tick(false); s.VerifyReset();
        });
        Run("horizontal distance includes both axes", () => {
            var s = new Scenario(); s.Warmup();
            for (int j = 0; j < 3; j++) {
                s.Tick(true);
                for (int i = 0; i < 44; i++) s.Tick(true, dx: .75, dz: 1);
                s.Finish();
            }
            Assert(s.Alerts == 1, "horizontal path discarded Z displacement");
        });
        Run("vertical travel is not sprint distance", () => {
            var s = new Scenario(); s.Warmup();
            for (int j = 0; j < 3; j++) {
                s.Tick(true);
                for (int i = 0; i < 44; i++) s.Tick(true, dx: .25, dy: i % 2 == 0 ? 2 : -2);
                s.Finish();
            }
            Assert(s.Alerts == 0, "vertical travel inflated sprint evidence");
        });
        Run("idle movement is not added to the next sprint", () => {
            var s = new Scenario(); s.Warmup();
            for (int j = 0; j < 3; j++) {
                for (int i = 0; i < 60; i++) s.Tick(false, dx: 2);
                s.Episode(11, 1);
            }
            Assert(s.Alerts == 0, "idle distance counted toward sprint");
        });
        Run("first sprint displacement only establishes a baseline", () => {
            var s = new Scenario(); s.Warmup();
            for (int j = 0; j < 3; j++) {
                s.Tick(true, dx: 5);
                for (int i = 0; i < 44; i++) s.Tick(true, dx: 1);
                s.Finish();
            }
            Assert(s.Alerts == 0, "unobserved run start inflated distance");
        });
        Run("candidate expiry prevents ancient evidence aggregation", () => {
            var s = new Scenario(); s.Warmup(); s.Episode(); s.Idle(301); s.Episode(); s.Episode();
            Assert(s.Alerts == 0, "expired candidate retained");
            s.Episode(); Assert(s.Alerts == 1, "fresh three candidates missing");
        });
        Run("evidence window includes exactly 300 seconds", () => {
            var s = new Scenario(); s.Warmup(); s.Episode(); double firstConfirmed = s.Sample.Time;
            s.Episode(); s.Idle(firstConfirmed + 300 - s.Sample.Time - 13.5); s.Episode();
            Assert(s.Sample.Time == firstConfirmed + 300, "fixture missed expiry boundary");
            Assert(s.Alerts == 1, "exactly 300 second old candidate expired early");
        });
        Run("evidence window excludes a candidate just past 300 seconds", () => {
            var s = new Scenario(); s.Warmup(); s.Episode(); double firstConfirmed = s.Sample.Time;
            s.Episode(); s.Idle(firstConfirmed + 300.25 - s.Sample.Time - 13.5); s.Episode();
            Assert(s.Alerts == 0, "expired candidate survived boundary");
            // Finish before the second candidate also crosses its 300 second boundary.
            s.Episode(10.25); Assert(s.Alerts == 1, "remaining recent evidence was discarded");
        });
        Run("cooldown ignores evidence and then needs three new bouts", () => {
            var s = new Scenario(); s.Warmup(); s.Episode(); s.Episode(); s.Episode();
            Assert(s.Alerts == 1, "first alert missing");
            s.Idle(2.25); for (int i = 0; i < 3; i++) s.Episode();
            Assert(s.Alerts == 1, "alert during cooldown");
            double end = s.LastAlert + 120;
            while (s.Sample.Time < end + 2.25) s.Tick(false, fresh: false);
            s.Episode(); s.Episode(); Assert(s.Alerts == 1, "cooldown evidence reused");
            s.Episode(); Assert(s.Alerts == 2, "three fresh bouts after cooldown missing");
        });
        Run("explicit evidence reset retains alert cooldown", () => {
            var s = new Scenario(); s.Warmup(); s.Episode(); s.Episode(); s.Episode();
            s.Tracker.ResetEvidence(); s.Idle(2.25); s.Episode(); s.Episode(); s.Episode();
            Assert(s.Alerts == 1, "manual reset bypassed cooldown");
        });
        foreach (var mutate in new Action<Scenario>[] {
            s => s.Sample.Time = double.NaN,
            s => s.Sample.X = double.PositiveInfinity,
            s => s.Sample.Y = double.NaN,
            s => s.Sample.Z = double.NegativeInfinity,
            s => s.Sample.SprintTime = 0,
            s => s.Sample.SprintTime = double.NaN,
            s => s.Sample.MovementSpeed = -1,
            s => s.Sample.MovementSpeed = double.PositiveInfinity,
            s => s.Sample.SprintTime = double.MaxValue,
        })
        {
            int index = count;
            Run("invalid numeric observation clears evidence " + index, () => {
                var s = new Scenario(); s.TwoCandidates(); var valid = s.Sample;
                s.Sample.Time += .25;
                mutate(s); Assert(!s.Send(), "invalid data notified");
                s.Sample = valid; s.Tick(false); s.VerifyReset();
            });
        }
        Console.WriteLine($"Tests: {count}; failures: {failures}");
        return failures == 0 ? 0 : 1;
    }
}
