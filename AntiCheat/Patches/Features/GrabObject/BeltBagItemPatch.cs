using HarmonyLib;

using System.Collections.Generic;

using Unity.Netcode;

using static AntiCheat.Patches.PatchHelper;

namespace AntiCheat.Patches
{
    [HarmonyPatch(typeof(BeltBagItem))]
    [HarmonyWrapSafe]
    public static class BeltBagItemPatch
    {


        /// <summary>
        /// 添加物品到腰包事件
        /// Prefix BeltBagItem.TryAddObjectToBagServerRpc
        /// </summary>
        [HarmonyPatch("__rpc_handler_2988305002")]
        [HarmonyPrefix]
        [HarmonyWrapSafe]
        public static bool __rpc_handler_2988305002(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
        {
            if (!Check(rpcParams, out var player))
            {
                return player != null;
            }
            if (!PluginConfig.GrabObject.Value)
            {
                return true;
            }

            NetworkObjectReference netObjectRef;
            int playerWhoAdded;
            int readerPosition = reader.Position;
            try
            {
                reader.ReadValueSafe(out netObjectRef, default);
                ByteUnpacker.ReadValueBitPacked(reader, out playerWhoAdded);
            }
            finally
            {
                reader.Seek(readerPosition);
            }

            int senderPlayerIndex = (int)player.playerClientId;
            if (playerWhoAdded != senderPlayerIndex)
            {
                // The native RPC permits non-owners, but uses this index for cancellation.
                ((BeltBagItem)target).CancelAddObjectToBagClientRpc(senderPlayerIndex);
                return false;
            }

            if (!netObjectRef.TryGet(out NetworkObject networkObject, null) || networkObject == null)
            {
                return true;
            }

            GrabbableObject component = networkObject.GetComponent<GrabbableObject>();
            // Native validation cancels missing objects and ordinary pickup/bag contention.
            if (component == null || component.isHeld || component.isHeldByEnemy || component.heldByPlayerOnServer)
            {
                return true;
            }
            if (component.itemProperties == null)
            {
                ((BeltBagItem)target).CancelAddObjectToBagClientRpc(senderPlayerIndex);
                return false;
            }

            if (!component.itemProperties.isScrap && component.itemProperties.itemId != 123984 && component.itemProperties.itemId != 819501)
            {
                return true;
            }
            if (PluginConfig.GrabObject_SendLog.Value)
            {
                ShowMessage(locale.Msg_GetString("GrabObject_BeltBag", new Dictionary<string, string>()
                {
                    { "{player}", player.playerUsername },
                    { "{itemName}", component.itemProperties.itemName }
                }));
            }

            if (PluginConfig.GrabObject_BeltBag.Value)
            {
                ((BeltBagItem)target).CancelAddObjectToBagClientRpc(senderPlayerIndex);
                return false;
            }
            return true;
        }
    }
}
