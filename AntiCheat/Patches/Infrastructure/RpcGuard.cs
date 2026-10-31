using GameNetcodeStuff;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace AntiCheat.Patches
{
    internal static class RpcGuard
    {
        internal static bool Enabled => PluginConfig.RpcValidation?.Value ?? false;

        internal static bool Reject(PlayerControllerB player, string rpc, string reasonKey, bool legacyKick = false)
        {
            if (!Enabled || player == null)
                return false;

            try
            {
                // Only the transport-authenticated player is supplied by the caller. Do not echo payload data.
                var name = new StringBuilder();
                foreach (char character in player.playerUsername ?? string.Empty)
                {
                    if (name.Length >= 64) break;
                    if (char.GetUnicodeCategory(character) == UnicodeCategory.Format) continue;
                    name.Append(char.IsControl(character) ? ' ' : character == '<' ? '‹' : character == '>' ? '›' : character);
                }
                string message = PatchHelper.locale.Msg_GetString("RpcRejected", new Dictionary<string, string>
                {
                    { "{player}", name.ToString() },
                    { "{id}", player.actualClientId.ToString() },
                    { "{rpc}", rpc },
                    { "{reason}", PatchHelper.locale.Msg_GetString(reasonKey) }
                });
                PatchHelper.ShowMessage(message, $"invalid-rpc:{player.actualClientId}:{rpc}:{reasonKey}");
            }
            catch (Exception exception)
            {
                AntiCheatPlugin.Log?.LogWarning($"Rejected {rpc}: notification failed ({exception.GetType().Name}).");
            }
            if (PluginConfig.RpcValidation.Kick || legacyKick)
            {
                try { PatchHelper.KickPlayer(player); }
                catch (Exception exception)
                {
                    AntiCheatPlugin.Log?.LogWarning($"Rejected {rpc}: disconnect failed ({exception.GetType().Name}).");
                }
            }
            // UI/disconnect failures must not escape into HarmonyWrapSafe and allow native execution.
            return false;
        }
    }
}
