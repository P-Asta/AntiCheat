using System;
using AntiCheat;
using AntiCheat.Patches;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

static class Program
{
    static int failures, cases;
    static GrabbableObject Scrap(bool twoHanded = false) => new GrabbableObject { itemProperties = new Item { isScrap = true, twoHanded = twoHanded } };
    static void Run(string name, Action test)
    {
        cases++;
        PatchHelper.Player = new PlayerControllerB();
        PatchHelper.Messages.Clear();
        PatchHelper.Kicked = PatchHelper.SkipValidation = false;
        StartOfRound.Instance = new StartOfRound();
        PluginConfig.GrabObject.Value = true;
        PluginConfig.GrabObject_Kick.Value = true;
        PluginConfig.GrabObject_TwoHand.Value = false;
        PluginConfig.GrabObject_MoreSlot.Value = true;
        PluginConfig.GrabObject_SendLog.Value = true;
        try { test(); Console.WriteLine("PASS " + name); }
        catch (Exception e) { failures++; Console.WriteLine("FAIL " + name + ": " + e.Message); }
    }
    static void Assert(bool condition, string message) { if (!condition) throw new Exception(message); }
    static __RpcParams Sender(ulong clientId = 7) => new __RpcParams { Server = new ServerRpcParams { Receive = new ServerRpcReceiveParams { SenderClientId = clientId } } };
    static FastBufferReader Reader(GrabbableObject item, int position = 0) => new FastBufferReader(new NetworkObjectReference { Object = new NetworkObject { Item = item } }, position);
    static void AssertOutcome(PlayerControllerB target, bool actual, bool allowed, bool kicked, params string[] messages)
    {
        Assert(actual == allowed && target.Cancelled == !allowed && PatchHelper.Kicked == kicked,
            $"allowed={actual}, cancelled={target.Cancelled}, kicked={PatchHelper.Kicked}; expected allowed={allowed}, kicked={kicked}");
        Assert(string.Join(",", PatchHelper.Messages) == string.Join(",", messages),
            "messages=[" + string.Join(",", PatchHelper.Messages) + "]; expected=[" + string.Join(",", messages) + "]");
    }
    static void Grab(GrabbableObject item, bool allowed, params string[] messages) => GrabExpect(item, allowed, !allowed && PluginConfig.GrabObject_Kick.Value, messages);
    static void GrabExpect(GrabbableObject item, bool allowed, bool kicked, params string[] messages)
    {
        var player = PatchHelper.Player;
        var reader = Reader(item);
        bool actual = PlayerControllerGrabObjectPatch.GrabObjectServerRpc(player, reader, Sender());
        AssertOutcome(player, actual, allowed, kicked, messages);
        Assert(reader.Position == 0, "RPC payload cursor was not restored");
    }
    static GrabbableObject Store(int slot, GrabbableObject item, bool selected = false)
    {
        var player = PatchHelper.Player;
        item.playerHeldBy = player;
        item.heldByPlayerOnServer = item.isHeld = true;
        item.isPocketed = !selected;
#if !LEGACY_SLOTS
        if (slot == 50) player.ItemOnlySlot = item;
        else
#endif
            player.ItemSlots[slot] = item;
        if (selected)
        {
            player.currentItemSlot = slot;
            player.currentlyHeldObjectServer = item;
            player.twoHanded = item.itemProperties.twoHanded;
            player.isHoldingObject = true;
        }
        return item;
    }
    static void FullNormalSlots() { for (int i = 0; i < 4; i++) Store(i, Scrap()); }
    static void Landed() => StartOfRound.Instance.shipHasLanded = true;
    static void Switch(bool allowed, PlayerControllerB target = null)
    {
        target ??= PatchHelper.Player;
        bool actual = PlayerControllerGrabObjectPatch.SwitchItemSlotsServerRpc(target, Reader(null), Sender());
        Assert(actual == allowed, $"switch allowed={actual}; expected {allowed}");
        Assert(!target.Cancelled && !PatchHelper.Kicked && PatchHelper.Messages.Count == 0,
            "switch validation emitted a grab response or punishment");
    }
    static int Main()
    {
        foreach (bool twoHanded in new[] { false, true })
        foreach (bool selected in new[] { false, true })
            Run($"jetpack (twoHanded={twoHanded}, selected={selected}) + two-handed scrap stays silent", () => {
                var p = PatchHelper.Player;
                Store(0, new JetpackItem { itemProperties = new Item { twoHanded = twoHanded } }, selected);
                if (!selected) p.currentItemSlot = 1;
                Grab(Scrap(true), true);
            });
        Run("two distinct two-handed scraps still warn", () => {
            Store(0, Scrap(true));
            Grab(Scrap(true), true, "GrabObject_TwoHand");
        });
        Run("stowed two-handed jetpack does not introduce a new one-hand restriction", () => {
            PluginConfig.GrabObject_TwoHand.Value = true;
            Store(0, new JetpackItem { itemProperties = new Item { twoHanded = true } });
            PatchHelper.Player.currentItemSlot = 1;
            Grab(Scrap(), true);
        });
        Run("jetpack does not hide multiple two-handed scraps", () => {
            Store(0, new JetpackItem());
            Store(1, Scrap(true));
            Grab(Scrap(true), true, "GrabObject_TwoHand");
        });
        Run("one-handed jetpack does not falsely trigger two-hand enforcement", () => {
            PluginConfig.GrabObject_TwoHand.Value = true;
            Store(0, new JetpackItem());
            Grab(Scrap(true), true);
        });
#if LEGACY_SLOTS
        Run("legacy game without new slot API still rejects fifth item", () => {
            FullNormalSlots(); Grab(new GrabbableObject(), false, "GrabObject_MoreSlot");
        });
#else
        Run("four normal items plus free utility slot accepts a tool", () => {
            FullNormalSlots(); Grab(new GrabbableObject(), true);
        });
        Run("occupied utility plus three normal items accepts fourth scrap", () => {
            FullNormalSlots();
            PatchHelper.Player.ItemSlots[3] = null;
            Store(50, new GrabbableObject(), selected: true);
            Grab(Scrap(), true);
        });
        Run("five occupied slots reject sixth item", () => {
            FullNormalSlots(); Store(50, new GrabbableObject());
            Grab(new GrabbableObject(), false, "GrabObject_MoreSlot");
        });
#endif
        Run("free utility slot cannot accept scrap", () => {
            FullNormalSlots(); Grab(Scrap(), false, "GrabObject_MoreSlot");
        });
        Run("free utility slot cannot accept blacklisted tool", () => {
            FullNormalSlots(); Grab(new GrabbableObject { itemProperties = new Item { disallowUtilitySlot = true } }, false, "GrabObject_MoreSlot");
        });
        Run("parent-local position is converted before checking distant world coordinates", () => {
            Landed();
            var p = PatchHelper.Player;
            p.transform.parent = new Transform { position = new Vector3(1000, 0, 0), localScale = new Vector3(2, 1, 1) };
            p.serverPlayerPosition = new Vector3(3, 0, 0);
            p.transform.position = new Vector3(700, 0, 0);
            var item = Scrap(); item.transform.position = new Vector3(1007, 0, 0);
            Grab(item, true);
        });
        Run("near rendered player prevents stale parent or teleport coordinates accusation", () => {
            Landed();
            PatchHelper.Player.transform.position = new Vector3(1000, 0, 0);
            var item = Scrap(); item.transform.position = new Vector3(1002, 0, 0);
            Grab(item, true);
        });
        Run("genuinely distant grab remains blocked", () => {
            Landed();
            var item = Scrap(); item.transform.position = new Vector3(101, 0, 0);
            Grab(item, false, "GrabObject");
        });
        Run("teleport guard still permits unsettled position", () => {
            Landed(); PatchHelper.Player.teleportedLastFrame = true;
            var item = Scrap(); item.transform.position = new Vector3(1000, 0, 0);
            Grab(item, true);
        });
        Run("wrong RPC target is left to native ownership rejection without punishment", () => {
            FullNormalSlots();
            var target = new PlayerControllerB { OwnerClientId = 99, actualClientId = 99 };
            bool actual = PlayerControllerGrabObjectPatch.GrabObjectServerRpc(target, Reader(Scrap()), Sender());
            AssertOutcome(target, actual, true, false);
            Assert(!PatchHelper.Player.Cancelled, "sender was cancelled for another player's target");
        });
        Run("sender lookup inconsistent with target does not accuse player", () => {
            FullNormalSlots();
            var target = new PlayerControllerB();
            bool actual = PlayerControllerGrabObjectPatch.GrabObjectServerRpc(target, Reader(Scrap()), Sender());
            AssertOutcome(target, actual, true, false);
        });
        Run("already claimed target is a native grab race without overflow accusation", () => {
            FullNormalSlots();
            var item = Scrap(); item.heldByPlayerOnServer = true;
            item.playerHeldBy = new PlayerControllerB { actualClientId = 99, OwnerClientId = 99 };
            Grab(item, true);
        });
        Run("replayed item already in own inventory is not a second two-handed item", () => {
            PluginConfig.GrabObject_TwoHand.Value = true;
            var item = Store(0, Scrap(true), selected: true);
            Grab(item, true);
        });
        Run("cancel response retains the rejected object reference", () => {
            PluginConfig.GrabObject_Kick.Value = false;
            FullNormalSlots();
            var p = PatchHelper.Player; var reader = Reader(Scrap());
            bool actual = PlayerControllerGrabObjectPatch.GrabObjectServerRpc(p, reader, Sender());
            AssertOutcome(p, actual, false, false, "GrabObject_MoreSlot");
            Assert(ReferenceEquals(p.CancelledReference.Object, reader.Value.Object), "cancellation replaced the actual object with a default reference");
        });
        Run("cancel response leaves RPC execution stage unchanged", () => {
            PluginConfig.GrabObject_Kick.Value = false;
            FullNormalSlots();
            var p = PatchHelper.Player; p.RpcStage = 0;
            Grab(Scrap(), false, "GrabObject_MoreSlot");
            Assert(p.RpcStage == 0 && p.ClientRpcStageAtCall == 0, "cancellation did not use the normal send stage");
            Assert(p.ServerRpcCalls == 0, "cancellation re-entered a server RPC");
        });
        Run("prefix preserves a nonzero reader entry position", () => {
            var reader = Reader(Scrap(), position: 5);
            bool actual = PlayerControllerGrabObjectPatch.GrabObjectServerRpc(PatchHelper.Player, reader, Sender());
            AssertOutcome(PatchHelper.Player, actual, true, false);
            Assert(reader.Position == 5, "reader position was reset to zero instead of restored");
        });
        Run("full slots containing a dropped item cancel quietly without punishment", () => {
            FullNormalSlots();
            PatchHelper.Player.ItemSlots[2].heldByPlayerOnServer = false;
            PatchHelper.Player.ItemSlots[2].playerHeldBy = null;
            GrabExpect(Scrap(), false, false);
        });
        Run("full slots containing another player's item cancel quietly without punishment", () => {
            FullNormalSlots();
            PatchHelper.Player.ItemSlots[2].playerHeldBy = new PlayerControllerB();
            GrabExpect(Scrap(), false, false);
        });
        Run("duplicate slot references cancel quietly without overflow accusation", () => {
            FullNormalSlots();
            PatchHelper.Player.ItemSlots[3] = PatchHelper.Player.ItemSlots[2];
            GrabExpect(Scrap(), false, false);
        });
        Run("disabled feature leaves a full inventory to native handling", () => {
            PluginConfig.GrabObject.Value = false; FullNormalSlots();
            Grab(Scrap(), true);
        });
        Run("disabled messages still enforce actual full inventory", () => {
            PluginConfig.GrabObject_SendLog.Value = false; FullNormalSlots();
            Grab(Scrap(), false);
        });
        Run("disabled grab feature permits switching from a two-handed item", () => {
            Store(0, Scrap(true), selected: true);
            PluginConfig.GrabObject.Value = false;
            PluginConfig.GrabObject_TwoHand.Value = true;
            Switch(true);
        });
        Run("disabled two-hand option permits switching from a two-handed item", () => {
            Store(0, Scrap(true), selected: true);
            Switch(true);
        });
        Run("coherent active two-handed item still blocks slot switching", () => {
            Store(0, Scrap(true), selected: true);
            PluginConfig.GrabObject_TwoHand.Value = true;
            Switch(false);
        });
        Run("stale held reference does not prevent normal slot switching", () => {
            Store(0, Scrap(true), selected: true);
            PatchHelper.Player.ItemSlots[0] = null;
            PluginConfig.GrabObject_TwoHand.Value = true;
            Switch(true);
        });
        Run("wrong switch target is left to native ownership rejection", () => {
            Store(0, Scrap(true), selected: true);
            PluginConfig.GrabObject_TwoHand.Value = true;
            Switch(true, new PlayerControllerB { OwnerClientId = 99, actualClientId = 99 });
        });
        Run("stale held two-handed reference does not accuse an ordinary grab", () => {
            Store(0, Scrap(true), selected: true);
            PatchHelper.Player.ItemSlots[0] = null;
            PluginConfig.GrabObject_TwoHand.Value = true;
            Grab(Scrap(), true);
        });
        Run("coherent active two-handed item still blocks an ordinary grab", () => {
            Store(0, Scrap(true), selected: true);
            PluginConfig.GrabObject_TwoHand.Value = true;
            Grab(Scrap(), false, "GrabObject_TwoHand");
        });
#if !LEGACY_SLOTS
        Run("slot resolver failure with no known vacancy cancels without accusation", () => {
            FullNormalSlots(); Store(50, new GrabbableObject());
            PatchHelper.Player.ThrowSlotLookup = true;
            GrabExpect(Scrap(), false, false);
        });
#endif
        Run("already stored item with stale held flag cancels without duplicating a slot", () => {
            var item = Store(0, Scrap());
            item.heldByPlayerOnServer = false;
            GrabExpect(item, false, false);
        });
#if !LEGACY_SLOTS
        Run("stale occupied utility slot is not confirmed overflow", () => {
            FullNormalSlots();
            var tool = Store(50, new GrabbableObject());
            tool.heldByPlayerOnServer = false;
            GrabExpect(new GrabbableObject(), false, false);
        });
        Run("duplicate between utility and regular slots is not confirmed overflow", () => {
            FullNormalSlots();
            PatchHelper.Player.ItemOnlySlot = PatchHelper.Player.ItemSlots[0];
            GrabExpect(new GrabbableObject(), false, false);
        });
#endif
        Run("missing item metadata cancels without accusation", () => {
            GrabExpect(new GrabbableObject { itemProperties = null }, false, false);
        });
        Run("missing inventory cancels without accusation", () => {
            PatchHelper.Player.ItemSlots = null;
            GrabExpect(Scrap(), false, false);
        });
        Run("overflow option disabled warns without cancelling or kicking", () => {
            FullNormalSlots(); PluginConfig.GrabObject_MoreSlot.Value = false;
            Grab(Scrap(), true, "GrabObject_MoreSlot");
        });
#if !LEGACY_SLOTS
        Run("unknown capacity cannot turn disabled two-hand enforcement into a kick", () => {
            FullNormalSlots(); Store(0, Scrap(true));
            PatchHelper.Player.ThrowSlotLookup = true;
            GrabExpect(Scrap(true), false, false);
        });
        Run("slot resolver failure with a vacant slot cancels without accusation", () => {
            PatchHelper.Player.ThrowSlotLookup = true;
            GrabExpect(Scrap(), false, false);
        });
        Run("out-of-range current slot cancels before vanilla mutates item ownership", () => {
            PatchHelper.Player.currentItemSlot = 4;
            GrabExpect(Scrap(), false, false);
        });
        foreach (int occupied in new[] { 0, 1, 2, 3 })
        foreach (bool twoHanded in new[] { false, true })
            Run($"utility-first inventory with {occupied} normal items accepts scrap (twoHanded={twoHanded})", () => {
                PluginConfig.GrabObject_TwoHand.Value = true;
                Store(50, new GrabbableObject(), selected: true);
                for (int i = 0; i < occupied; i++) Store(i, Scrap());
                Grab(Scrap(twoHanded), true);
            });
#endif
        Console.WriteLine($"Cases: {cases}; Failures: {failures}");
        return failures == 0 ? 0 : 1;
    }
}
