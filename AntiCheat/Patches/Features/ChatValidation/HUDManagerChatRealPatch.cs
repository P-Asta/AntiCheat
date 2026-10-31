using AntiCheat.Utils;

using HarmonyLib;

using System;
using System.Collections.Generic;

using Unity.Netcode;

namespace AntiCheat.Patches
{
    [HarmonyPatch(typeof(HUDManager))]
    [HarmonyWrapSafe]
    public static class HUDManagerChatRealPatch
    {
        private const string RpcName = "HUDManager.AddPlayerChatMessageServerRpc";

        [HarmonyPatch(typeof(HUDManager), "__rpc_handler_2930587515")]
        [HarmonyPrefix]
        [HarmonyWrapSafe]
        public static bool AddPlayerChatMessageServerRpc(NetworkBehaviour target, FastBufferReader reader, __RpcParams rpcParams)
        {
            if (PatchHelper.Check(rpcParams, out var player))
            {
                bool validate = RpcGuard.Enabled;
                if (!TryReadMessage(reader, out string chatMessage, out int playerId, out string reason))
                {
                    return validate ? RpcGuard.Reject(player, RpcName, reason) : false;
                }

                var players = StartOfRound.Instance?.allPlayerScripts;
                if (players == null)
                {
                    return false;
                }
                if (playerId < 0 || playerId >= players.Length)
                {
                    return validate ? RpcGuard.Reject(player, RpcName, "RpcInvalidIndex") : false;
                }
                var claimedPlayer = players[playerId];
                if (claimedPlayer == null)
                {
                    return false;
                }
                // Feature switches control identity enforcement and reporting. Unreadable
                // requests must never reach native parsing or its client-side array access.
                if (!validate && !PluginConfig.ChatReal.Value)
                {
                    return true;
                }
                if (claimedPlayer != player)
                {
                    if (validate)
                    {
                        return RpcGuard.Reject(player, RpcName, "RpcChatIdentity",
                            PluginConfig.ChatReal.Value && PluginConfig.ChatReal2.Value);
                    }
                    PatchHelper.ShowMessage(PatchHelper.locale.Msg_GetString("ChatReal", new Dictionary<string, string>()
                    {
                        { "{player}", player.playerUsername },
                        { "{player2}", claimedPlayer.playerUsername },
                    }));
                    if (PluginConfig.ChatReal2.Value)
                    {
                        PatchHelper.KickPlayer(player);
                    }
                    return false;
                }

                PatchHelper.LogInfo(player, RpcName, $"chatMessage:{chatMessage}");
                if (!PluginConfig.ChatReal.Value)
                {
                    return true;
                }
                bool allowed = CooldownManager.CheckCooldown("Chat", player);
                if (allowed && PatchHelper.locale.current_language == "zh_CN")
                {
                    AccessTools.DeclaredMethod(typeof(HUDManager), "AddPlayerChatMessageClientRpc").Invoke(
                        HUDManager.Instance, new object[] { chatMessage, playerId });
                    return false;
                }
                return allowed;
            }
            else if (player == null)
            {
                return false;
            }

            return true;
        }

        private static bool TryReadMessage(FastBufferReader reader, out string message, out int playerId, out string reason)
        {
            int position = reader.Position;
            message = null;
            playerId = -1;
            reason = "RpcMalformedPayload";
            try
            {
                reader.ReadValueSafe(out bool hasMessage, default);
                if (!hasMessage)
                {
                    reason = "RpcInvalidChat";
                    return false;
                }

                // The native server allows at most 50 UTF-16 characters. Validate the wire
                // length before the string reader casts/multiplies it or allocates memory.
                int stringPosition = reader.Position;
                reader.ReadValueSafe(out uint characters, default);
                if (characters > 50)
                {
                    reason = "RpcInvalidChat";
                    return false;
                }
                if ((long)characters * sizeof(char) > reader.Length - reader.Position)
                {
                    return false;
                }
                reader.Seek(stringPosition);
                reader.ReadValueSafe(out message, false);
                // NGO's int decoder accepts reserved size markers as zero; reject them
                // explicitly, and bound the entire value before its unsafe first-byte peek.
                int actorPosition = reader.Position;
                reader.ReadValueSafe(out byte marker, default);
                reader.Seek(actorPosition);
                int byteCount = marker & 7;
                if (byteCount < 1 || byteCount > 5 || !reader.TryBeginRead(byteCount))
                {
                    return false;
                }
                ByteUnpacker.ReadValueBitPacked(reader, out playerId);
                return true;
            }
            catch (Exception exception) when (exception is OverflowException || exception is ArgumentException || exception is InvalidOperationException)
            {
                return false;
            }
            finally
            {
                reader.Seek(position);
            }
        }
    }
}
