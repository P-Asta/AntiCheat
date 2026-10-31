using GameNetcodeStuff;

using HarmonyLib;

using System.Collections.Generic;
using System.Reflection;

using Unity.Netcode;
using UnityEngine;

using static AntiCheat.Patches.PatchHelper;

namespace AntiCheat.Patches
{
    [HarmonyPatch(typeof(PlayerControllerB))]
    [HarmonyWrapSafe]
    public static class PlayerControllerGrabObjectPatch
    {
        // v80+ routes eligible tools to the separate utility slot (50).
        // Resolve the new overload at runtime to retain compatibility with v73.
        private static readonly MethodInfo FirstEmptyItemSlotWithItem = AccessTools.Method(
            typeof(PlayerControllerB), "FirstEmptyItemSlot", new[] { typeof(GrabbableObject) });
        private static readonly FieldInfo UtilitySlot = AccessTools.Field(typeof(PlayerControllerB), "ItemOnlySlot");

        [HarmonyPatch("__rpc_handler_412259855")]
        [HarmonyPrefix]
        [HarmonyWrapSafe]
        public static bool SwitchItemSlotsServerRpc(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
        {
            if (rpcParams.Server.Receive.SenderClientId != target.OwnerClientId)
            {
                return true; // Let the native handler reject requests for someone else's player.
            }
            if (Check(rpcParams, out var player))
            {
                if (target == player && PluginConfig.GrabObject.Value && PluginConfig.GrabObject_TwoHand.Value &&
                    IsHoldingTwoHandedItem(player))
                {
                    return false;
                }

                return true;
            }
            else if (player == null)
            {
                return false;
            }
            return true;
        }

        [HarmonyPatch("__rpc_handler_1554282707")]
        [HarmonyPrefix]
        [HarmonyWrapSafe]
        public static bool GrabObjectServerRpc(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
        {
            if (rpcParams.Server.Receive.SenderClientId != target.OwnerClientId)
            {
                return true;
            }
            if (Check(rpcParams, out var player))
            {
                if (target == player && PluginConfig.GrabObject.Value)
                {
                    var position = reader.Position;
                    NetworkObjectReference grabbedObject;
                    try
                    {
                        reader.ReadValueSafe(out grabbedObject, default);
                    }
                    finally
                    {
                        reader.Seek(position);
                    }
                    if (grabbedObject.TryGet(out var networkObject, null))
                    {
                        var grabbable = networkObject.GetComponentInChildren<GrabbableObject>();
                        // Vanilla rejects a contested/already processed grab. It is not evidence of cheating.
                        if (grabbable == null || grabbable.heldByPlayerOnServer)
                        {
                            return true;
                        }
                        if (grabbable.itemProperties == null || player.ItemSlots == null)
                        {
                            player.GrabObjectClientRpc(false, grabbedObject);
                            return false;
                        }

                        var allSlotsFull = true;
                        var hasJetpack = false;
                        var hasTwoHandedWithoutJetpack = false;
                        var inventoryConsistent = true;
                        var alreadyStored = false;
                        var seenItems = new HashSet<GrabbableObject>();
                        foreach (var item in player.ItemSlots)
                        {
                            alreadyStored |= item == grabbable;
                            if (item == null)
                            {
                                allSlotsFull = false;
                            }
                            else if (!IsOwnedInventoryItem(player, item) || item == grabbable || !seenItems.Add(item))
                            {
                                inventoryConsistent = false;
                            }
                            else
                            {
                                hasJetpack |= item is JetpackItem;
                                hasTwoHandedWithoutJetpack |= item.itemProperties.twoHanded && !(item is JetpackItem);
                            }
                        }
                        var utilityItem = UtilitySlot?.GetValue(player) as GrabbableObject;
                        if (alreadyStored || utilityItem == grabbable)
                        {
                            player.GrabObjectClientRpc(false, grabbedObject);
                            return false;
                        }
                        if (utilityItem != null && (!IsOwnedInventoryItem(player, utilityItem) ||
                            utilityItem == grabbable || !seenItems.Add(utilityItem)))
                        {
                            inventoryConsistent = false;
                        }
                        var regularSlotsFull = allSlotsFull;
                        if (FirstEmptyItemSlotWithItem != null)
                        {
                            try
                            {
                                allSlotsFull = (int)FirstEmptyItemSlotWithItem.Invoke(player, new object[] { grabbable }) < 0;
                            }
                            catch (TargetInvocationException ex)
                            {
                                LogInfo(player, "PlayerControllerB.GrabObjectServerRpc", $"Slot lookup failed: {ex.InnerException?.Message}");
                                // Vanilla would repeat the failing lookup after changing object ownership.
                                player.GrabObjectClientRpc(false, grabbedObject);
                                return false;
                            }
                        }

                        LogInfo(player, "PlayerControllerB.GrabObjectServerRpc", $"itemName:{grabbable.itemProperties.itemName}",
                            $"full:{allSlotsFull}", $"inventoryConsistent:{inventoryConsistent}",
                            $"slot:{player.currentItemSlot}", $"utilityOccupied:{utilityItem != null}",
                            $"Distance:{Vector3.Distance(player.transform.position, grabbable.transform.position)}");
                        bool ban = false;

                        var heldItemName = player.currentlyHeldObjectServer?.itemProperties?.itemName ?? "unknown";
                        // A one-handed jetpack can be pocketed normally. Its presence proves nothing.
                        var jetpackTwoHandGrabSuppressed = hasJetpack &&
                            grabbable.itemProperties.twoHanded && !hasTwoHandedWithoutJetpack;
                        var twoHandDetected = inventoryConsistent &&
                            (IsHoldingTwoHandedItem(player) ||
                             (grabbable.itemProperties.twoHanded && hasTwoHandedWithoutJetpack));
                        var twoHandBlocked = twoHandDetected && PluginConfig.GrabObject_TwoHand.Value;

                        if (twoHandDetected)
                        {
                            if (PluginConfig.GrabObject_SendLog.Value && !jetpackTwoHandGrabSuppressed)
                            {
                                ShowMessage(locale.Msg_GetString("GrabObject_TwoHand", new System.Collections.Generic.Dictionary<string, string>()
                                {
                                    { "{player}", player.playerUsername },
                                    { "{heldItemName}", heldItemName },
                                    { "{itemName}", grabbable.itemProperties.itemName },
                                    { "{hasTwoHand}", hasTwoHandedWithoutJetpack.ToString() },
                                    { "{jetpack}", hasJetpack.ToString() }
                                }));
                            }

                            if (twoHandBlocked)
                            {
                                ban = true;
                            }
                        }

                        // A contradictory snapshot is not a confirmed overflow.
                        // Reject safely without accusing/kicking while native routing has no space.
                        var moreSlotDetected = allSlotsFull && regularSlotsFull && inventoryConsistent && !ban;
                        if (moreSlotDetected && PluginConfig.GrabObject_SendLog.Value)
                        {
                            ShowMessage(locale.Msg_GetString("GrabObject_MoreSlot", new System.Collections.Generic.Dictionary<string, string>()
                            {
                                { "{player}", player.playerUsername },
                                { "{itemName}", grabbable.itemProperties.itemName }
                            }));
                        }

                        if (PluginConfig.GrabObject_MoreSlot.Value && !ban)
                        {
                            ban = allSlotsFull;
                        }

                        if (ban)
                        {
                            player.GrabObjectClientRpc(false, grabbedObject);
                            if (PluginConfig.GrabObject_Kick.Value && (twoHandBlocked || moreSlotDetected))
                            {
                                KickPlayer(player);
                            }
                            return false;
                        }

                        var syncedWorldPosition = player.transform.parent != null
                            ? player.transform.parent.TransformPoint(player.serverPlayerPosition)
                            : player.serverPlayerPosition;
                        if (Vector3.Distance(grabbable.transform.position, syncedWorldPosition) > 100 &&
                            Vector3.Distance(grabbable.transform.position, player.transform.position) > 100 &&
                            !StartOfRound.Instance.shipIsLeaving &&
                            StartOfRound.Instance.shipHasLanded)
                        {
                            if (player.teleportedLastFrame)
                            {
                                return true;
                            }

                            if (PluginConfig.GrabObject_SendLog.Value)
                            {
                                ShowMessage(locale.Msg_GetString("GrabObject", new System.Collections.Generic.Dictionary<string, string>()
                                {
                                    { "{player}", player.playerUsername },
                                    { "{object_position}", grabbable.transform.position.ToString() },
                                    { "{player_position}", syncedWorldPosition.ToString() }
                                }));
                            }

                            player.GrabObjectClientRpc(false, grabbedObject);
                            if (PluginConfig.GrabObject_Kick.Value)
                            {
                                KickPlayer(player);
                            }

                            return false;
                        }
                    }
                }
            }
            else if (player == null)
            {
                return false;
            }
            return true;
        }

        private static bool IsOwnedInventoryItem(PlayerControllerB player, GrabbableObject item)
        {
            return item != null && item.itemProperties != null && item.heldByPlayerOnServer && item.playerHeldBy == player;
        }

        private static bool IsHoldingTwoHandedItem(PlayerControllerB player)
        {
            var held = player.currentlyHeldObjectServer;
            if (!player.twoHanded || !IsOwnedInventoryItem(player, held) || !held.itemProperties.twoHanded)
            {
                return false;
            }
            if (player.currentItemSlot == 50)
            {
                return UtilitySlot?.GetValue(player) as GrabbableObject == held;
            }
            return player.ItemSlots != null && player.currentItemSlot >= 0 && player.currentItemSlot < player.ItemSlots.Length &&
                player.ItemSlots[player.currentItemSlot] == held;
        }
    }
}
