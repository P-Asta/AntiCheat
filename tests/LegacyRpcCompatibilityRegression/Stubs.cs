// Game boundary doubles; the test uses real Harmony and the production selector/dispatcher.
using System;
using System.Collections.Generic;
using Unity.Netcode;
namespace Unity.Netcode
{
    public class NetworkBehaviour { }
    public struct FastBufferReader { public int Position; }
    public struct __RpcParams { public ulong Sender; }
}
public static class AntiCheatPlugin
{
    public static readonly TestLogger Log = new TestLogger();
}
public sealed class TestLogger
{
    public readonly List<string> Warnings = new List<string>();
    public void LogWarning(object value) => Warnings.Add(value.ToString());
}
public class EnemyAI : NetworkBehaviour
{
#if LEGACY_RPC_ALL || LEGACY_RPC_MIXED
    private static void __rpc_handler_255411420(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams) { }
#endif
    private static void __rpc_handler_2081148948(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams) { }
}
namespace GameNetcodeStuff
{
    public class PlayerControllerB : NetworkBehaviour
    {
#if LEGACY_RPC_ALL
        private static void __rpc_handler_760742013(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams) { }
        private static void __rpc_handler_4121569671(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams) { }
        private static void __rpc_handler_2013428264(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams) { }
#endif
        // Current universal RPC must never get routed into legacy ServerRpc validation.
        private static void __rpc_handler_890924887(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams) { }
    }
}
public class GrabbableObject : NetworkBehaviour
{
#if LEGACY_RPC_ALL || LEGACY_RPC_MIXED
    private static void __rpc_handler_4280509730(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams) { }
#endif
    private static void __rpc_handler_319375719(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams) { }
}
namespace AntiCheat.Patches
{
    public static class DispatchCapture
    {
        public static string Name;
        public static NetworkBehaviour Target;
        public static FastBufferReader Reader;
        public static __RpcParams Parameters;
        public static bool ReturnValue;
        public static bool Record(string name, NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
        {
            Name = name; Target = target; Reader = reader; Parameters = rpcParams;
            return ReturnValue;
        }
    }
    public static class EnemyAIControlPatch
    {
        public static bool UpdateEnemyPositionServerRpc(NetworkBehaviour t, FastBufferReader r, __RpcParams p) => DispatchCapture.Record("enemy", t, r, p);
    }
    public static class PlayerControllerOperationLogPatch
    {
        public static bool DropAllHeldItemsServerRpc(NetworkBehaviour t, FastBufferReader r, __RpcParams p) => DispatchCapture.Record("drop", t, r, p);
    }
    public static class PlayerControllerCombatPatch
    {
        public static bool KillPlayerServerRpc(NetworkBehaviour t, FastBufferReader r, __RpcParams p) => DispatchCapture.Record("kill", t, r, p);
    }
    public static class PlayerControllerMovementPatch
    {
        public static bool UpdatePlayerPositionServerRpc(NetworkBehaviour t, FastBufferReader r, __RpcParams p) => DispatchCapture.Record("move", t, r, p);
    }
    public static class GrabbableObjectShipSystemsPatch
    {
        public static bool ActivateItemServerRpc(NetworkBehaviour t, FastBufferReader r, __RpcParams p) => DispatchCapture.Record("activate", t, r, p);
    }
}
