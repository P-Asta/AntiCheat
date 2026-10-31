# Belt bag validation regression checks

Run `dotnet run --project tests/BeltBagRegression` with .NET SDK 10.
The executable links the production `BeltBagItemPatch.cs` against minimal Unity,
network, and configuration boundaries. No test framework or game installation is
needed. Assertions cover native pass-through, emitted messages, cancellation
recipients, configuration combinations, and preservation of the reader cursor.

The expectations were checked against ILSpy output from the installed v81 game:

- `BeltBagItem.TryAddObjectToBagServerRpc` has `RequireOwnership = false`. A missing
  network reference, missing `GrabbableObject`, or an object with `isHeld`,
  `heldByPlayerOnServer`, or `isHeldByEnemy` set reaches the normal cancellation
  RPC. These are not evidence of a disallowed item.
- `BeltBagItem.ItemInteractLeftRight` excludes scrap and item IDs `123984` and
  `819501`; those restrictions and their logging/enforcement settings remain.
- `CancelAddObjectToBagClientRpc` indexes the player array with `playerWhoAdded`.
  The prefix silently rejects a mismatched index and cancels the actual sender.
  It does not introduce a bag ownership restriction.

These checks do not execute Unity, Harmony patch loading, or multiplayer message
delivery. In-game verification still requires a host and a remote player racing
to pick up or bag the same object; the losing request should cancel normally
without a `GrabObject_BeltBag` warning.
