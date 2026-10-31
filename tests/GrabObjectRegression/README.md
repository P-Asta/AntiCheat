# Grab validation regression checks

Run `dotnet run --project tests/GrabObjectRegression` with .NET SDK 10.
No test framework or game installation is needed. The executable compiles the
actual production RPC prefix against minimal Unity/network/config boundary doubles.
Assertions cover returned permission, emitted message keys, cancellation, and kicks.
Run again with `-p:LegacySlots=true` to verify the fallback when both the v81
item-aware slot API and the utility-slot field are absent. This is a compatibility
boundary test, not execution against the actual v73 game.

The v81 slot-selection double follows the installed game's `FirstEmptyItemSlot`
contract. The suite executes the production prefix and covers utility-first
inventory states, actual overflow, stale/duplicate references, ownership guards,
contested grabs, two-hand settings, world/local distance, failed slot lookups,
original-reference cancellation, and reader cursor preservation. The vector
double supports translation/scaling; it does not emulate Unity's entire transform
hierarchy or destroyed-object null semantics.

These tests do not validate Harmony patch loading or multiplayer delivery.
Verify in v81 with a host and a remote player:

1. Carry a one-handed jetpack, select it or another slot, and grab two-handed
   scrap: no `GrabObject_TwoHand` message or cancellation, including with TwoHand
   enforcement enabled. A coherently held item actually marked two-handed still
   prevents another grab when enforcement is enabled.
2. Fill the utility slot, then fill all four normal slots: no overflow message.
3. Fill all four normal slots, then grab an eligible tool into an empty utility slot:
   no overflow message.
4. With all five slots occupied, an extra grab remains rejected when MoreSlot is enabled.
5. Race to grab/bag the same item: the native losing-request cancellation should
   produce no cheating message. Check the separate `tests/BeltBagRegression` suite.
6. Grab near the player inside the ship/vehicle and after teleporting: no distance
   accusation. Truly distant grabs still reject while landed and not teleporting.
7. Drop/place an item, switch between utility and normal slots, and immediately
   grab again; repeat across rounds. Capture the host's operation log if a warning
   persists. The log includes capacity, inventory consistency, active slot, and
   utility occupancy.

The precise occupied-utility sequence in step 2 also passes on the original code;
an in-game reproduction/log is still needed to establish whether another cause
contributes to the reported symptom. See `docs/grab-validation-audit-ko.md` for
the actual binary identity, confirmed findings, and limitations.
