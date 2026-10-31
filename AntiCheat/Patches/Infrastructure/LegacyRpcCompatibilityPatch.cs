using GameNetcodeStuff;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Netcode;

namespace AntiCheat.Patches
{
    // These ServerRpc handlers were removed or replaced in newer game versions.
    // Keep their old validation on compatible versions without aborting PatchAll elsewhere.
    [HarmonyPatch]
    [HarmonyWrapSafe]
    internal static class LegacyRpcCompatibilityPatch
    {
        private static readonly (Type Type, string Name)[] Targets =
        {
            (typeof(EnemyAI), "__rpc_handler_255411420"),
            (typeof(PlayerControllerB), "__rpc_handler_760742013"),
            (typeof(PlayerControllerB), "__rpc_handler_4121569671"),
            (typeof(PlayerControllerB), "__rpc_handler_2013428264"),
            (typeof(GrabbableObject), "__rpc_handler_4280509730")
        };

        private static readonly List<MethodBase> AvailableTargets = FindTargets();

        // HarmonyX falls back to attribute targets when TargetMethods is empty.
        // Skip this optional class entirely in that case to avoid an undefined-target error.
        [HarmonyPrepare]
        private static bool Prepare() => AvailableTargets.Count > 0;

        [HarmonyTargetMethods]
        private static IEnumerable<MethodBase> TargetMethods() => AvailableTargets;

        private static List<MethodBase> FindTargets()
        {
            var available = new List<MethodBase>();
            foreach (var target in Targets)
            {
                var method = AccessTools.DeclaredMethod(target.Type, target.Name);
                if (method != null)
                {
                    available.Add(method);
                }
                else
                {
                    AntiCheatPlugin.Log?.LogWarning(
                        $"Compatibility: {target.Type.FullName}.{target.Name} is unavailable; " +
                        "its legacy RPC check is disabled for this game version.");
                }
            }
            return available;
        }

        [HarmonyPrefix]
        private static bool Prefix(MethodBase __originalMethod, NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
        {
            // Do not route replacement universal RPCs here: their sender semantics differ.
            switch (__originalMethod.Name)
            {
                case "__rpc_handler_255411420":
                    return EnemyAIControlPatch.UpdateEnemyPositionServerRpc(target, reader, rpcParams);
                case "__rpc_handler_760742013":
                    return PlayerControllerOperationLogPatch.DropAllHeldItemsServerRpc(target, reader, rpcParams);
                case "__rpc_handler_4121569671":
                    return PlayerControllerCombatPatch.KillPlayerServerRpc(target, reader, rpcParams);
                case "__rpc_handler_2013428264":
                    return PlayerControllerMovementPatch.UpdatePlayerPositionServerRpc(target, reader, rpcParams);
                case "__rpc_handler_4280509730":
                    return GrabbableObjectShipSystemsPatch.ActivateItemServerRpc(target, reader, rpcParams);
                default:
                    return true;
            }
        }
    }
}
