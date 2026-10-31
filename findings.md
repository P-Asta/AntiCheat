# Findings

## 2026-10-31: Sprint observer final verification

- Pure tracker43, game adapter44, host notification21, real observation Harmony15, all existing remaining suites288:411 executions passed. Full Release build0 errors/75 existing NU1603-Harmony003 warnings. Actual Unity/multiplayer validation remains outstanding.
- Parent coordinate changes now preserve the same player's notification cooldown while resetting evidence; regression initially failed. Tiny timescale changes and synchronized remote slime signals are excluded, also verified RED-to-GREEN. Remote teleport flags may stay set, so rising edges plus TeleportPlayer events reset observation without mutating game state.
- Real HarmonyX2.10.1/net472 tests confirm optional target all/mixed/none behavior, original method execution and evidence clearing/resumption. No new production dependencies.
- ZIP1.0.2 matches all9 staged/current files;160155 bytes, SHA256 b3eb5421d4176fac60403e0214914cf9677617462d9545cd31ac276a1f88f353. New user icon remains unchanged. No upload.

## 2026-10-31: Authorized sprint observation

- User explicitly accepted heuristic host-only suspicion, no auto-enforcement. New observer uses scaled time, received local position in a stable parent, Sprinting animator tag, runtime sprintTime/movementSpeed; no remote sprintMeter/isSprinting or network sender inference.
- Independent design audit: one candidate per episode; confirm after2s nonsprint rest;3episodes in300s;120s notification cooldown. Both2x duration and2x unweighted nominal sprint distance required. Legitimate idle position silence must preserve history, but stale Sprinting/gaps discard it. Routine animator transitions/exhaustion must not reset all history or repetition could never be observed.
- Native DamageOnOtherClients updates health/timeSinceTakingDamage (absolute realtime timestamp). Damage/teleport/jump event hooks and sample changes clear evidence. TZP residual drunkness/inertia must outlive button release exemption. Exclude held TZP conservatively and add effect/damage grace.
- Native owner-only sliding flags cannot prove absence remotely; replicated slime audio state also excludes. Physics parents, vehicles, special interaction, water, external force and uncertain state cause reset. No heuristic can establish all missing remote state, so no proof-of-cheat wording/public broadcast.
- New notification method tested2RED ->21/21 total notification regression GREEN: local host chat even when existing detector destination isPublicChat, sanitizednames, no kicks, no client/localplayer reports. Puretracker andadapter workers still implementing.

## 2026-10-31: Stamina and invalid RPC follow-up

- BetterStamina1.0.2 DLL SHA256 `55dfe66999f98e0d72952f8ae8f3343eeef4dd1933bca91f334323873edcd4b9`: owner-side stamina delta multipliers, no transmitted mod signature. Native stamina is owner-only; TZP adds recovery/residual effects, and certain damage>=10 adds damage/125 stamina. No heuristic stamina enforcement implemented.
- Public SkidPwner1.0.1 and LobbyGuard source commit `1f09c4108ed2def50b06db42c87b79bb79cc5bd3` inspected. Neither broad client bans nor transport/voice code copied. New safeguards cover terminal/moon indices, array/string lengths before allocation, negative credits, delivery count below native -1 sentinel, and authenticated chat authors.
- Native packed int decoder returns zero for reserved markers0/6/7 (0 does not advance cursor). Independent review caught incorrect test-double exceptions; fixtures corrected and production now checks marker1..5/full size before native decode.
- GUI previously only logged; now native DisplayTip, useSave=false. Duplicate alerts throttled10sec with bounded history. ChatReal legacy Kick is ORed with new optional Kick once.
- Installed metadata audit found5 removed mandatory RPC targets out of90. Optional legacy router retains old checks where available and warns otherwise; other85 targets preserved. Actual HarmonyX2.10.1 rejects empty TargetMethods fallback: RED reproduced, HarmonyPrepare gate fixed it. Replacement universal RPC checks are not claimed.
- Legacy tests use actual HarmonyX2.10.1 on net472 because old MonoMod cannot detour .NET10; no new production dependency. Source-text assertions removed in favor of actual behavior/patch registration checks.
- Details in docs/rpc-validation-audit-ko.md. Live multiplayer validation remains pending.

- Project targets netstandard2.1, BepInEx 5.4, game reference 73.0.0-ngd.0; local NuGet cache contains 81.0.5-ngd.0.
- GrabObjectServerRpc checks only ItemSlots for capacity. Jetpack message exception requires the jetpack to be selected and no two-handed item in inventory; else-if classification also hides jetpacks marked two-handed.
- User confirmed extra/utility slot is native v81, not a mod.
- No existing tests or runtime logs in repository. Working tree initially clean.
- Inspected installed v81 Assembly-CSharp.dll with ilspycmd. FirstEmptyItemSlot(GrabbableObject) is read-only and returns 50 for an eligible tool when ItemOnlySlot is empty, otherwise a free regular slot, or -1. GrabObjectClientRpc uses this method to select the destination. The grab RPC handler ID remains 1554282707.
- Context7 had no v81 API coverage; actual installed game binary provided authoritative verification. Public supporting reference: https://github.com/Solo00n/ImprovedTABSlot (native slot routing).
- Original production prefix failed 4 regression cases: three jetpack notification variants and a valid tool grab with four full normal slots/free utility. Occupied utility plus three normal items accepting fourth scrap already passes; user's exact runtime symptom remains unconfirmed without logs/game reproduction.

## Current ILSpy audit evidence

- Installed Steam build ID 22825947. Artifact: `C:/Program Files (x86)/Steam/steamapps/common/Lethal Company/Lethal Company_Data/Managed/Assembly-CSharp.dll`, 2034688 bytes, SHA-256 `5f7db5538b78dc408845a3002907619785ac9f9c6b6059d13dc9a602d9b65731`, MVID `aca1e98d-6f84-4d3f-85cd-22b6f7be2f9b`.
- REA static managed inspection evidence `ev_976fe5f3e38cce5e48f7896e99c75dea3ff16283844792d75fd3ec0eacb4e793`. No target code executed by inspection.
- Fresh `ilspycmd -t GameNetcodeStuff.PlayerControllerB` output is in `%TEMP%/anticheat-grab-audit-20261031/`. Full copyrighted game source remains outside the repository.
- Scope continues the existing uncommitted fix; no worktree switch or reset, to preserve previous edits and tests.

### Confirmed control flow and fixes to make

- ILSpy 11.0.0.9375: `BeginGrabObject` (PlayerControllerB lines 2347-2401) checks active `twoHanded`, not jetpack possession. One-handed jetpack + new two-handed scrap is legal. Existing `hasJetpack` OR clauses can falsely block it even though notifications were suppressed.
- `GrabObjectServerRpc` (2494-2544) already rejects `heldByPlayerOnServer` via `GrabObjectClientRpc(false, originalReference)`. Normal simultaneous pickup/bag contention is not evidence of cheating. Let vanilla reject it before inspecting capacity/two-hand/distance.
- Native RPC handler (8672-8693) checks sender ownership before acting. Our prefix runs earlier and must respect that guard and target/player identity before any accusation.
- `SwitchToItemSlot` (1898-1985) writes utility slot 50 separately. `FirstEmptyItemSlot` (1852-1874) is the correct read-only capacity authority. Same reference already stored or contradictory inventory ownership is uncertain state, not proven overflow.
- Native `__endSendClientRpc` handles host messages synchronously (separate ILSpy NetworkBehaviour output, lines 236-259). A delayed host ClientRpc is NOT established as the cause; no speculative pending-inventory tracker is needed.
- `serverPlayerPosition` normally holds local-space data: sender uses `thisPlayerBody.localPosition` (7609), receiver stores it (4447), interpolation uses `transform.localPosition` (6030-6040). Existing distance check compares that against item world position. Transform the synchronized position through the parent and require both synchronized and rendered world positions to be far; retain ship/teleport guards.
- Cancellation can directly call the game's failed-grab ClientRpc with the original reference. Forcing private RPC execution stage and recursively invoking the ServerRpc is unnecessary.
- BeltBag native server rejection handles held/player/enemy contention normally. Existing bag prefix mislabels those transient conditions as forbidden contents; separate them from actual scrap/blacklist restriction.

Design: keep existing settings and message keys; eliminate invalid evidence, use game slot routing, quietly cancel a genuinely full but contradictory snapshot, and retain rejection/notification for coherent overflow. Add detailed operation logs for unknown capacity rather than accusing. Do not change gameplay state to test available slots. Legacy fallback uses regular slots where the item-aware API is absent.

Limits: original user's exact occupied-utility-first incident has no matching runtime log in available current logs; static findings and controlled regressions will be reported separately from live multiplayer verification.
