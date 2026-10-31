using System;
using AntiCheat;
using AntiCheat.Patches;
using GameNetcodeStuff;
using Unity.Netcode;

static class Program
{
    static int failures, count;
    static void Run(string name, Action test)
    {
        count++;
        PatchHelper.Player = new PlayerControllerB();
        PatchHelper.CheckResult = true;
        PatchHelper.Messages.Clear();
        PluginConfig.GrabObject.Value = true;
        PluginConfig.GrabObject_SendLog.Value = true;
        PluginConfig.GrabObject_BeltBag.Value = true;
        try { test(); Console.WriteLine("PASS " + name); }
        catch (Exception exception) { failures++; Console.WriteLine("FAIL " + name + ": " + exception.Message); }
    }

    static void Add(GrabbableObject item, bool allowed, bool message = false, int? cancellation = null,
        int claimedPlayer = 2, int initialPosition = 0, bool unresolved = false, BeltBagItem bag = null)
    {
        bag ??= new BeltBagItem();
        var state = new ReaderState {
            Position = initialPosition, PlayerIndex = claimedPlayer,
            Reference = new NetworkObjectReference { Object = unresolved ? null : new NetworkObject { Item = item } }
        };
        bool actual = BeltBagItemPatch.__rpc_handler_2988305002(bag, new FastBufferReader { State = state }, default);
        if (actual != allowed) throw new Exception($"allowed={actual}; expected {allowed}");
        if (state.Position != initialPosition) throw new Exception($"reader position={state.Position}; expected {initialPosition}");
        string expectedMessage = message ? "GrabObject_BeltBag" : "";
        if (string.Join(",", PatchHelper.Messages) != expectedMessage)
            throw new Exception("messages=[" + string.Join(",", PatchHelper.Messages) + "]; expected [" + expectedMessage + "]");
        string expectedCancellation = cancellation?.ToString() ?? "";
        if (string.Join(",", bag.CancelledPlayerIndices) != expectedCancellation)
            throw new Exception("cancelled=[" + string.Join(",", bag.CancelledPlayerIndices) + "]; expected [" + expectedCancellation + "]");
    }

    static int Main()
    {
        Run("available tool passes to vanilla", () => Add(new GrabbableObject(), true));
        foreach (bool scrap in new[] { false, true })
        {
            Run($"player-held candidate is normal contention (scrap={scrap})", () =>
                Add(new GrabbableObject { isHeld = true, itemProperties = new Item { isScrap = scrap } }, true));
            Run($"enemy-held candidate is normal contention (scrap={scrap})", () =>
                Add(new GrabbableObject { isHeldByEnemy = true, itemProperties = new Item { isScrap = scrap } }, true));
            Run($"server-reserved candidate is normal contention (scrap={scrap})", () =>
                Add(new GrabbableObject { heldByPlayerOnServer = true, itemProperties = new Item { isScrap = scrap } }, true));
        }
        Run("unresolved network reference passes to native cancellation", () => Add(null, true, unresolved: true));
        Run("missing GrabbableObject passes without dereference or accusation", () => Add(null, true));
        Run("missing item properties cancels without dereference or accusation", () =>
            Add(new GrabbableObject { itemProperties = null }, false, cancellation: 2));
        Run("reader preserves a nonzero entry position", () => Add(new GrabbableObject(), true, initialPosition: 7));
        Run("reader is restored when payload decoding fails", () => {
            var state = new ReaderState { Position = 7, ThrowReadingPlayerIndex = true };
            try
            {
                BeltBagItemPatch.__rpc_handler_2988305002(new BeltBagItem(), new FastBufferReader { State = state }, default);
                throw new Exception("expected malformed payload to throw at the network boundary");
            }
            catch (InvalidOperationException)
            {
                if (state.Position != 7) throw new Exception($"reader position={state.Position}; expected 7");
                if (PatchHelper.Messages.Count != 0) throw new Exception("malformed payload was accused of cheating");
            }
        });
        foreach (bool log in new[] { false, true })
        foreach (bool enforce in new[] { false, true })
        {
            Run($"scrap restriction settings (log={log}, enforce={enforce})", () => {
                PluginConfig.GrabObject_SendLog.Value = log;
                PluginConfig.GrabObject_BeltBag.Value = enforce;
                Add(new GrabbableObject { itemProperties = new Item { isScrap = true } }, !enforce, log, enforce ? 2 : null);
            });
            Run($"spoofed player index cancels actual sender (log={log}, enforce={enforce})", () => {
                PluginConfig.GrabObject_SendLog.Value = log;
                PluginConfig.GrabObject_BeltBag.Value = enforce;
                Add(new GrabbableObject { itemProperties = new Item { isScrap = true } }, false, cancellation: 2, claimedPlayer: 99);
            });
        }
        foreach (int itemId in new[] { 123984, 819501 })
            Run($"blacklisted item {itemId} remains restricted", () =>
                Add(new GrabbableObject { itemProperties = new Item { itemId = itemId } }, false, true, 2));
        Run("negative claimed index cancels actual sender without accusation", () =>
            Add(new GrabbableObject(), false, cancellation: 2, claimedPlayer: -1));
        Run("a different valid sender index is used for cancellation", () => {
            PatchHelper.Player.playerClientId = 3;
            Add(new GrabbableObject { itemProperties = new Item { isScrap = true } }, false, true, 3, claimedPlayer: 3);
        });
        Run("RequireOwnership=false bag does not add an ownership restriction", () =>
            Add(new GrabbableObject(), true, bag: new BeltBagItem { OwnerClientId = 99, playerHeldBy = new PlayerControllerB() }));
        Run("global disabled setting bypasses restrictions and parsing", () => {
            PluginConfig.GrabObject.Value = false;
            Add(new GrabbableObject { itemProperties = new Item { isScrap = true } }, true, claimedPlayer: 99, initialPosition: 7);
        });
        Run("excluded host bypasses restrictions", () => {
            PatchHelper.CheckResult = false;
            Add(new GrabbableObject { itemProperties = new Item { isScrap = true } }, true, initialPosition: 7);
        });
        Run("unknown sender remains rejected", () => {
            PatchHelper.CheckResult = false;
            PatchHelper.Player = null;
            Add(new GrabbableObject(), false, initialPosition: 7);
        });
        Console.WriteLine($"Tests: {count}; failures: {failures}");
        return failures == 0 ? 0 : 1;
    }
}
