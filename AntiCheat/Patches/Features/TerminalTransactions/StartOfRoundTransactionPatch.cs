using HarmonyLib;

using System;
using System.Collections.Generic;
using System.Linq;

using Unity.Netcode;

using static AntiCheat.Patches.PatchHelper;

namespace AntiCheat.Patches
{
    [HarmonyPatch(typeof(StartOfRound))]
    [HarmonyWrapSafe]
    public static class StartOfRoundTransactionPatch
    {
        [HarmonyPatch("__rpc_handler_1134466287")]
        [HarmonyPrefix]
        [HarmonyWrapSafe]
        public static bool ChangeLevelServerRpc(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
        {
            if (Check(rpcParams, out var player))
            {
                if (PluginConfig.RemoteTerminal.Value &&
                    !CheckRemoteTerminal(player, "StartOfRound.ChangeLevelServerRpc"))
                {
                    return false;
                }

                if (!TryReadTransaction(reader, out int levelID, out int newGroupCreditsAmount))
                {
                    return RejectUnsafe(player, "StartOfRound.ChangeLevelServerRpc", "RpcMalformedPayload");
                }
                var round = (StartOfRound)target;
                if (round.levels == null)
                {
                    return false;
                }
                if (levelID < 0 || levelID >= round.levels.Length)
                {
                    return RejectUnsafe(player, "StartOfRound.ChangeLevelServerRpc", "RpcInvalidIndex");
                }
                if (round.levels[levelID] == null)
                {
                    return false;
                }
                if (RpcGuard.Enabled && newGroupCreditsAmount < 0)
                {
                    return RpcGuard.Reject(player, "StartOfRound.ChangeLevelServerRpc", "RpcInvalidCredits");
                }
                if (PluginConfig.FreeBuy.Value)
                {
                    if (newGroupCreditsAmount > Money || Money < 0)
                    {
                        ShowMessage(locale.Msg_GetString("FreeBuy_SetMoney", new Dictionary<string, string>()
                        {
                            { "{player}", player.playerUsername },
                            { "{Money}", (newGroupCreditsAmount - Money).ToString() }
                        }));
                        if (PluginConfig.FreeBuy2.Value)
                        {
                            KickPlayer(player);
                        }
                        return false;
                    }
                    var route = terminal?.terminalNodes?.allKeywords?.FirstOrDefault(x =>
                        x != null && string.Equals(x.word, "route", StringComparison.OrdinalIgnoreCase));
                    var level = round.levels[levelID].PlanetName?.Split(' ')[0];
                    var destination = route?.compatibleNouns?.FirstOrDefault(x => x?.result != null &&
                        (x.result.buyRerouteToMoon == levelID || x.result.name == level + "route"))?.result;
                    if (destination != null)
                    {
                        int itemCost = destination.itemCost;
                        level = round.currentLevel?.PlanetName?.Split(' ')[0];
                        int currentLevelID = Array.IndexOf(round.levels, round.currentLevel);
                        var currentRoute = route.compatibleNouns.FirstOrDefault(x => x?.result != null &&
                            ((currentLevelID >= 0 && x.result.buyRerouteToMoon == currentLevelID) || x.result.name == level + "route"))?.result;
                        if (PluginConfig.ShipSetting_ChangToFreeMoon.Value && itemCost == 0 && currentRoute != null && currentRoute.itemCost != 0)
                        {
                            ShowMessage(locale.Msg_GetString("ChangeToFreeLevel", new Dictionary<string, string>()
                            {
                                { "{player}", player.playerUsername }
                            }));
                            return false;
                        }
                        LogInfo(player, "StartOfRound.ChangeLevelServerRpc", $"levelID:{levelID}", $"itemCost:{itemCost}");
                        if (itemCost != 0)
                        {
                            int newValue = Money - itemCost;
                            if (newValue != newGroupCreditsAmount || Money == 0)
                            {
                                ShowMessage(locale.Msg_GetString("FreeBuy_Level", new Dictionary<string, string>()
                                {
                                    { "{player}", player.playerUsername }
                                }));
                                if (PluginConfig.FreeBuy2.Value)
                                {
                                    KickPlayer(player);
                                }
                                return false;
                            }
                        }
                    }
                }
                if (PluginConfig.OperationLog.Value)
                {
                    ShowMessageHostOnly(locale.OperationLog_GetString("ChangeLevel", new Dictionary<string, string>()
                    {
                        { "{player}", player.playerUsername },
                        { "{planet}", round.levels[levelID].PlanetName }
                    }));
                }
            }
            else if (player == null)
            {
                return false;
            }
            return true;
        }

        [HarmonyPatch("__rpc_handler_3953483456")]
        [HarmonyPrefix]
        [HarmonyWrapSafe]
        public static bool BuyShipUnlockableServerRpc(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
        {
            if (Check(rpcParams, out var player))
            {
                if (PluginConfig.RemoteTerminal.Value &&
                    !CheckRemoteTerminal(player, "StartOfRound.BuyShipUnlockableServerRpc"))
                {
                    return false;
                }
                if (!TryReadTransaction(reader, out int unlockableID, out int newGroupCreditsAmount))
                {
                    return RejectUnsafe(player, "StartOfRound.BuyShipUnlockableServerRpc", "RpcMalformedPayload");
                }
                var unlockables = ((StartOfRound)target).unlockablesList?.unlockables;
                if (unlockables == null)
                {
                    return false;
                }
                if (unlockableID < 0 || unlockableID >= unlockables.Count)
                {
                    return RejectUnsafe(player, "StartOfRound.BuyShipUnlockableServerRpc", "RpcInvalidIndex");
                }
                var unlockable = unlockables[unlockableID];
                if (unlockable == null)
                {
                    return false;
                }
                if (RpcGuard.Enabled && newGroupCreditsAmount < 0)
                {
                    return RpcGuard.Reject(player, "StartOfRound.BuyShipUnlockableServerRpc", "RpcInvalidCredits");
                }
                if (PluginConfig.FreeBuy.Value)
                {
                    bool isFree = unlockable.shopSelectionNode != null && unlockable.shopSelectionNode.itemCost == 0;
                    if (!isFree && (Money == newGroupCreditsAmount || Money == 0))
                    {
                        ShowMessage(locale.Msg_GetString("FreeBuy_unlockable", new Dictionary<string, string>()
                        {
                            { "{player}", player.playerUsername }
                        }));
                        if (PluginConfig.FreeBuy2.Value)
                        {
                            KickPlayer(player);
                        }
                        return false;
                    }
                    else if (newGroupCreditsAmount > Money || Money < 0)
                    {
                        ShowMessage(locale.Msg_GetString("FreeBuy_SetMoney", new Dictionary<string, string>()
                        {
                            { "{player}", player.playerUsername },
                            { "{Money}", (newGroupCreditsAmount - Money).ToString() }
                        }));
                        if (PluginConfig.FreeBuy2.Value)
                        {
                            KickPlayer(player);
                        }
                        return false;
                    }
                }
                if (PluginConfig.OperationLog.Value)
                {
                    ShowMessageHostOnly(locale.OperationLog_GetString("BuyShipUnlockable", new Dictionary<string, string>()
                    {
                        { "{player}", player.playerUsername },
                        { "{unlockable}", unlockable.unlockableName }
                    }));
                }
            }
            else if (player == null)
            {
                return false;
            }
            return true;
        }

        private static bool TryReadTransaction(FastBufferReader reader, out int id, out int credits)
        {
            int originalPosition = reader.Position;
            id = credits = 0;
            try
            {
                if (!TerminalTransactionPatch.TryReadPackedInt(reader, out id))
                {
                    return false;
                }
                if (!TerminalTransactionPatch.TryReadPackedInt(reader, out credits))
                {
                    return false;
                }
                return true;
            }
            catch (Exception exception) when (exception is OverflowException || exception is ArgumentException || exception is InvalidOperationException)
            {
                return false;
            }
            finally
            {
                reader.Seek(originalPosition);
            }
        }

        private static bool RejectUnsafe(GameNetcodeStuff.PlayerControllerB player, string rpc, string reason)
        {
            return RpcGuard.Enabled ? RpcGuard.Reject(player, rpc, reason) : false;
        }

        [HarmonyPatch("BuyShipUnlockableClientRpc")]
        [HarmonyPrefix]
        [HarmonyWrapSafe]
        public static bool BuyShipUnlockableClientRpc(int newGroupCreditsAmount, int unlockableID = -1)
        {
            if (!StartOfRound.Instance.localPlayerController.IsHost)
            {
                return true;
            }
            Money = newGroupCreditsAmount;
            return true;
        }

        [HarmonyPatch("ChangeLevelClientRpc")]
        [HarmonyPrefix]
        [HarmonyWrapSafe]
        public static bool ChangeLevelClientRpc(int levelID, int newGroupCreditsAmount)
        {
            if (!StartOfRound.Instance.localPlayerController.IsHost)
            {
                return true;
            }
            Money = newGroupCreditsAmount;
            return true;
        }
    }
}
