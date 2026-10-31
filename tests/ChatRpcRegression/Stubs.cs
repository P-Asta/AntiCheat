using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace HarmonyLib
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class HarmonyPatch : Attribute
    {
        public HarmonyPatch(Type type) { }
        public HarmonyPatch(Type type, string name) { }
    }
    public class HarmonyPrefix : Attribute { }
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class HarmonyWrapSafe : Attribute { }
    public static class AccessTools
    {
        public static MethodInfo DeclaredMethod(Type type, string name) => type.GetMethod(name);
    }
}
namespace GameNetcodeStuff
{
    public class PlayerControllerB
    {
        public string playerUsername = "sender";
        public ulong playerSteamId = 123;
        public ulong playerClientId = 1;
    }
}
namespace Unity.Netcode
{
    public class NetworkBehaviour { }
    public struct __RpcParams { }
    public static class FastBufferWriter { public struct ForPrimitives { } }
    public class ReaderState
    {
        public byte[] Bytes;
        public int Position;
        public int StringReads;
        public int PackedReads;
    }
    public struct FastBufferReader
    {
        public ReaderState State;
        public int Position => State.Position;
        public int Length => State.Bytes.Length;
        public void Seek(int position) => State.Position = position;
        public bool TryBeginRead(int count) => count >= 0 && count <= Length - Position;
        public void ReadValueSafe<T>(out T value, FastBufferWriter.ForPrimitives marker = default) where T : unmanaged
        {
            int count = typeof(T) == typeof(bool) || typeof(T) == typeof(byte) ? 1 : 4;
            if (!TryBeginRead(count)) throw new OverflowException("truncated primitive");
            object result = typeof(T) == typeof(bool) ? (object)(State.Bytes[Position] != 0) : typeof(T) == typeof(byte) ? (object)State.Bytes[Position] : BitConverter.ToUInt32(State.Bytes, Position);
            State.Position += count;
            value = (T)result;
        }
        public void ReadValueSafe(out string value, bool oneByteChars = false)
        {
            State.StringReads++;
            ReadValueSafe(out uint count);
            // Never allocate hostile lengths in the harness. A call still counts as a failed safety boundary.
            if (count > 4096) throw new OverflowException("would allocate an oversized string");
            int bytes = checked((int)count * (oneByteChars ? 1 : 2));
            if (!TryBeginRead(bytes)) throw new OverflowException("truncated string");
            value = Encoding.Unicode.GetString(State.Bytes, Position, bytes);
            State.Position += bytes;
        }
        public byte ReadByte()
        {
            if (!TryBeginRead(1)) throw new OverflowException("truncated packed int");
            return State.Bytes[State.Position++];
        }
    }
    public static class ByteUnpacker
    {
        public static void ReadValueBitPacked(FastBufferReader reader, out int value)
        {
            reader.State.PackedReads++;
            byte first = reader.ReadByte();
            int size = first & 7;
            if (size == 0) { reader.Seek(reader.Position - 1); value = 0; return; }
            if (size > 5)
            {
                if (!reader.TryBeginRead(size - 1)) throw new OverflowException("truncated packed int");
                reader.Seek(reader.Position + size - 1);
                value = 0;
                return;
            }
            uint bits = size == 5 ? 0u : first;
            for (int i = size == 5 ? 0 : 1; i < (size == 5 ? 4 : size); i++)
                bits |= (uint)reader.ReadByte() << (i * 8);
            if (size != 5) bits >>= 3;
            value = (int)(bits >> 1) ^ -(int)(bits & 1);
        }
    }
}
public class StartOfRound
{
    public static StartOfRound Instance;
    public GameNetcodeStuff.PlayerControllerB[] allPlayerScripts;
}
public class HUDManager : Unity.Netcode.NetworkBehaviour
{
    public static HUDManager Instance = new HUDManager();
    public readonly List<string> Broadcasts = new List<string>();
    public void AddPlayerChatMessageClientRpc(string text, int actor) => Broadcasts.Add(actor + ":" + text);
}
namespace AntiCheat
{
    public class Setting { public bool Value; }
    public static class PluginConfig
    {
        public static Setting ChatReal = new Setting();
        public static Setting ChatReal2 = new Setting();
    }
}
namespace AntiCheat.Utils
{
    public static class CooldownManager
    {
        public static bool Allowed = true;
        public static int Calls;
        public static bool CheckCooldown(string group, GameNetcodeStuff.PlayerControllerB player) { Calls++; return Allowed; }
    }
}
namespace AntiCheat.Patches
{
    public class LocaleStub
    {
        public string current_language = "en_US";
        public string Msg_GetString(string key, Dictionary<string, string> replacements) => key;
    }
    public static class PatchHelper
    {
        public static LocaleStub locale = new LocaleStub();
        public static GameNetcodeStuff.PlayerControllerB Player;
        public static bool CheckResult = true;
        public static readonly List<string> Messages = new List<string>();
        public static int Kicks;
        public static bool Check(Unity.Netcode.__RpcParams rpc, out GameNetcodeStuff.PlayerControllerB player) { player = Player; return CheckResult; }
        public static void LogInfo(GameNetcodeStuff.PlayerControllerB player, string rpc, params object[] parameters) { }
        public static void LogInfo(string message) { }
        public static void ShowMessage(string message) => Messages.Add(message);
        public static void KickPlayer(GameNetcodeStuff.PlayerControllerB player) => Kicks++;
    }
    public static class RpcGuard
    {
        public static bool Enabled = true;
        public static readonly List<string> Reasons = new List<string>();
        public static bool Reject(GameNetcodeStuff.PlayerControllerB player, string rpc, string reason, bool legacyKick = false)
        {
            if (player != PatchHelper.Player) throw new Exception("wrong attributed sender");
            Reasons.Add(reason);
            if (legacyKick) PatchHelper.KickPlayer(player);
            return false;
        }
    }
}
