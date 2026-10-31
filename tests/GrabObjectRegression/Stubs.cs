// Minimal game boundary doubles: execute the actual production RPC prefix without Unity.
using System;
using System.Collections.Generic;
using System.Reflection;

namespace HarmonyLib
{
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    public class HarmonyPatch : Attribute { public HarmonyPatch(Type t) {} public HarmonyPatch(string s) {} }
    public class HarmonyWrapSafe : Attribute {}
    public class HarmonyPrefix : Attribute {}
    public static class AccessTools
    {
        public static MethodInfo Method(Type type, string name, Type[] parameters) =>
            type.GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, parameters, null);
        public static FieldInfo Field(Type type, string name) =>
            type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
    }
}
namespace UnityEngine
{
    public class Transform
    {
        public Vector3 position;
        public Transform parent;
        // Translation and scaling cover the parent-space regression; fixtures do not use rotation.
        public Vector3 localScale = new Vector3(1, 1, 1);
        public Vector3 TransformPoint(Vector3 point) => position + new Vector3(point.x * localScale.x, point.y * localScale.y, point.z * localScale.z);
    }
    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public static Vector3 zero => default;
        public float sqrMagnitude => x * x + y * y + z * z;
        public static Vector3 operator +(Vector3 a, Vector3 b) => new Vector3(a.x + b.x, a.y + b.y, a.z + b.z);
        public static Vector3 operator -(Vector3 a, Vector3 b) => new Vector3(a.x - b.x, a.y - b.y, a.z - b.z);
        public static float Distance(Vector3 a, Vector3 b) => MathF.Sqrt((a - b).sqrMagnitude);
        public override string ToString() => $"({x}, {y}, {z})";
    }
}
namespace Unity.Netcode
{
    public class NetworkBehaviour
    {
        protected enum __RpcExecStage { Send = 0, Execute = 1, Client = 2 }
        protected __RpcExecStage __rpc_exec_stage;
        public ulong OwnerClientId = 7;
        public bool IsServer = true, IsHost = true;
        public int RpcStage { get => (int)__rpc_exec_stage; set => __rpc_exec_stage = (__RpcExecStage)value; }
    }
    public class NetworkObject
    {
        public GrabbableObject Item;
        public T GetComponentInChildren<T>() where T : class => Item as T;
    }
    public struct NetworkObjectReference
    {
        public NetworkObject Object;
        public bool TryGet(out NetworkObject obj, object manager = null) { obj = Object; return obj != null; }
    }
    public struct FastBufferReader
    {
        // Unity reader copies share the native cursor, including through prefix arguments.
        sealed class Cursor { public int Position; }
        readonly Cursor cursor;
        public NetworkObjectReference Value;
        public int Position => cursor.Position;
        public FastBufferReader(NetworkObjectReference value, int position = 0) { Value = value; cursor = new Cursor { Position = position }; }
        public void ReadValueSafe(out NetworkObjectReference value, object unused) { value = Value; cursor.Position += 8; }
        public void Seek(int offset) => cursor.Position = offset;
    }
    public struct ServerRpcReceiveParams { public ulong SenderClientId; }
    public struct ServerRpcParams { public ServerRpcReceiveParams Receive; }
    public struct __RpcParams { public ServerRpcParams Server; }
}
public class Item
{
    public bool twoHanded, isScrap, disallowUtilitySlot;
    public string itemName = "test item";
}
public class GrabbableObject
{
    public Item itemProperties = new Item();
    public bool heldByPlayerOnServer, isHeld, isHeldByEnemy, isPocketed;
    public GameNetcodeStuff.PlayerControllerB playerHeldBy;
    public UnityEngine.Transform transform = new UnityEngine.Transform();
}
public class JetpackItem : GrabbableObject {}
public class StartOfRound
{
    public static StartOfRound Instance = new StartOfRound();
    public bool shipIsLeaving, shipHasLanded;
}
namespace GameNetcodeStuff
{
    public class PlayerControllerB : Unity.Netcode.NetworkBehaviour
    {
        public GrabbableObject[] ItemSlots = new GrabbableObject[4];
        public GrabbableObject currentlyHeldObjectServer;
#if !LEGACY_SLOTS
        public GrabbableObject ItemOnlySlot;
#endif
        public int currentItemSlot;
        public ulong actualClientId = 7, playerClientId = 1;
        public string playerUsername = "player";
        public UnityEngine.Transform transform = new UnityEngine.Transform();
        public UnityEngine.Vector3 serverPlayerPosition;
        public bool teleportedLastFrame, twoHanded, isHoldingObject, Cancelled;
        public int ServerRpcCalls, ClientRpcCalls, ClientRpcStageAtCall;
        public Unity.Netcode.NetworkObjectReference CancelledReference;
        public bool ThrowSlotLookup;

        // Native v81 slot contract, checked against installed Assembly-CSharp.dll.
#if !LEGACY_SLOTS
        private int FirstEmptyItemSlot(GrabbableObject attemptingGrab = null)
        {
            if (ThrowSlotLookup) throw new InvalidOperationException("simulated slot resolver failure");
            if (ItemOnlySlot == null && attemptingGrab != null && !attemptingGrab.itemProperties.isScrap &&
                !attemptingGrab.itemProperties.twoHanded && !attemptingGrab.itemProperties.disallowUtilitySlot)
                return 50;
            if (currentItemSlot != 50 && ItemSlots[currentItemSlot] == null) return currentItemSlot;
            return Array.FindIndex(ItemSlots, item => item == null);
        }
#endif
        // Model the old cancellation route until production stops manufacturing a server RPC.
        private void GrabObjectServerRpc(Unity.Netcode.NetworkObjectReference value)
        {
            ServerRpcCalls++;
            if (__rpc_exec_stage != __RpcExecStage.Execute) return;
            __rpc_exec_stage = __RpcExecStage.Send;
            GrabObjectClientRpc(false, value);
        }
        public void GrabObjectClientRpc(bool validated, Unity.Netcode.NetworkObjectReference value)
        {
            ClientRpcCalls++;
            ClientRpcStageAtCall = RpcStage;
            Cancelled = !validated;
            CancelledReference = value;
        }
    }
}
namespace AntiCheat
{
    public class Setting { public bool Value; public Setting(bool value) { Value = value; } }
    public static class PluginConfig
    {
        public static Setting GrabObject = new Setting(true), GrabObject_SendLog = new Setting(true),
            GrabObject_TwoHand = new Setting(false), GrabObject_MoreSlot = new Setting(true), GrabObject_Kick = new Setting(true);
    }
}
namespace AntiCheat.Patches
{
    public class TestLocale { public string Msg_GetString(string key, Dictionary<string, string> args) => key; }
    public static class PatchHelper
    {
        public static GameNetcodeStuff.PlayerControllerB Player;
        public static TestLocale locale = new TestLocale();
        public static List<string> Messages = new List<string>();
        public static bool Kicked, SkipValidation;
        public static bool Check(Unity.Netcode.__RpcParams args, out GameNetcodeStuff.PlayerControllerB player)
        {
            player = Player != null && Player.actualClientId == args.Server.Receive.SenderClientId ? Player : null;
            return player != null && !SkipValidation;
        }
        public static void LogInfo(GameNetcodeStuff.PlayerControllerB player, string rpc, params object[] args) {}
        public static void LogInfo(string message) {}
        public static void ShowMessage(string message) => Messages.Add(message);
        public static void KickPlayer(GameNetcodeStuff.PlayerControllerB player) => Kicked = true;
    }
}
