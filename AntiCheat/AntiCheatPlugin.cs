using AntiCheat;
using AntiCheat.Locale;

using BepInEx;
using BepInEx.Logging;

using GameNetcodeStuff;

using HarmonyLib;

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Globalization;
using System.Text;
using UnityEngine;

[BepInPlugin(LCMPluginInfo.PLUGIN_GUID, LCMPluginInfo.PLUGIN_NAME, LCMPluginInfo.PLUGIN_VERSION)]
public class AntiCheatPlugin : BaseUnityPlugin
{
    internal static ManualLogSource Log = null!;
    internal static LocalizationManager localizationManager = null!;

    private static readonly Dictionary<string, float> _messageTimes = new Dictionary<string, float>();

    private static readonly MethodInfo AddChatMessageMethod =
        AccessTools.DeclaredMethod(typeof(HUDManager), "AddChatMessage");

    private static readonly MethodInfo AddTextMessageClientRpcMethod =
        AccessTools.DeclaredMethod(typeof(HUDManager), "AddTextMessageClientRpc");

    private void Awake()
    {
        Log = Logger;
        localizationManager = new LocalizationManager();

        PluginConfig.Init(this);

        var harmony = new Harmony(LCMPluginInfo.PLUGIN_GUID);
        harmony.PatchAll(Assembly.GetExecutingAssembly());
    }

    public static void LogInfo(string info)
    {
        if (StartOfRound.Instance != null && !StartOfRound.Instance.IsHost)
        {
            return;
        }

        if (PluginConfig.Log == null || PluginConfig.Log.Value)
        {
            Log.LogInfo(info);
            File.AppendAllLines(
                "AntiCheat.log",
                new[] { $"[{DateTime.Now:MM-dd HH:mm:ss:ff}] {info}" });
        }
    }

    public static void LogInfo(PlayerControllerB player, string rpc, params object[] param)
    {
        LogInfo($"{player.playerUsername}({player.playerClientId}) -> {rpc};{string.Join("|", param)}");
    }

    public static void ShowMessage(string msg, string dedupeKey = null)
    {
        if (HUDManager.Instance == null)
        {
            return;
        }

        var showMessage = localizationManager
            .MessageFormat()
            .Replace("{Prefix}", localizationManager.Prefix())
            .Replace("{msg}", msg);

        var compareKey = dedupeKey ?? showMessage;
        float now = Time.realtimeSinceStartup;
        if (_messageTimes.TryGetValue(compareKey, out float previous) && now >= previous && now - previous < 10f)
        {
            return;
        }

        // Keep notification history bounded, including lobbies with changing player names.
        if (_messageTimes.Count >= 256)
        {
            _messageTimes.Clear();
        }
        _messageTimes[compareKey] = now;
        LogInfo($"Detection|{showMessage}");

        switch (PluginConfig.DetectedMessageType.Value)
        {
            case PluginConfig.MessageType.PublicChat:
                AddTextMessageClientRpc(showMessage);
                break;
            case PluginConfig.MessageType.HostChat:
                ShowMessageHostOnly(showMessage);
                break;
            default:
                HUDManager.Instance.DisplayTip("AstaAntiCheat", showMessage, true, false);
                break;
        }
    }

    public static void ShowMessageHostOnly(string msg)
    {
        if (HUDManager.Instance == null)
        {
            return;
        }

        LogInfo($"ShowMessageHostOnly -> {msg}");
        AddChatMessageMethod.Invoke(HUDManager.Instance, new object[] { msg, "", -1, false });
    }

    internal static void ShowStaminaSuspicion(PlayerControllerB player)
    {
        if (StartOfRound.Instance == null || !StartOfRound.Instance.IsHost ||
            player == null || player.IsOwner || HUDManager.Instance == null)
            return;

        var name = new StringBuilder();
        foreach (char character in player.playerUsername ?? string.Empty)
        {
            if (name.Length >= 64) break;
            if (char.GetUnicodeCategory(character) == UnicodeCategory.Format) continue;
            name.Append(char.IsControl(character) ? ' ' : character == '<' ? '‹' : character == '>' ? '›' : character);
        }
        string message = localizationManager.Msg_GetString("StaminaSuspicion", new Dictionary<string, string>
        {
            { "{player}", name.ToString() },
            { "{id}", player.actualClientId.ToString() }
        });
        // Observation is uncertain: always local, regardless of the confirmed-detection destination.
        ShowMessageHostOnly(localizationManager.MessageFormat()
            .Replace("{Prefix}", localizationManager.Prefix()).Replace("{msg}", message));
    }

    public static void AddTextMessageClientRpc(string showMessage)
    {
        if (HUDManager.Instance == null)
        {
            return;
        }

        AddTextMessageClientRpcMethod.Invoke(HUDManager.Instance, new object[] { showMessage });
    }
}
