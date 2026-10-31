// Unity/network boundaries; the tests execute the actual production RPC prefix.
using System;
using System.Collections.Generic;

namespace HarmonyLib
{
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    public class HarmonyPatch : Attribute { public HarmonyPatch(Type type) {} public HarmonyPatch(string name) {} }
    public class HarmonyPrefix : Attribute {}
    public class HarmonyWrapSafe : Attribute {}
}
namespace Unity.Netcode
{
    public class NetworkBehaviour { public ulong OwnerClientId; }
    public class NetworkObject
    {
        public GrabbableObject Item;
        public T GetComponent<T>() where T : class => Item as T;
    }
    public struct NetworkObjectReference
    {
        public NetworkObject Object;
        public bool TryGet(out NetworkObject value, object manager = null) { value = Object; return value != null; }
    }
    public class ReaderState
    {
        public int Position;
        public int PlayerIndex = 2;
        public NetworkObjectReference Reference;
        public bool ThrowReadingPlayerIndex;
    }
    public struct FastBufferReader
    {
        public ReaderState State;
        public int Position => State.Position;
        public void ReadValueSafe(out NetworkObjectReference value, object unused)
        {
            State.Position += 8;
            value = State.Reference;
        }
        public void Seek(int position) => State.Position = position;
    }
    public static class ByteUnpacker
    {
        public static void ReadValueBitPacked(FastBufferReader reader, out int value)
        {
            reader.State.Position++;
            if (reader.State.ThrowReadingPlayerIndex) throw new InvalidOperationException("incomplete RPC");
            value = reader.State.PlayerIndex;
        }
    }
    public struct __RpcParams {}
}
public class Item
{
    public bool isScrap;
    public int itemId;
    public string itemName = "test item";
}
public class GrabbableObject : Unity.Netcode.NetworkBehaviour
{
    public Item itemProperties = new Item();
    public bool isHeld, isHeldByEnemy, heldByPlayerOnServer;
    public GameNetcodeStuff.PlayerControllerB playerHeldBy;
}
public class BeltBagItem : GrabbableObject
{
    public List<int> CancelledPlayerIndices = new List<int>();
    public void CancelAddObjectToBagClientRpc(int index) => CancelledPlayerIndices.Add(index);
}
namespace GameNetcodeStuff
{
    public class PlayerControllerB
    {
        public ulong playerClientId = 2;
        public string playerUsername = "sender";
    }
}
namespace AntiCheat
{
    public class Setting { public bool Value = true; }
    public static class PluginConfig
    {
        public static Setting GrabObject = new Setting(), GrabObject_SendLog = new Setting(), GrabObject_BeltBag = new Setting();
    }
}
namespace AntiCheat.Patches
{
    public class TestLocale { public string Msg_GetString(string key, Dictionary<string, string> replacements) => key; }
    public static class PatchHelper
    {
        public static GameNetcodeStuff.PlayerControllerB Player;
        public static bool CheckResult = true;
        public static List<string> Messages = new List<string>();
        public static TestLocale locale = new TestLocale();
        public static bool Check(Unity.Netcode.__RpcParams args, out GameNetcodeStuff.PlayerControllerB player)
        { player = Player; return CheckResult; }
        public static void ShowMessage(string message) => Messages.Add(message);
    }
}

