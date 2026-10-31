using System;
using System.Collections;
using System.Reflection;
using AntiCheat;
using AntiCheat.Patches;
using GameNetcodeStuff;
using UnityEngine;

static class Program
{
    static int count, failures;
    static void Assert(bool condition, string message) { if (!condition) throw new Exception(message); }
    static void Run(string name, Action test)
    {
        count++;
        HUDManager.Instance = new();
        StartOfRound.Instance = null;
        AntiCheatPlugin.localizationManager = new();
        AntiCheatPlugin.Log = new();
        PluginConfig.DetectedMessageType.Value = PluginConfig.MessageType.GUI;
        Time.realtimeSinceStartup = 100;
        PluginConfig.RpcValidation = new();
        PatchHelper.Kicks = 0;
        PatchHelper.ThrowKicking = false;
        foreach (var field in typeof(AntiCheatPlugin).GetFields(BindingFlags.Static | BindingFlags.NonPublic))
        {
            if (field.FieldType == typeof(string)) field.SetValue(null, "");
            if (field.GetValue(null) is IDictionary dictionary) dictionary.Clear();
        }
        try { test(); Console.WriteLine("PASS " + name); }
        catch (Exception ex) { failures++; Console.WriteLine("FAIL " + name + ": " + ex.Message); }
    }
    static int Main()
    {
        Run("GUI mode shows an actual warning", () => {
            AntiCheatPlugin.ShowMessage("Blocked invalid request");
            Assert(HUDManager.Instance.Tips.Count == 1, "no game notification");
            Assert(HUDManager.Instance.Tips[0] == "AstaAntiCheat|[AC] Blocked invalid request|True|False", "wrong notification contents/options");
            Assert(HUDManager.Instance.PublicChats.Count == 0, "GUI broadcast to clients");
        });
        Run("host chat stays local", () => {
            PluginConfig.DetectedMessageType.Value = PluginConfig.MessageType.HostChat;
            AntiCheatPlugin.ShowMessage("blocked");
            Assert(HUDManager.Instance.HostChats.Count == 1 && HUDManager.Instance.PublicChats.Count == 0, "wrong destination");
        });
        Run("public chat destination retained", () => {
            PluginConfig.DetectedMessageType.Value = PluginConfig.MessageType.PublicChat;
            AntiCheatPlugin.ShowMessage("blocked");
            Assert(HUDManager.Instance.PublicChats.Count == 1, "no public notification");
        });
        Run("interleaved duplicate alerts are throttled", () => {
            PluginConfig.DetectedMessageType.Value = PluginConfig.MessageType.PublicChat;
            AntiCheatPlugin.ShowMessage("one", "actor1:buy");
            AntiCheatPlugin.ShowMessage("two", "actor2:buy");
            AntiCheatPlugin.ShowMessage("one with different detail", "actor1:buy");
            Assert(HUDManager.Instance.PublicChats.Count == 2, "interleaving bypassed deduplication");
        });
        Run("same detection can notify again after cooldown", () => {
            PluginConfig.DetectedMessageType.Value = PluginConfig.MessageType.PublicChat;
            AntiCheatPlugin.ShowMessage("blocked", "actor1:buy");
            Time.realtimeSinceStartup = 110;
            AntiCheatPlugin.ShowMessage("blocked", "actor1:buy");
            Assert(HUDManager.Instance.PublicChats.Count == 2, "alert suppressed forever");
        });
        Run("duplicate suppressed before cooldown expires", () => {
            PluginConfig.DetectedMessageType.Value = PluginConfig.MessageType.PublicChat;
            AntiCheatPlugin.ShowMessage("blocked", "key");
            Time.realtimeSinceStartup = 109.9f;
            AntiCheatPlugin.ShowMessage("blocked", "key");
            Assert(HUDManager.Instance.PublicChats.Count == 1, "cooldown not honored");
        });
        Run("missing HUD does not consume notification", () => {
            PluginConfig.DetectedMessageType.Value = PluginConfig.MessageType.PublicChat;
            HUDManager.Instance = null;
            AntiCheatPlugin.ShowMessage("blocked", "key");
            HUDManager.Instance = new();
            AntiCheatPlugin.ShowMessage("blocked", "key");
            Assert(HUDManager.Instance.PublicChats.Count == 1, "alert lost while HUD unavailable");
        });
        Run("invalid request rejected and actor notified without automatic kick", () => {
            bool allowed = RpcGuard.Reject(new PlayerControllerB(), "BuyItems", "RpcInvalidIndex");
            Assert(!allowed, "invalid request accepted");
            Assert(HUDManager.Instance.Tips.Count == 1 && HUDManager.Instance.Tips[0].Contains("Alice | BuyItems | RpcInvalidIndex"), "actor/reason missing");
            Assert(PatchHelper.Kicks == 0, "default unexpectedly kicks");
        });
        Run("configured kick applies only to known sender", () => {
            PluginConfig.RpcValidation.Kick = true;
            RpcGuard.Reject(new PlayerControllerB(), "BuyItems", "RpcInvalidIndex");
            RpcGuard.Reject(null, "BuyItems", "RpcInvalidIndex");
            Assert(PatchHelper.Kicks == 1, "kick wrong actor");
        });
        Run("disabled guard does not accuse or kick", () => {
            PluginConfig.RpcValidation.Value = false;
            PluginConfig.RpcValidation.Kick = true;
            RpcGuard.Reject(new PlayerControllerB(), "BuyItems", "RpcInvalidIndex");
            Assert(!RpcGuard.Enabled && HUDManager.Instance.Tips.Count == 0 && PatchHelper.Kicks == 0, "disabled guard has enforcement side effects");
        });
        Run("legacy kick remains active when new kick defaults off", () => {
            RpcGuard.Reject(new PlayerControllerB(), "Chat", "RpcChatIdentity", true);
            Assert(PatchHelper.Kicks == 1, "legacy enforcement lost");
        });
        Run("both kick settings produce only one kick", () => {
            PluginConfig.RpcValidation.Kick = true;
            RpcGuard.Reject(new PlayerControllerB(), "Chat", "RpcChatIdentity", true);
            Assert(PatchHelper.Kicks == 1, "duplicate kick");
        });
        Run("sender name cannot inject rich text or newlines into warning", () => {
            RpcGuard.Reject(new PlayerControllerB { playerUsername = "<size=0>Bob\nInjected" }, "BuyItems", "RpcInvalidIndex");
            Assert(HUDManager.Instance.Tips.Count == 1, "missing alert");
            Assert(!HUDManager.Instance.Tips[0].Contains("<size") && !HUDManager.Instance.Tips[0].Contains('\n'), "untrusted name markup/control characters rendered");
        });
        Run("sender rename does not bypass notification throttle", () => {
            var player = new PlayerControllerB();
            RpcGuard.Reject(player, "BuyItems", "RpcInvalidIndex");
            player.playerUsername = "Renamed";
            RpcGuard.Reject(player, "BuyItems", "RpcInvalidIndex");
            Assert(HUDManager.Instance.Tips.Count == 1, "renaming bypassed throttle");
        });
        Run("HUD failure cannot let a rejected RPC reach vanilla", () => {
            HUDManager.Instance.ThrowTip = true;
            PluginConfig.RpcValidation.Kick = true;
            Assert(!RpcGuard.Reject(new PlayerControllerB(), "BuyItems", "RpcInvalidIndex"), "invalid request accepted");
            Assert(PatchHelper.Kicks == 1, "configured enforcement lost after UI error");
        });
        Run("disconnect failure still cancels the invalid request", () => {
            PluginConfig.RpcValidation.Kick = true;
            PatchHelper.ThrowKicking = true;
            Assert(!RpcGuard.Reject(new PlayerControllerB(), "BuyItems", "RpcInvalidIndex"), "invalid request accepted");
        });
        Run("stamina suspicion is host-only even with public notifications configured", () => {
            StartOfRound.Instance = new();
            PluginConfig.DetectedMessageType.Value = PluginConfig.MessageType.PublicChat;
            AntiCheatPlugin.ShowStaminaSuspicion(new PlayerControllerB());
            Assert(HUDManager.Instance.HostChats.Count == 1, "host did not receive suspicion");
            Assert(HUDManager.Instance.HostChats[0].Contains("Alice") && HUDManager.Instance.HostChats[0].Contains("not proof"), "missing actor/uncertainty");
            Assert(HUDManager.Instance.PublicChats.Count == 0 && PatchHelper.Kicks == 0, "suspicion enforced or broadcast");
        });
        Run("stamina suspicion sanitizes player markup", () => {
            StartOfRound.Instance = new();
            AntiCheatPlugin.ShowStaminaSuspicion(new PlayerControllerB { playerUsername = "<size=0>Hidden\nFake" });
            Assert(HUDManager.Instance.HostChats.Count == 1, "no local report");
            Assert(!HUDManager.Instance.HostChats[0].Contains("<size") && !HUDManager.Instance.HostChats[0].Contains('\n'), "unsafe name in report");
        });
        Run("client cannot emit stamina suspicion", () => {
            StartOfRound.Instance = new() { IsHost = false };
            AntiCheatPlugin.ShowStaminaSuspicion(new PlayerControllerB());
            Assert(HUDManager.Instance.HostChats.Count == 0 && HUDManager.Instance.PublicChats.Count == 0, "client emitted suspicion");
        });
        Run("missing round or subject is silent", () => {
            AntiCheatPlugin.ShowStaminaSuspicion(new PlayerControllerB());
            StartOfRound.Instance = new();
            AntiCheatPlugin.ShowStaminaSuspicion(null);
            Assert(HUDManager.Instance.HostChats.Count == 0, "missing context report");
        });
        Run("local player is excluded from stamina suspicion", () => {
            StartOfRound.Instance = new();
            AntiCheatPlugin.ShowStaminaSuspicion(new PlayerControllerB { IsOwner = true });
            Assert(HUDManager.Instance.HostChats.Count == 0, "host was reported");
        });
        Console.WriteLine($"{count - failures}/{count} passed");
        return failures == 0 ? 0 : 1;
    }
}
