using System;
using System.Reflection;
using AntiCheat;
using AntiCheat.Patches;
using GameNetcodeStuff;
using UnityEngine;

static class Program
{
    static int cases, failures;
    static PlayerControllerB player;
    static void Assert(bool condition, string message) { if (!condition) throw new Exception(message); }
    static void Setup()
    {
        StaminaObservationPatch.Clear();
        Time.time = Time.realtimeSinceStartup = 100;
        Time.deltaTime = .25f; Time.timeScale = 1;
        Unity.Netcode.NetworkManager.Singleton = new Unity.Netcode.NetworkManager();
        PluginConfig.StaminaObservation.Value = true;
        AntiCheatPlugin.Notifications.Clear();
        var round = StartOfRound.Instance = new StartOfRound();
        player = new PlayerControllerB();
        player.transform.parent = round.playersContainer;
        round.allPlayerScripts = new[] { player };
        round.ClientPlayerList[7] = 0;
    }
    static void Run(string name, Action test)
    {
        cases++;
        try { Setup(); test(); Console.WriteLine("PASS " + name); }
        catch (Exception e) { failures++; Console.WriteLine("FAIL " + name + ": " + e.GetBaseException().Message); }
    }
    static void Tick(bool sprinting, float duration = .25f, bool fresh = true)
    {
        player.playerBodyAnimator.Sprinting = sprinting;
        Time.deltaTime = duration; Time.time += duration; Time.realtimeSinceStartup += duration;
        player.timeSincePlayerMoving = fresh ? 0 : 5;
        if (sprinting) player.serverPlayerPosition.x += 2.25f * player.movementSpeed * duration;
        StaminaObservationPatch.LateUpdate(player);
    }
    static void Idle(float duration = 15, bool fresh = true) { for (float n = 0; n < duration; n += .25f) Tick(false, fresh: fresh); }
    static void Sprint(float duration = 14, bool fresh = true) { for (float n = 0; n < duration; n += .25f) Tick(true, fresh: fresh); }
    static void Bout(float duration = 14) { Sprint(duration); Idle(2.5f); }
    static void ThreeBouts() { Bout(); Bout(); Bout(); }
    static void Quiet() => Assert(AntiCheatPlugin.Notifications.Count == 0, "unexpected suspicion notification");
    static int Main()
    {
        Run("three separate abnormal bouts notify only the observed remote", () => {
            Idle(); ThreeBouts();
            Assert(AntiCheatPlugin.Notifications.Count == 1 && ReferenceEquals(AntiCheatPlugin.Notifications[0], player), "expected one remote suspicion");
            Assert(player.health == 100 && player.sprintMeter == .75f && player.carryWeight == 1, "game state mutated");
        });
        Run("normal sprint and stale idle are allowed", () => { Idle(fresh: false); for (int n = 0; n < 10; n++) Bout(4); Quiet(); });
        Run("stale idle still confirms legitimate rest for observation", () => {
            Idle(fresh: false);
            for (int n = 0; n < 3; n++) { Sprint(); Idle(2.5f, fresh: false); }
            Assert(AntiCheatPlugin.Notifications.Count == 1, "idle wrongly required position packets");
        });
        Run("stale movement cannot accumulate suspicion", () => { Idle(); for (int n = 0; n < 3; n++) { Sprint(fresh: false); Idle(2.5f); } Quiet(); });
        Run("one uninterrupted long sprint is not three bouts", () => { Idle(); Sprint(80); Idle(2.5f); Quiet(); });
        Run("held TZP is excluded before gas or drunkness starts", () => { Idle(); player.currentlyHeldObjectServer = new TetraChemicalItem(); ThreeBouts(); Quiet(); });
        Run("TZP residual inertia and drunkness remain excluded", () => { Idle(); player.drunkness = .03f; ThreeBouts(); Quiet(); player.drunkness = 0; player.drunknessInertia = -.2f; ThreeBouts(); Quiet(); });
        Run("TZP effect ends with grace and a required rest", () => {
            Idle(); Bout(); Bout(); player.drunkness = .1f; Tick(true); player.drunkness = 0;
            Sprint(25); Idle(2.5f); Quiet();
        });
        Run("health loss and healing reset earlier evidence", () => { Idle(); Bout(); Bout(); player.health = 90; Tick(true, .05f); player.health = 100; Tick(true, .05f); Idle(); Bout(); Quiet(); });
        Run("damage event catches loss and healing between samples", () => {
            Idle(); Bout(); Bout();
            StaminaObservationEventPatch.Postfix(player, typeof(PlayerControllerB).GetMethod("DamageOnOtherClients", BindingFlags.Instance | BindingFlags.NonPublic));
            player.health = 100;
            Idle(); Bout(); Quiet();
        });
        Run("damage timestamp change is checked between samples", () => {
            Idle(); Bout(); Bout(); player.timeSinceTakingDamage = Time.realtimeSinceStartup; Tick(true, .05f);
            Idle(); Bout(); Quiet();
        });
        Run("remote owner mismatch is ignored", () => { Idle(); player.OwnerClientId = 99; ThreeBouts(); Quiet(); });
        Run("player slot mapping mismatch is ignored", () => { Idle(); StartOfRound.Instance.ClientPlayerList[7] = 5; ThreeBouts(); Quiet(); });
        Run("host player and nonhost cannot generate warnings", () => { Idle(); player.IsOwner = true; ThreeBouts(); Quiet(); player.IsOwner = false; StartOfRound.Instance.IsHost = false; ThreeBouts(); Quiet(); });
        Run("disabled observation has no effect", () => { Idle(); PluginConfig.StaminaObservation.Value = false; ThreeBouts(); Quiet(); });
        Run("unsafe round and level changes clear prior evidence", () => {
            Idle(); Bout(); Bout(); StartOfRound.Instance.shipIsLeaving = true; Tick(false); StartOfRound.Instance.shipIsLeaving = false;
            Idle(); Bout(); Quiet(); StartOfRound.Instance.currentLevel = new SelectableLevel(); Idle(); Bout(); Quiet();
        });
        Run("parent changes and special movements clear evidence", () => {
            Idle(); Bout(); Bout(); player.transform.parent = new Transform(); Tick(true); Idle(); Bout(); Quiet();
            player.jetpackControls = true; ThreeBouts(); Quiet();
        });
        Run("lifecycle clearing prevents evidence inheritance", () => { Idle(); Bout(); Bout(); StaminaObservationPatch.Clear(); Idle(); Bout(); Quiet(); });
        Run("bad timescale cannot accumulate", () => { Idle(); Time.timeScale = .5f; ThreeBouts(); Quiet(); });
        Run("animator transitions and poisoned movement are excluded", () => { Idle(); player.playerBodyAnimator.Transition = true; ThreeBouts(); Quiet(); player.playerBodyAnimator.Transition = false; player.poison = .1f; ThreeBouts(); Quiet(); });
        Run("nullable game and missing player safely reset", () => { StaminaObservationPatch.LateUpdate(null); StartOfRound.Instance = null; StaminaObservationPatch.LateUpdate(player); Quiet(); });
        Run("normal transitions and exhausted rests retain independent bouts", () => {
            Idle();
            for (int n = 0; n < 3; n++)
            {
                player.playerBodyAnimator.Transition = true; Tick(true); player.playerBodyAnimator.Transition = false;
                Sprint();
                player.playerBodyAnimator.Transition = true; player.isExhausted = true; Tick(false);
                player.playerBodyAnimator.Transition = false; Idle(2.5f); player.isExhausted = false;
            }
            Assert(AntiCheatPlugin.Notifications.Count == 1, "routine transitions erased all candidates");
        });
        Run("interleaved local player updates do not erase remote evidence", () => {
            Idle();
            var local = new PlayerControllerB { IsOwner = true, isHostPlayerObject = true, actualClientId = 0, OwnerClientId = 0 };
            for (int n = 0; n < 3; n++)
            {
                for (int frame = 0; frame < 56; frame++) { Tick(true); StaminaObservationPatch.LateUpdate(local); }
                for (int frame = 0; frame < 10; frame++) { Tick(false); StaminaObservationPatch.LateUpdate(local); }
            }
            Assert(AntiCheatPlugin.Notifications.Count == 1, "host object cleared remote evidence");
        });
        Run("stale remote teleport flags recover after grace and fresh rest", () => {
            Idle(); Bout(); Bout(); player.teleportedLastFrame = true; player.teleportingThisFrame = true;
            Tick(false); Idle(); Bout(); Quiet(); Bout(); Bout();
            Assert(AntiCheatPlugin.Notifications.Count == 1, "stale teleport flag permanently disabled observation");
        });
        Run("transient TZP before next sample clears all accumulated evidence", () => {
            Idle(); Bout(); Bout(); player.currentlyHeldObjectServer = new TetraChemicalItem(); Tick(true, .05f);
            player.currentlyHeldObjectServer = null; Idle(); Bout(); Quiet();
        });
        Run("lifecycle postfix clears entries on disconnect and destroy", () => {
            Idle(); Bout(); Bout();
            StaminaObservationEventPatch.Postfix(StartOfRound.Instance, typeof(StartOfRound).GetMethod("OnPlayerDC", BindingFlags.Instance | BindingFlags.NonPublic));
            Idle(); Bout(); Quiet();
            StaminaObservationEventPatch.Postfix(player, typeof(PlayerControllerB).GetMethod("OnDestroy", BindingFlags.Instance | BindingFlags.NonPublic));
            Idle(); Bout(); Quiet();
        });
        Run("jump and repeat teleport postfixes reset even unchanged flags", () => {
            Idle(); Bout(); Bout();
            StaminaObservationEventPatch.Postfix(player, typeof(PlayerControllerB).GetMethod("PlayerJumpedClientRpc", BindingFlags.Instance | BindingFlags.NonPublic));
            Idle(); Bout(); Quiet();
            StaminaObservationEventPatch.Postfix(player, typeof(PlayerControllerB).GetMethod("TeleportPlayer", BindingFlags.Instance | BindingFlags.NonPublic));
            Idle(); Bout(); Quiet();
        });
        Run("parent changes preserve notification cooldown for the same player", () => {
            Idle(); ThreeBouts();
            Assert(AntiCheatPlugin.Notifications.Count == 1, "first alert missing");
            player.transform.parent = new Transform(); Tick(false); Idle(); ThreeBouts();
            Assert(AntiCheatPlugin.Notifications.Count == 1, "parent change bypassed notification cooldown");
            Idle(70); ThreeBouts();
            Assert(AntiCheatPlugin.Notifications.Count == 2, "new evidence after cooldown did not recover");
        });
        Run("small time scale deviations are still excluded", () => { Idle(); Time.timeScale = .9995f; ThreeBouts(); Quiet(); });
        foreach (var exclusion in new (string Name, Action<PlayerControllerB> Set)[] {
            ("vehicle", p => p.inVehicleAnimation = true),
            ("ladder", p => p.isClimbingLadder = true),
            ("special animation", p => p.inSpecialInteractAnimation = true),
            ("water", p => p.isUnderwater = true),
            ("sinking", p => p.isSinking = true),
            ("physics parent", p => p.physicsParent = new Transform()),
            ("elevator", p => p.isInElevator = true),
            ("slime audio", p => p.slimeSlipAudio.isPlaying = true),
            ("remote slime synchronization before audio playback", p => p.slimeSlipAudioVolumeSync = .1f),
            ("external force", p => p.externalForceAutoFade = new Vector3(1, 0, 0)),
            ("nonfinite external force", p => p.externalForces = new Vector3(float.NaN, 0, 0)),
            ("dead player", p => p.isPlayerDead = true),
            ("unspawned player", p => p.IsSpawned = false),
            ("critical injury", p => p.criticallyInjured = true),
            ("nonfinite stamina baseline", p => p.sprintTime = float.NaN)
        })
        {
            Run(exclusion.Name + " is excluded", () => { Idle(); exclusion.Set(player); ThreeBouts(); Quiet(); });
        }
        Console.WriteLine($"{cases - failures}/{cases} passed");
        return failures == 0 ? 0 : 1;
    }
}
