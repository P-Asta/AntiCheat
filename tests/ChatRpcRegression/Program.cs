using System;
using System.Collections.Generic;
using System.Text;
using AntiCheat;
using AntiCheat.Patches;
using AntiCheat.Utils;
using GameNetcodeStuff;
using Unity.Netcode;

static class Program
{
    static int count, failures;
    static void Run(string name, Action action)
    {
        count++;
        PatchHelper.Player = new PlayerControllerB();
        PatchHelper.CheckResult = true;
        PatchHelper.Kicks = 0;
        PatchHelper.Messages.Clear();
        PatchHelper.locale.current_language = "en_US";
        RpcGuard.Enabled = true;
        RpcGuard.Reasons.Clear();
        PluginConfig.ChatReal.Value = true;
        PluginConfig.ChatReal2.Value = false;
        CooldownManager.Allowed = true;
        CooldownManager.Calls = 0;
        HUDManager.Instance.Broadcasts.Clear();
        StartOfRound.Instance = new StartOfRound { allPlayerScripts = new[] { new PlayerControllerB(), PatchHelper.Player, new PlayerControllerB() } };
        try { action(); Console.WriteLine("PASS " + name); }
        catch (Exception e) { failures++; Console.WriteLine("FAIL " + name + ": " + e.Message); }
    }
    static byte[] Payload(string text = "hello", int actor = 1, uint? length = null, bool omitActor = false)
    {
        var bytes = new List<byte> { (byte)(text == null ? 0 : 1) };
        if (text != null)
        {
            bytes.AddRange(BitConverter.GetBytes(length ?? (uint)text.Length));
            bytes.AddRange(Encoding.Unicode.GetBytes(text));
        }
        if (!omitActor)
        {
            uint bits = (uint)((actor << 1) ^ (actor >> 31));
            int size = bits <= 0x1f ? 1 : bits <= 0x1fff ? 2 : bits <= 0x1fffff ? 3 : bits <= 0x1fffffff ? 4 : 5;
            if (size == 5) { bytes.Add(5); bytes.AddRange(BitConverter.GetBytes(bits)); }
            else { bits = (bits << 3) | (uint)size; for (int i = 0; i < size; i++) bytes.Add((byte)(bits >> (8 * i))); }
        }
        return bytes.ToArray();
    }
    static void Check(byte[] bytes, bool allowed, string reason = null, int offset = 0, int? reads = null, int kicks = 0, string legacy = null, int broadcasts = 0, int? packedReads = null)
    {
        var data = new byte[bytes.Length + offset];
        bytes.CopyTo(data, offset);
        var state = new ReaderState { Bytes = data, Position = offset };
        bool result = HUDManagerChatRealPatch.AddPlayerChatMessageServerRpc(HUDManager.Instance, new FastBufferReader { State = state }, default);
        Assert(result == allowed, $"allowed={result}; expected {allowed}");
        Assert(state.Position == offset, $"cursor={state.Position}; expected {offset}");
        Assert(string.Join(",", RpcGuard.Reasons) == (reason ?? ""), "reasons=" + string.Join(",", RpcGuard.Reasons));
        Assert(reads == null || state.StringReads == reads, $"stringReads={state.StringReads}; expected {reads}");
        Assert(packedReads == null || state.PackedReads == packedReads, $"packedReads={state.PackedReads}; expected {packedReads}");
        Assert(PatchHelper.Kicks == kicks, "legacy kicks=" + PatchHelper.Kicks);
        Assert(string.Join(",", PatchHelper.Messages) == (legacy ?? ""), "legacy messages=" + string.Join(",", PatchHelper.Messages));
        Assert(HUDManager.Instance.Broadcasts.Count == broadcasts, "broadcast count=" + HUDManager.Instance.Broadcasts.Count);
    }
    static void Assert(bool value, string message) { if (!value) throw new Exception(message); }
    static int Main()
    {
        Run("valid chat", () => Check(Payload(), true, reads: 1));
        Run("50 UTF16 characters allowed", () => Check(Payload(new string('a', 50)), true, reads: 1));
        Run("normal tag and minimap mention is not evidence", () => Check(Payload("<size=0>Tyzeron.Minimap"), true));
        Run("empty nonnull chat is vanilla-valid", () => Check(Payload(""), true));
        Run("nonzero reader position restored", () => Check(Payload(), true, offset: 7));
        Run("null chat is blocked", () => Check(Payload(null), false, "RpcInvalidChat", reads: 0));
        Run("51 characters rejected before allocation", () => Check(Payload(new string('a', 51)), false, "RpcInvalidChat", reads: 0));
        Run("huge uint length rejected before allocation", () => Check(Payload("", length: uint.MaxValue), false, "RpcInvalidChat", reads: 0));
        Run("overflowing UTF16 length rejected before allocation", () => Check(Payload("", length: 0x80000000), false, "RpcInvalidChat", reads: 0));
        Run("truncated length is rejected", () => Check(new byte[] { 1, 2 }, false, "RpcMalformedPayload", offset: 5, reads: 0));
        Run("truncated body rejected before allocation", () => Check(Payload("x", length: 50), false, "RpcMalformedPayload", reads: 0));
        Run("missing flag is rejected", () => Check(Array.Empty<byte>(), false, "RpcMalformedPayload", reads: 0));
        Run("missing actor is rejected before unsafe first-byte peek", () => Check(Payload(omitActor: true), false, "RpcMalformedPayload", offset: 3, packedReads: 0));
        foreach (byte marker in new byte[] { 0, 6, 7 })
            Run("malformed actor encoding rejected: " + marker, () => {
                var bytes = new List<byte>(Payload(omitActor: true));
                bytes.Add(marker);
                for (int i = 1; i < marker; i++) bytes.Add(0);
                Check(bytes.ToArray(), false, "RpcMalformedPayload", packedReads: 0);
            });
        foreach (int actor in new[] { -1, -2, 3, 99, int.MaxValue, int.MinValue })
            Run("actor outside actual player slots: " + actor, () => Check(Payload(actor: actor), false, "RpcInvalidIndex"));
        Run("spoofing a valid actor blocked", () => Check(Payload(actor: 0), false, "RpcChatIdentity"));
        Run("duplicate Steam ID is not actor proof", () => { StartOfRound.Instance.allPlayerScripts[0].playerSteamId = PatchHelper.Player.playerSteamId; Check(Payload(actor: 0), false, "RpcChatIdentity"); });
        Run("new guard preserves enabled legacy kick", () => { PluginConfig.ChatReal2.Value = true; Check(Payload(actor: 0), false, "RpcChatIdentity", kicks: 1); });
        Run("disabled legacy feature does not enable its kick", () => { PluginConfig.ChatReal.Value = false; PluginConfig.ChatReal2.Value = true; Check(Payload(actor: 0), false, "RpcChatIdentity"); });
        Run("null slot quietly rejected as unavailable state", () => { StartOfRound.Instance.allPlayerScripts[1] = null; Check(Payload(), false); });
        Run("missing player array quietly rejected", () => { StartOfRound.Instance.allPlayerScripts = null; Check(Payload(), false); });
        Run("new feature works when legacy disabled", () => { PluginConfig.ChatReal.Value = false; Check(Payload(actor: 0), false, "RpcChatIdentity"); });
        Run("new-only normal chat skips legacy cooldown", () => { PluginConfig.ChatReal.Value = false; CooldownManager.Allowed = false; Check(Payload(), true); Assert(CooldownManager.Calls == 0, "legacy cooldown called"); });
        Run("both features disabled still silently reject malformed input", () => { PluginConfig.ChatReal.Value = false; RpcGuard.Enabled = false; Check(Array.Empty<byte>(), false, offset: 9, reads: 0); });
        Run("both features disabled still reject null input", () => { PluginConfig.ChatReal.Value = false; RpcGuard.Enabled = false; Check(Payload(null), false, reads: 0); });
        Run("both features disabled still bound strings before allocation", () => { PluginConfig.ChatReal.Value = false; RpcGuard.Enabled = false; Check(Payload("", length: uint.MaxValue), false, reads: 0); });
        Run("both features disabled still reject oversized native chat", () => { PluginConfig.ChatReal.Value = false; RpcGuard.Enabled = false; Check(Payload(new string('a', 51)), false, reads: 0); });
        Run("both features disabled still reject out-of-range actor", () => { PluginConfig.ChatReal.Value = false; RpcGuard.Enabled = false; Check(Payload(actor: 3), false); });
        Run("both features disabled still reject negative actor", () => { PluginConfig.ChatReal.Value = false; RpcGuard.Enabled = false; Check(Payload(actor: -1), false); });
        Run("both features disabled still reject unavailable slot", () => { PluginConfig.ChatReal.Value = false; RpcGuard.Enabled = false; StartOfRound.Instance.allPlayerScripts[1] = null; Check(Payload(), false); });
        Run("both features disabled allow valid chat without cooldown", () => { PluginConfig.ChatReal.Value = false; RpcGuard.Enabled = false; CooldownManager.Allowed = false; Check(Payload(), true, offset: 4); Assert(CooldownManager.Calls == 0, "legacy cooldown called"); });
        Run("both features disabled honor disabled identity check", () => { PluginConfig.ChatReal.Value = false; RpcGuard.Enabled = false; Check(Payload(actor: 0), true); });
        Run("legacy mode valid chat", () => { RpcGuard.Enabled = false; Check(Payload(), true); });
        Run("legacy impersonation reports legacy setting", () => { RpcGuard.Enabled = false; PluginConfig.ChatReal2.Value = true; Check(Payload(actor: 0), false, kicks: 1, legacy: "ChatReal"); });
        Run("legacy null chat quietly blocked", () => { RpcGuard.Enabled = false; Check(Payload(null), false, reads: 0); });
        Run("legacy malformed chat quietly blocked", () => { RpcGuard.Enabled = false; Check(new byte[] { 1 }, false, reads: 0); });
        Run("legacy oversized chat cannot bypass native limits", () => { RpcGuard.Enabled = false; PatchHelper.locale.current_language = "zh_CN"; Check(Payload(new string('a', 51)), false, reads: 0); });
        Run("cooldown rejection is normal and silent", () => { CooldownManager.Allowed = false; Check(Payload(), false); });
        Run("Chinese valid chat uses single existing relay", () => { PatchHelper.locale.current_language = "zh_CN"; Check(Payload(new string('a', 50)), false, broadcasts: 1); });
        Run("Chinese oversized chat is blocked before relay", () => { PatchHelper.locale.current_language = "zh_CN"; Check(Payload(new string('a', 51)), false, "RpcInvalidChat", reads: 0); });
        Run("Chinese cooldown does not relay", () => { PatchHelper.locale.current_language = "zh_CN"; CooldownManager.Allowed = false; Check(Payload(), false); });
        Run("host bypass preserves cursor and does not parse", () => { PatchHelper.CheckResult = false; Check(Array.Empty<byte>(), true, offset: 6, reads: 0); });
        Run("unresolved sender rejected without attribution", () => { PatchHelper.CheckResult = false; PatchHelper.Player = null; Check(Array.Empty<byte>(), false, offset: 6, reads: 0); });
        Console.WriteLine($"Tests: {count}; failures: {failures}");
        return failures == 0 ? 0 : 1;
    }
}
