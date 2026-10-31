using AntiCheat.Utils;

using Unity.Netcode;

namespace AntiCheat.Patches
{
    public static class GrabbableObjectShipSystemsPatch
    {
        // Registered only when its legacy handler exists; see LegacyRpcCompatibilityPatch.
        public static bool ActivateItemServerRpc(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
        {
            if (PatchHelper.Check(rpcParams, out var player))
            {
                if (target is RemoteProp remoteProp)
                {
                    AntiCheatPlugin.LogInfo(player, $"({remoteProp.itemProperties.itemName})GrabbableObject.ActivateItemServerRpc");

                    var canUse = CooldownManager.CheckCooldown("ShipLight", player);
                    if (!canUse)
                    {
                        UnityEngine.Object.FindFirstObjectByType<ShipLights>().SetShipLightsClientRpc(true);
                    }

                    return canUse;
                }
            }
            else if (player == null)
            {
                return false;
            }

            return target != null;
        }
    }
}
