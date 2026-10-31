using HarmonyLib;

using System;
using System.Collections.Generic;
using System.Linq;

using Unity.Netcode;

namespace AntiCheat.Patches
{
    [HarmonyPatch(typeof(Terminal))]
    [HarmonyWrapSafe]
    public static class TerminalTransactionPatch
    {
        [HarmonyPatch("__rpc_handler_4003509079")]
        [HarmonyPrefix]
        public static bool BuyItemsServerRpc(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
        {
            if (PatchHelper.Check(rpcParams, out var player))
            {
                if (PluginConfig.RemoteTerminal.Value &&
                    !PatchHelper.CheckRemoteTerminal(player, "Terminal.BuyItemsServerRpc"))
                {
                    return false;
                }
                int[] boughtItems;
                int newGroupCredits, numItemsInShip;
                int originalPosition = reader.Position;
                try
                {
                    reader.ReadValueSafe(out bool hasItems, default);
                    if (!hasItems)
                    {
                        return RejectUnsafe(player, "Terminal.BuyItemsServerRpc", "RpcMalformedPayload");
                    }

                    // NGO allocates the array before checking its body. Validate the raw
                    // element count first; vanilla's server accepts at most twelve items.
                    int arrayPosition = reader.Position;
                    reader.ReadValueSafe(out int itemCount, default);
                    if (itemCount < 0 || itemCount > 12 || itemCount > (reader.Length - reader.Position) / sizeof(int))
                    {
                        return RejectUnsafe(player, "Terminal.BuyItemsServerRpc", "RpcMalformedPayload");
                    }
                    reader.Seek(arrayPosition);
                    reader.ReadValueSafe(out boughtItems, default);
                    if (!TryReadPackedInt(reader, out newGroupCredits))
                    {
                        return RejectUnsafe(player, "Terminal.BuyItemsServerRpc", "RpcMalformedPayload");
                    }
                    if (!TryReadPackedInt(reader, out numItemsInShip))
                    {
                        return RejectUnsafe(player, "Terminal.BuyItemsServerRpc", "RpcMalformedPayload");
                    }
                }
                catch (Exception exception) when (exception is OverflowException || exception is ArgumentException || exception is InvalidOperationException)
                {
                    return RejectUnsafe(player, "Terminal.BuyItemsServerRpc", "RpcMalformedPayload");
                }
                finally
                {
                    reader.Seek(originalPosition);
                }

                var terminal = (Terminal)target;
                if (terminal.buyableItemsList == null)
                {
                    return false;
                }
                foreach (int itemId in boughtItems)
                {
                    if (itemId < 0 || itemId >= terminal.buyableItemsList.Length)
                    {
                        return RejectUnsafe(player, "Terminal.BuyItemsServerRpc", "RpcInvalidIndex");
                    }
                    if (terminal.buyableItemsList[itemId] == null)
                    {
                        return false;
                    }
                }
                if (RpcGuard.Enabled && newGroupCredits < 0)
                {
                    return RpcGuard.Reject(player, "Terminal.BuyItemsServerRpc", "RpcInvalidCredits");
                }
                // -1 tells native SyncGroupCreditsClientRpc to preserve its current count.
                if (RpcGuard.Enabled && numItemsInShip < -1)
                {
                    return RpcGuard.Reject(player, "Terminal.BuyItemsServerRpc", "RpcInvalidCount");
                }
                if (PluginConfig.FreeBuy.Value)
                {
                    bool allItemsFree = boughtItems.GroupBy(id => id).All(group =>
                    {
                        int id = group.Key;
                        int price = terminal.buyableItemsList[id].creditsWorth;
                        if (price == 0) return true;
                        if (terminal.itemSalesPercentages == null || id >= terminal.itemSalesPercentages.Length) return false;
                        // Native rounds only after multiplying the discounted unit price by
                        // the requested quantity: 1 x 50% x 1 is free; x 2 costs one credit.
                        float totalPrice = price * (terminal.itemSalesPercentages[id] / 100f) * group.Count();
                        return totalPrice >= 0f && totalPrice < 1f;
                    });
                    if (!allItemsFree && (PatchHelper.Money == newGroupCredits || PatchHelper.Money == 0))
                    {
                        PatchHelper.ShowMessage(PatchHelper.locale.Msg_GetString("FreeBuy_Item", new Dictionary<string, string>()
                        {
                            { "{player}", player.playerUsername },
                            { "{items}", string.Join(",", boughtItems.GroupBy(x => terminal.buyableItemsList[x].itemName).Select(g => g.Count() == 1 ? g.Key : $"{g.Key}*{g.Count()}")) }
                        }));
                        if (PluginConfig.FreeBuy_Kick.Value)
                        {
                            PatchHelper.KickPlayer(player);
                        }
                        return false;
                    }
                    else if (newGroupCredits > PatchHelper.Money || PatchHelper.Money < 0)
                    {
                        PatchHelper.ShowMessage(PatchHelper.locale.Msg_GetString("FreeBuy_SetMoney", new Dictionary<string, string>()
                        {
                            { "{player}", player.playerUsername },
                            { "{Money}", (newGroupCredits - PatchHelper.Money).ToString() }
                        }));
                        if (PluginConfig.FreeBuy_Kick.Value)
                        {
                            PatchHelper.KickPlayer(player);
                        }
                        return false;
                    }
                }
                if (PluginConfig.OperationLog.Value)
                {
                    PatchHelper.ShowMessageHostOnly(PatchHelper.locale.OperationLog_GetString("BuyItem", new Dictionary<string, string>()
                    {
                        { "{player}", player.playerUsername },
                        { "{items}", string.Join(",", boughtItems.GroupBy(x => terminal.buyableItemsList[x].itemName).Select(g => g.Count() == 1 ? g.Key : $"{g.Key}*{g.Count()}")) }
                    }));
                }
            }
            else if (player == null)
            {
                return false;
            }
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch("__rpc_handler_2452398197")]
        public static bool BuyVehicleServerRpc(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
        {
            if (PatchHelper.Check(rpcParams, out var player))
            {
                if (PluginConfig.RemoteTerminal.Value &&
                    !PatchHelper.CheckRemoteTerminal(player, "Terminal.BuyVehicleServerRpc"))
                {
                    return false;
                }
                int vehicleID, newGroupCredits;
                bool useWarranty;
                int originalPosition = reader.Position;
                try
                {
                    if (!TryReadPackedInt(reader, out vehicleID))
                    {
                        return RejectUnsafe(player, "Terminal.BuyVehicleServerRpc", "RpcMalformedPayload");
                    }
                    if (!TryReadPackedInt(reader, out newGroupCredits))
                    {
                        return RejectUnsafe(player, "Terminal.BuyVehicleServerRpc", "RpcMalformedPayload");
                    }
                    reader.ReadValueSafe(out useWarranty, default);
                }
                catch (Exception exception) when (exception is OverflowException || exception is ArgumentException || exception is InvalidOperationException)
                {
                    return RejectUnsafe(player, "Terminal.BuyVehicleServerRpc", "RpcMalformedPayload");
                }
                finally
                {
                    reader.Seek(originalPosition);
                }
                var terminal = (Terminal)target;
                if (terminal.buyableVehicles == null)
                {
                    return false;
                }
                if (vehicleID < 0 || vehicleID >= terminal.buyableVehicles.Length)
                {
                    return RejectUnsafe(player, "Terminal.BuyVehicleServerRpc", "RpcInvalidIndex");
                }
                if (RpcGuard.Enabled && newGroupCredits < 0)
                {
                    return RpcGuard.Reject(player, "Terminal.BuyVehicleServerRpc", "RpcInvalidCredits");
                }
                if (PluginConfig.FreeBuy.Value)
                {
                    if (!(useWarranty && (bool)AccessTools.DeclaredField(typeof(Terminal), "hasWarrantyTicket").GetValue(target)))
                    {
                        if (VehiclePurchaseCostsCredits(terminal, vehicleID) &&
                            (PatchHelper.Money == newGroupCredits || PatchHelper.Money == 0))
                        {
                            PatchHelper.ShowMessage(PatchHelper.locale.Msg_GetString("FreeBuy_Item", new Dictionary<string, string>()
                            {
                                { "{player}", player.playerUsername },
                                { "{items}", PatchHelper.locale.Item_GetString("Cruiser") }
                            }));
                            if (PluginConfig.FreeBuy_Kick.Value)
                            {
                                PatchHelper.KickPlayer(player);
                            }
                            return false;
                        }
                        else if (newGroupCredits > PatchHelper.Money || PatchHelper.Money < 0)
                        {
                            PatchHelper.ShowMessage(PatchHelper.locale.Msg_GetString("FreeBuy_SetMoney", new Dictionary<string, string>()
                            {
                                { "{player}", player.playerUsername },
                                { "{Money}", (newGroupCredits - PatchHelper.Money).ToString() }
                            }));
                            if (PluginConfig.FreeBuy_Kick.Value)
                            {
                                PatchHelper.KickPlayer(player);
                            }
                            return false;
                        }
                    }
                }
                if (PluginConfig.OperationLog.Value)
                {
                    PatchHelper.ShowMessageHostOnly(PatchHelper.locale.OperationLog_GetString("BuyItem", new Dictionary<string, string>()
                    {
                        { "{player}", player.playerUsername },
                        { "{items}", PatchHelper.locale.Item_GetString("Cruiser") }
                    }));
                }
            }
            else if (player == null)
            {
                return false;
            }
            return true;
        }

        internal static bool TryReadPackedInt(FastBufferReader reader, out int value)
        {
            value = 0;
            if (!reader.TryBeginRead(1)) return false;
            int position = reader.Position;
            reader.ReadValueSafe(out byte firstByte, default);
            reader.Seek(position);
            int encodedSize = firstByte & 7;
            // Native uint/int decoding silently returns zero for sizes 0, 6 and 7.
            // Reject those headers and truncated bodies before its unsafe first-byte peek.
            if (encodedSize < 1 || encodedSize > 5 || !reader.TryBeginRead(encodedSize)) return false;
            ByteUnpacker.ReadValueBitPacked(reader, out value);
            return true;
        }

        private static bool VehiclePurchaseCostsCredits(Terminal terminal, int vehicleID)
        {
            // The store display's BuyableVehicle.creditsWorth is not the purchase price.
            // Native purchases use a non-confirmation TerminalNode and its sale percentage.
            var keywords = terminal.terminalNodes?.allKeywords;
            if (keywords == null || terminal.buyableItemsList == null || terminal.itemSalesPercentages == null ||
                vehicleID >= terminal.itemSalesPercentages.Length - terminal.buyableItemsList.Length)
            {
                return false;
            }
            int percentage = terminal.itemSalesPercentages[terminal.buyableItemsList.Length + vehicleID];
            var pending = new Stack<TerminalNode>();
            var visited = new HashSet<TerminalNode>();
            foreach (var keyword in keywords)
            {
                if (keyword?.compatibleNouns == null) continue;
                foreach (var noun in keyword.compatibleNouns)
                {
                    if (noun?.result != null) pending.Push(noun.result);
                }
            }
            bool foundPaidPurchase = false;
            while (pending.Count > 0)
            {
                var node = pending.Pop();
                if (!visited.Add(node)) continue;
                if (!node.isConfirmationNode && node.buyVehicleIndex == vehicleID)
                {
                    float discountedPrice = node.itemCost * (percentage / 100f);
                    // Native casts this price to int. A free path or an unknown/out-of-range
                    // price cannot establish that unchanged credits prove a free-buy exploit.
                    if (discountedPrice < 1f || discountedPrice > int.MaxValue) return false;
                    foundPaidPurchase = true;
                }
                if (node.terminalOptions == null) continue;
                foreach (var option in node.terminalOptions)
                {
                    if (option?.result != null) pending.Push(option.result);
                }
            }
            return foundPaidPurchase;
        }

        private static bool RejectUnsafe(GameNetcodeStuff.PlayerControllerB player, string rpc, string reason)
        {
            // Invalid payloads cannot safely reach native indexing even when alerts are disabled.
            return RpcGuard.Enabled ? RpcGuard.Reject(player, rpc, reason) : false;
        }
    }
}
