using System;
using System.Linq;
using System.Reflection;
using AntiCheat.Patches;
using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;

static class Program
{
    static int cases, failures;
    static PlayerControllerB player;
    static readonly MethodInfo LateUpdate = AccessTools.DeclaredMethod(typeof(PlayerControllerB), "LateUpdate");
    static readonly string[] PlayerEvents = { "DamageOnOtherClients", "TeleportPlayer", "PlayerJumpedClientRpc", "OnDestroy" };
    static readonly string[] RoundEvents = { "OnPlayerDC", "EndOfGame", "OnDestroy" };
    static void Assert(bool value, string message) { if (!value) throw new Exception(message); }
    static void Run(string name, Action test)
    {
        cases++;
        try { test(); Console.WriteLine("PASS " + name); }
        catch (Exception error) { failures++; Console.WriteLine("FAIL " + name + ": " + error.GetBaseException().Message); }
    }
    static void Setup()
    {
        StaminaObservationPatch.Clear();
        AntiCheatPlugin.Notifications = 0;
        Time.time = 100;
        Time.deltaTime = .25f;
        Time.timeScale = 1;
        var round = StartOfRound.Instance = new StartOfRound();
        player = new PlayerControllerB();
        player.transform.parent = round.playersContainer;
        round.allPlayerScripts = new[] { player };
        round.ClientPlayerList[7] = 0;
    }
    static void Tick(bool sprint)
    {
        Time.time += .25f;
        player.timeSincePlayerMoving = 0;
        player.playerBodyAnimator.Sprinting = sprint;
        if (sprint) player.serverPlayerPosition.x += .5625f;
        LateUpdate.Invoke(player, null);
    }
    static void Idle(float duration = 15) { for (float n = 0; n < duration; n += .25f) Tick(false); }
    static void Bout() { for (int n = 0; n < 56; n++) Tick(true); Idle(2.5f); }
    static object[] Arguments(MethodBase method) => method.GetParameters().Select(parameter =>
        parameter.HasDefaultValue ? parameter.DefaultValue : parameter.ParameterType.IsValueType ? Activator.CreateInstance(parameter.ParameterType) : null).ToArray();

    static int Main()
    {
        var harmony = new Harmony("AstaAntiCheat.StaminaObservationHarmonyRegression");
        Run("real Harmony installs the observer and only available optional events", () => {
            var patches = harmony.CreateClassProcessor(typeof(StaminaObservationPatch)).Patch();
            Assert(patches.Count == 1 && Harmony.GetPatchInfo(LateUpdate).Owners.Contains(harmony.Id), "LateUpdate was not installed");
            var events = harmony.CreateClassProcessor(typeof(StaminaObservationEventPatch)).Patch();
#if STAMINA_EVENTS_ALL
            const int expected = 7;
#elif STAMINA_EVENTS_MIXED
            const int expected = 2;
#else
            const int expected = 0;
#endif
            Assert(events.Count == expected, "optional event target count differs: " + events.Count);
            var originals = Harmony.GetAllPatchedMethods().Where(method => Harmony.GetPatchInfo(method).Owners.Contains(harmony.Id)).ToArray();
            Assert(originals.Length == expected + 1, "unexpected extra target installed");
            foreach (var original in originals.Where(method => method != LateUpdate))
                Assert((original.DeclaringType == typeof(PlayerControllerB) ? PlayerEvents : RoundEvents).Contains(original.Name), "unrelated method patched");
        });
        Run("installed LateUpdate postfix preserves original execution and produces a suspicion", () => {
            Setup(); Idle(); Bout(); Bout(); Bout();
            Assert(player.OriginalLateUpdateCalls > 0, "original LateUpdate did not execute");
            Assert(AntiCheatPlugin.Notifications == 1, "installed postfix did not observe three bouts");
        });
        foreach (var type in new[] { typeof(PlayerControllerB), typeof(StartOfRound) })
        foreach (var name in type == typeof(PlayerControllerB) ? PlayerEvents : RoundEvents)
        {
            var method = AccessTools.DeclaredMethod(type, name);
            if (method == null) continue;
            Run(type.Name + "." + name + " executes original and clears pending evidence", () => {
                Setup(); Idle(); Bout(); Bout();
                object instance = type == typeof(PlayerControllerB) ? (object)player : StartOfRound.Instance;
                method.Invoke(instance, Arguments(method));
                Assert(player.OriginalEventCalls + StartOfRound.Instance.OriginalEventCalls == 1, "event original was skipped or repeated");
                Idle(); Bout();
                Assert(AntiCheatPlugin.Notifications == 0, "event did not clear the two earlier bouts");
                Bout(); Bout();
                Assert(AntiCheatPlugin.Notifications == 1, "observer did not resume after event");
            });
        }
        harmony.UnpatchSelf();
        Console.WriteLine($"{cases - failures}/{cases} passed");
        return failures == 0 ? 0 : 1;
    }
}
