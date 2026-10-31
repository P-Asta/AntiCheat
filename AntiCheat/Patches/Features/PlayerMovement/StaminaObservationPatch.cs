using GameNetcodeStuff;
using HarmonyLib;
using AntiCheat.Utils;
using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Netcode;
using UnityEngine;

namespace AntiCheat.Patches
{
    [HarmonyPatch(typeof(PlayerControllerB), "LateUpdate")]
    [HarmonyWrapSafe]
    internal static class StaminaObservationPatch
    {
        private sealed class ObservationState
        {
            internal readonly SprintSuspicionTracker Tracker = new SprintSuspicionTracker();
            internal ulong Owner;
            internal SelectableLevel Level;
            internal Transform Parent;
            internal float DamageTime;
            internal bool TeleportFlags;
            internal double LastFrame, NextSample, GraceUntil;
        }

        private static readonly Dictionary<PlayerControllerB, ObservationState> States = new Dictionary<PlayerControllerB, ObservationState>();
        private static StartOfRound observedRound;

        [HarmonyPostfix]
        internal static void LateUpdate(PlayerControllerB __instance)
        {
            var round = StartOfRound.Instance;
            var network = NetworkManager.Singleton;
            if (PluginConfig.StaminaObservation == null || !PluginConfig.StaminaObservation.Value ||
                round == null || !round.IsHost || network == null || !network.IsHost || !network.IsListening)
            {
                Clear();
                return;
            }
            if (!ReferenceEquals(observedRound, round))
            {
                Clear();
                observedRound = round;
            }
            if (!round.shipHasLanded || round.shipIsLeaving || round.inShipPhase || round.newGameIsLoading ||
                round.suckingPlayersOutOfShip || round.currentLevel == null || round.timeSinceRoundStarted < 10f)
            {
                States.Clear();
                return;
            }
            // Other player objects also run LateUpdate. They must not erase a remote player's evidence.
            if (!IsRemotePlayer(__instance, round))
            {
                Forget(__instance);
                return;
            }

            double now = Time.time;
            if (!Finite(now))
            {
                Forget(__instance);
                return;
            }
            if (!States.TryGetValue(__instance, out var state) || state.Owner != __instance.OwnerClientId ||
                !ReferenceEquals(state.Level, round.currentLevel))
            {
                state = new ObservationState
                {
                    Owner = __instance.OwnerClientId,
                    Level = round.currentLevel,
                    Parent = __instance.transform.parent,
                    DamageTime = __instance.timeSinceTakingDamage,
                    TeleportFlags = __instance.teleportingThisFrame || __instance.teleportedLastFrame,
                    LastFrame = now,
                    NextSample = now,
                    GraceUntil = now + GraceDuration(__instance)
                };
                States[__instance] = state;
            }

            bool interrupted = now < state.LastFrame || now - state.LastFrame > 1d;
            state.LastFrame = now;
            bool parentChanged = !ReferenceEquals(state.Parent, __instance.transform.parent);
            state.Parent = __instance.transform.parent;
            bool damaged = state.DamageTime != __instance.timeSinceTakingDamage;
            state.DamageTime = __instance.timeSinceTakingDamage;
            bool teleportFlags = __instance.teleportingThisFrame || __instance.teleportedLastFrame;
            bool teleported = teleportFlags && !state.TeleportFlags;
            state.TeleportFlags = teleportFlags;
            // Remote teleport flags are not reliably cleared by the owner's movement loop.
            // Observe their rising edge and every TeleportPlayer call, rather than excluding forever.
            if (interrupted || parentChanged || damaged || teleported || UnsafeObservation(__instance))
            {
                ResetWithGrace(state, __instance, now);
                return;
            }
            if (now < state.GraceUntil)
            {
                state.Tracker.ResetEvidence();
                return;
            }

            var animator = __instance.playerBodyAnimator;
            // Normal animation transitions and exhaustion are rest/uncertainty, not new violations.
            bool sprinting = !animator.IsInTransition(0) && !__instance.isExhausted &&
                animator.GetCurrentAnimatorStateInfo(0).IsTag("Sprinting");
            bool fresh = !sprinting || (Finite(__instance.timeSincePlayerMoving) &&
                __instance.timeSincePlayerMoving >= 0f && __instance.timeSincePlayerMoving <= 0.75f);
            if (!fresh)
            {
                state.Tracker.ResetEvidence();
                return;
            }
            if (now < state.NextSample) return;
            state.NextSample = now + 0.25d;
            var position = __instance.serverPlayerPosition;
            if (state.Tracker.Observe(new SprintObservation
            {
                Time = now,
                X = position.x, Y = position.y, Z = position.z,
                SprintTime = __instance.sprintTime,
                MovementSpeed = __instance.movementSpeed,
                Eligible = true,
                Sprinting = sprinting,
                PositionFresh = fresh
            }))
            {
                AntiCheatPlugin.ShowStaminaSuspicion(__instance);
            }
        }

        private static bool IsRemotePlayer(PlayerControllerB player, StartOfRound round)
        {
            if (player == null || !player.IsSpawned || player.IsOwner || player.isHostPlayerObject ||
                !player.isPlayerControlled || player.isPlayerDead || player.actualClientId != player.OwnerClientId ||
                round.ClientPlayerList == null || round.allPlayerScripts == null ||
                !round.ClientPlayerList.TryGetValue(player.actualClientId, out int slot) ||
                slot < 0 || slot >= round.allPlayerScripts.Length)
                return false;
            return player.playerClientId == (ulong)slot && ReferenceEquals(round.allPlayerScripts[slot], player);
        }

        private static bool UnsafeObservation(PlayerControllerB player)
        {
            return !Finite(Time.timeScale) || Time.timeScale != 1f ||
                !Finite(Time.deltaTime) || Time.deltaTime <= 0f || Time.deltaTime > 0.5f ||
                !Finite(player.sprintTime) || player.sprintTime <= 0f ||
                !Finite(player.movementSpeed) || player.movementSpeed <= 0f ||
                !Finite(player.timeSinceTakingDamage) ||
                !Finite(player.drunkness) || !Finite(player.drunknessInertia) || !Finite(player.poison) ||
                player.health != 100 || player.criticallyInjured ||
                player.currentlyHeldObjectServer is TetraChemicalItem || player.drunkness != 0f ||
                player.drunknessInertia != 0f || player.increasingDrunknessThisFrame || player.poison != 0f ||
                player.physicsParent != null || player.overridePhysicsParent != null ||
                player.isInElevator || player.isInHangarShipRoom || player.inVehicleAnimation || player.isClimbingLadder ||
                player.inSpecialInteractAnimation || player.enteringSpecialAnimation || player.inAnimationWithEnemy != null ||
                player.disableSyncInAnimation ||
                player.jetpackControls || player.disablingJetpackControls || player.isPlayerSliding ||
                player.slipperyFloor != 0f || !Finite(player.slimeSlipAudioVolumeSync) || player.slimeSlipAudioVolumeSync > 0f ||
                (player.slimeSlipAudio != null && player.slimeSlipAudio.isPlaying) ||
                !Finite(player.externalForces.sqrMagnitude) || !Finite(player.externalForceAutoFade.sqrMagnitude) ||
                player.externalForces.sqrMagnitude > 0.0001f || player.externalForceAutoFade.sqrMagnitude > 0.0001f ||
                player.isUnderwater || player.isSinking || player.playerBodyAnimator == null ||
                !player.playerBodyAnimator.isActiveAndEnabled;
        }

        internal static void ResetForEvent(PlayerControllerB player)
        {
            if (player != null && States.TryGetValue(player, out var state) && Finite(Time.time))
                ResetWithGrace(state, player, Time.time);
        }

        private static void ResetWithGrace(ObservationState state, PlayerControllerB player, double now)
        {
            state.Tracker.ResetEvidence();
            state.GraceUntil = Math.Max(state.GraceUntil, now + GraceDuration(player));
            state.NextSample = now;
        }

        private static double GraceDuration(PlayerControllerB player) =>
            Finite(player.sprintTime) && player.sprintTime > 0f ? Math.Max(10d, 2d * player.sprintTime) : 10d;

        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);

        internal static void Forget(PlayerControllerB player)
        {
            if (!ReferenceEquals(player, null)) States.Remove(player);
        }

        internal static void Clear()
        {
            States.Clear();
            observedRound = null;
        }
    }

    // Optional ordinary-method postfixes observe accepted game events without parsing RPC senders or payloads.
    [HarmonyPatch]
    [HarmonyWrapSafe]
    internal static class StaminaObservationEventPatch
    {
        private static readonly List<MethodBase> Methods = FindMethods();

        private static List<MethodBase> FindMethods()
        {
            var result = new List<MethodBase>();
            foreach (var name in new[] { "DamageOnOtherClients", "TeleportPlayer", "PlayerJumpedClientRpc", "OnDestroy" })
            {
                var method = AccessTools.DeclaredMethod(typeof(PlayerControllerB), name);
                if (method != null) result.Add(method);
            }
            foreach (var name in new[] { "OnPlayerDC", "EndOfGame", "OnDestroy" })
            {
                var method = AccessTools.DeclaredMethod(typeof(StartOfRound), name);
                if (method != null) result.Add(method);
            }
            return result;
        }

        [HarmonyPrepare]
        private static bool Prepare() => Methods.Count > 0;

        [HarmonyTargetMethods]
        private static IEnumerable<MethodBase> TargetMethods() => Methods;

        [HarmonyPostfix]
        internal static void Postfix(object __instance, MethodBase __originalMethod)
        {
            if (__instance is PlayerControllerB player)
            {
                if (__originalMethod.Name == "OnDestroy") StaminaObservationPatch.Forget(player);
                else StaminaObservationPatch.ResetForEvent(player);
            }
            else if (__instance is StartOfRound)
            {
                // A disconnect/round teardown invalidates observed identities and round context.
                StaminaObservationPatch.Clear();
            }
        }
    }
}
