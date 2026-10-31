using System;
using System.Collections.Generic;
using System.Reflection;

namespace BepInEx
{
    public class BepInPlugin : Attribute { public BepInPlugin(string id, string name, string version) { } }
    public class BaseUnityPlugin { public BepInEx.Logging.ManualLogSource Logger = new(); }
}
namespace BepInEx.Logging { public class ManualLogSource { public void LogInfo(object value) { } public void LogWarning(object value) { } } }
namespace GameNetcodeStuff { public class PlayerControllerB { public string playerUsername = "Alice"; public ulong playerClientId = 1, actualClientId = 10; public bool IsOwner; } }
namespace HarmonyLib
{
    public class Harmony { public Harmony(string id) { } public void PatchAll(Assembly assembly) { } }
    public static class AccessTools { public static MethodInfo DeclaredMethod(Type type, string name) => type.GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance); }
}
namespace UnityEngine { public static class Time { public static float realtimeSinceStartup; } }
namespace AntiCheat.Locale
{
    public class LocalizationManager
    {
        public string MessageFormat() => "[{Prefix}] {msg}";
        public string Prefix() => "AC";
        public string Msg_GetString(string key, Dictionary<string, string> values = null)
        {
            string message = key == "RpcRejected" ? "{player} | {rpc} | {reason}" : key == "StaminaSuspicion" ? "Suspicion: {player} (client {id}); repeated long sprint, not proof" : key;
            if (values != null) foreach (var pair in values) message = message.Replace(pair.Key, pair.Value);
            return message;
        }
    }
}
namespace AntiCheat
{
    public class Entry<T> { public T Value; public Entry(T value) { Value = value; } }
    public class Feature { public bool Value = true, Kick; }
    public static class PluginConfig
    {
        public enum MessageType { GUI, HostChat, PublicChat }
        public static Entry<bool> Log = new(false);
        public static Entry<MessageType> DetectedMessageType = new(MessageType.GUI);
        public static Feature RpcValidation = new();
        public static void Init(object plugin) { }
    }
}
namespace AntiCheat.Patches
{
    public static class PatchHelper
    {
        public static AntiCheat.Locale.LocalizationManager locale => AntiCheatPlugin.localizationManager;
        public static int Kicks;
        public static bool ThrowKicking;
        public static void ShowMessage(string message, string key) => AntiCheatPlugin.ShowMessage(message, key);
        public static void KickPlayer(GameNetcodeStuff.PlayerControllerB player) { Kicks++; if (ThrowKicking) throw new InvalidOperationException("disconnect unavailable"); }
    }
}
internal static class LCMPluginInfo
{
    public const string PLUGIN_GUID = "AntiCheat", PLUGIN_NAME = "AstaAntiCheat", PLUGIN_VERSION = "1.0.1";
}
public class StartOfRound { public static StartOfRound Instance; public bool IsHost = true; }
public class HUDManager
{
    public static HUDManager Instance = new();
    public List<string> Tips = new(), HostChats = new(), PublicChats = new();
    public bool ThrowTip;
    public void DisplayTip(string headerText, string bodyText, bool isWarning = false, bool useSave = false, string prefsKey = "LC_Tip1")
    {
        if (ThrowTip) throw new InvalidOperationException("HUD not ready");
        Tips.Add(headerText + "|" + bodyText + "|" + isWarning + "|" + useSave);
    }
    public void AddChatMessage(string message, string name, int player, bool system) => HostChats.Add(message);
    public void AddTextMessageClientRpc(string message) => PublicChats.Add(message);
}
