using GameNetcodeStuff;

using Unity.Netcode;

namespace AntiCheat.Patches
{
    public static class PlayerControllerOperationLogPatch
    {
        // Registered only when its legacy handler exists; see LegacyRpcCompatibilityPatch.
        public static bool DropAllHeldItemsServerRpc(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
        {
            if (!PatchHelper.Check(rpcParams, out var player))
            {
                return player != null;
            }

            AntiCheatPlugin.LogInfo(player, "PlayerControllerB.DropAllHeldItemsServerRpc");
            return true;
        }
    }
}
