# Grab validation false positives

## 2026-10-31: Authorized Thunderstore publication

User explicitly requested uploading the prepared package, superseding the earlier ZIP-only instruction. Publish the verified AstaAntiCheat1.0.2 ZIP to the authenticated asta team in Lethal Company. No package edits. Browser upload control accepted thunderstore.zip (154.74KiB); selected Server-side/BepInEx/Mods/Bug Fixes, NSFW off.

- [x] Verify final ZIP hash/9 entries/README/icon/DLL and authenticated team.
- [x] Attach ZIP and configure community/categories.
- [x] Submit and verify the published package/version; record its URL.

Publication completed: UI reported Success and Lethal Company listing; public page shows Latest version1.0.2, dependency string asta-AstaAntiCheat-1.0.2, updated English README/Korean link and download link. URL https://thunderstore.io/c/lethal-company/p/asta/AstaAntiCheat/. Existing AI Generated category remains alongside selected BepInEx/Bug Fixes/Mods/Server-side. Screenshot saved under the authorized visualization directory. File picker recovered through the visible upload control after hidden-input timeout; no security settings changed. Package hash unchanged9fa827c4df650a45130d9c8bee33a428effdadc69733b5c1366527387ede104b.

## 2026-10-31: Separate English/Korean repository README

User requests upstream-style language separation and a conventional repository README. Default README.md will be English; docs/README-ko.md will contain matching Korean documentation. Preserve docs/README-en.md as a short navigation page for existing links, original attribution and existing Chinese/Russian documents. Organize around introduction/features/installation/configuration/contributing rather than implementation or test reports. Keep detailed technical evidence in existing audit documents. Apply the same English README to Thunderstore with absolute repository links and rebuild its ZIP. No code/version/icon changes or publishing.

- [x] Inspect existing documents, upstream layout and project conventions.
- [x] Rewrite English and Korean documentation; review accuracy and language parity.
- [x] Validate links/Markdown, refresh packaged README/ZIP, verify unchanged binaries/icon and report.

Verified48 Markdown links against repository paths, balanced code fences, UTF-8, and English/Korean separation. Independent review found no factual or navigation issues. ZIP rebuilt with9 entries; only README content differs from the prior ZIP. Version remains1.0.2; DLL/icon/locales/manifest/changelog preserved. New ZIP158449 bytes, SHA256 9fa827c4df650a45130d9c8bee33a428effdadc69733b5c1366527387ede104b. No code tests or build rerun for this documentation-only change; git diff --check passed.

## 2026-10-31: Host-only repeated sprint suspicion (authorized)

User accepted a conservative observer. Informational host-only alerts; never block movement, change stamina/health, call KickPlayer/RpcGuard, or send public accusations. Preserve prior changes and icon. Prepare thunderstore/ZIP1.0.2, no publication.

Design: pure per-player tracker plus read-only PlayerControllerB.LateUpdate host adapter sampled every0.25 scaled seconds. Use received serverPlayerPosition in a stable parent and remote Animator Sprinting tag; never remote sprintMeter/isSprinting. Require both duration>2*runtime sprintTime and horizontal distance>2*runtime sprintTime*movementSpeed*2.25. Weight assumed1 for generous budget; thresholds are heuristics, not proof.

One candidate per continuous episode, confirmed after2 seconds stable nonsprinting rest so late damage/TZP can cancel it. Three separate candidates within300 seconds notify host;120 seconds cooldown then require new evidence. Never count a single endless sprint repeatedly. Known idle rest may have stale position; sprinting with stale position/gaps resets all evidence. Start first episode only after stable safe rest.

Nonfinite/missing state, frame gap>0.75s, time reversal/timescale change, changed runtime movement parameters, parent/identity/round transitions, teleport/implausible position jump reset all evidence. TZP active/residual/held chemical, any health/damage timestamp change or damage event, non-full health, vehicle/ladder/physics parent/special animation/jetpack/sliding/external movement exclude and clear observations. After uncertainty require2s stable safe rest before restarting. Lifecycle/disconnect clears state. Observed host data cannot establish absent client effects with certainty.

- [x] Confirm native fields/hooks and independently review design.
- [x] TDD pure tracker: valid/abnormal episodes, resets/cooldown/boundaries;43 cases passed.
- [x] TDD host adapter: host-only identity, TZP/damage/residual exclusions, fresh positions, cleanup, no enforcement/public messages;44 cases passed.
- [x] Enabled-by-default observer setting with no kick setting,3 locale messages and docs.
- [x] Review, run all suites, Release build; update1.0.2 package and verify9 files/icon. Report runtime/heuristic limits.

Final observation verification:411 regression executions passed (54+38+28+112+50+21+6+43+44+15), Release0 errors/75 existing NU1603-Harmony003 warnings, diff check passed. Real Harmony observation hooks tested with all/some/no optional events. Independent review's parent-change cooldown finding reproduced and corrected; no remaining important findings. Game multiplayer remains untested. ZIP1.0.2 contains exactly9 verified entries,160155 bytes; SHA256 b3eb5421d4176fac60403e0214914cf9677617462d9545cd31ac276a1f88f353. User icon preserved; no upload.

Rulings: reuse current dirty workspace, root planning files and C#/Harmony dependencies. User explicitly approved the observer. Reuse brainstorming/writing-plans/test-first workflows. Existing audit files are in C:/Users/pAsta/AppData/Local/Temp; current tool TEMP now points to a sandbox path. Wrong plan-header patch was rejected before mutation and corrected.

- [x] Inspect grab RPC validation and dependencies.
- [x] Verify v81 native utility-slot behavior and reproduce failures.
- [x] Fix jetpack message suppression and slot availability with focused regression tests.
- [x] Build, review diff, and record runtime verification limits.

Runtime follow-up: exact occupied-utility-first symptom still needs an in-game reproduction; no multiplayer session was run here.

Scope: suppress jetpack/two-handed scrap messages; preserve existing enforcement settings. Recognize native v81 utility slot without disabling actual inventory overflow detection.

## 2026-10-31: ILSpy-backed grab validation audit

Goal: trace the installed game's pickup/drop/utility-slot synchronization and fix demonstrable false positives in the related grab validation, preserving detection of genuinely invalid grabs.

- [x] Inspect actual ILSpy output, installed version/logs and independent audits; record evidence and uncertainty.
- [x] Design the smallest compatible validation changes and reproduce identified cases in production-prefix regression tests.
- [x] Implement fixes in the existing dirty workspace, preserving previous changes; extend boundary doubles only where the actual game requires it.
- [x] Run v81 and legacy regression suites, Release build, diff checks, and independent review.
- [x] Document validated behavior, build artifact, and remaining in-game/multiplayer checks.

Primary files: `AntiCheat/Patches/Features/GrabObject/PlayerControllerGrabObjectPatch.cs`, adjacent grab validation if evidence requires it, `tests/GrabObjectRegression/*`, and these planning files. No game installation changes.

Review focus: RPC sender/target identity; utility slot 50 and repeated/pending grabs; held/slot state during drops; world versus local position; cancellation and reflection failures. Follow test-first fixes for confirmed cases. Baseline: prior turn executed all 13 v81 prefix tests successfully; tests do not model multiplayer execution.

Implementation decisions (authorized bugfix scope): preserve the current workspace and prior edits; use existing publicized game references for native failed-grab ClientRpc; remove jetpack-possession-only violation conditions; use coherent object ownership for inventory evidence; compare world positions; respect sender/target ownership. Separate belt-bag contention from contents checks. Tests must demonstrate RED before fixes and cover genuine violations as well as allowed paths.

Verification commands: `dotnet run --project tests/GrabObjectRegression --no-restore`; repeat with `-p:LegacySlots=true`; `dotnet run --project tests/BeltBagRegression`; `dotnet build AntiCheat/AntiCheat.csproj -c Release --no-restore`; `git diff --check`.

Tool issue: PowerShell execution policy blocked the plan resolver script; no `.planning` directory/selectors present, so the existing root plan was reused after direct inspection. No policy setting changed.

Final verification: 54 v81 grab + 38 legacy fallback + 28 bag cases passed (120 executions); Release build exited 0 with 55 NU1603/Harmony003 warnings and 0 errors; diff check passed. Final independent reviewer confirmed the slot-lookup failure fix and reported no remaining blocking findings. DLL: `AntiCheat/bin/Release/netstandard2.1/AntiCheat.dll`.

Remaining runtime validation: host + remote-player reproduction using the updated DLL, actual Harmony/Unity behavior, and the original reported sequence. No in-game validation or installation was claimed or performed.

## 2026-10-31: AstaAntiCheat Thunderstore release

User explicitly authorized renaming the mod, fork attribution, preparing `thunderstore/` including `thunderstore.zip`, and publishing that ZIP on Thunderstore. User supplied `thunderstore/icon.png`; preserve and use it.

- [x] Inspect metadata, docs, current Thunderstore format, connected publishing options, and browser account.
- [x] Rename display/assembly/package to AstaAntiCheat 1.0.0; preserve internal AntiCheat GUID/config compatibility and author attribution.
- [x] Write fork README/changelog/manifest, build Release, assemble DLL and locales with the supplied icon.
- [x] Validate ZIP structure, manifest, image, DLL metadata and file hashes; review package.
- [x] Finish with the updated ZIP only, per the user's later instruction; Thunderstore publication cancelled before submission.

Release layout: package metadata at ZIP root; `BepInEx/plugins/AstaAntiCheat/AstaAntiCheat.dll` and adjacent `locales/*.json`. No upstream AntiCheat dependency, game binaries, test artifacts, personal logs, or nested release ZIP. Source link stays `https://github.com/P-Asta/AntiCheat`.

Official packaging rules verified at `https://wiki.thunderstore.io/mods/creating-a-package`; dependency `BepInEx-BepInExPack-5.4.2305` verified on the package page. No Thunderstore connector was found; use browser UI. An existing completed upload page shows team `asta`; use a new upload tab to avoid altering the previous Replay result.

Prior upload attempt: browser chooser could not attach the ZIP because the ChatGPT Chrome extension lacked file-URL access. Native UI fallback then stopped for that turn because the tool could not confidently determine the browser URL. Publication was NOT completed. Package review had no blocking findings.

User replaced `thunderstore/icon.png`; confirmed PNG 256x256, SHA-256 `80980973efbb516decb494eaf1b71021f37e133887cff24755fa9db0a0adb68a`. Rebuilt ZIP from the same nine-file allowlist and verified every archived file matches staging. Resume authorized publication if browser access works.

Final user preference: "ZIP만 준비해줘". Do not continue publication without a new request. Closed the agent-created upload tab. Updated ZIP is 145426 bytes, SHA-256 `dfc8d75581a26b44b29ebf9465b16342c89d69c2503e2bfa9fe3c1e6e49c1bf6`; all nine archived files match staging and ZIP integrity passed.

## 2026-10-31: BetterStamina detection feasibility

User asks whether BetterStamina 1.0.2 can be blocked without accusing legitimate TZP effects or health-cost extra sprint.

- [x] Inspect existing detection features and exact package documentation; no stamina detector exists.
- [x] Inspect the shipped BetterStamina 1.0.2 DLL and native stamina/TZP/extra-sprint paths.
- [x] Determine which state is actually observable on the host and review false-positive constraints.
- [x] Record evidence and give a concrete feasibility recommendation; only implement enforcement if the available evidence supports the requested reliability.

Reuse the existing root planning files. Current production/package files remain unchanged during investigation. The legacy Thunderstore URL could not be opened by web tool; canonical thunderstore.io version URL works. Read-only game audit delegated separately from package analysis.

## 2026-10-31: Invalid request blocking and notifications

User expanded the request to protections and notifications comparable to SkidPwner/LobbyGuard. Implement the validated gaps in the existing C#/Harmony host mod. Preserve dirty workspace and existing API/config compatibility; no game install, git reset, upload or new production dependencies. This continues already authorized development in place.

Design: validate RPC structure and current host content indices before gameplay/logging; never identify BetterStamina from elapsed sprint time or remote sprintMeter. Default new RpcValidationSetting Enable=true, Kick=false. A rejected request notification names the authenticated sender, operation and localized reason without claiming a particular installed mod. Existing notification destination remains configurable. Fix GUI destination to show an actual game notification and throttle duplicate notifications for ten seconds with bounded state.

Shared interface: internal AntiCheat.Patches.RpcGuard.Enabled; bool RpcGuard.Reject(PlayerControllerB player, string rpc, string reasonKey, bool legacyKick=false). Reason keys: RpcInvalidIndex, RpcInvalidCredits, RpcInvalidCount, RpcMalformedPayload, RpcChatIdentity, RpcInvalidChat. Reject returns false, reports through ShowMessage using stable sender/operation/reason key, and kicks once if the new feature or an enabled legacy ChatReal Kick requests it. Never pass untrusted payloads into notification formatting.

- [x] Terminal/routing: regression tests (RED), minimal guards (GREEN) for item/vehicle/unlockable/moon indices, null/truncated/oversized arrays, negative credits and sender handling. Keep free routes and warranty paths legal; no hardcoded content IDs or blanket remote purchase ban. Restore original reader cursor on every path.
- [x] Chat: production-prefix regressions (RED/GREEN) for encoded string length before allocation, malformed data, actor bounds and impersonation; accept legitimate 50-character messages and content that merely mentions mod names/TMP tags. Honor feature toggles and restore reader cursor.
- [x] Notifications: production-method regression tests (RED/GREEN), shared guard/config/locales; show GUI via native DisplayTip, retain HostChat/PublicChat, permit alerts again after cooldown and avoid consuming alerts before HUD exists.
- [x] Review current movement RPC compatibility separately; avoid inventing sender identity for modern universal RPCs.
- [x] Run all suites, Release build, independent review and diff checks. Document exact protections and host-only stamina limitations; update thunderstore folder/ZIP to version 1.0.1 after verified changes.

Verification: existing 54/38/28 grab/bag suites plus new terminal, chat and notification suites; Release build; locale parsing; package file identity/integrity. No live multiplayer or actual malicious client is available, so do not claim those validations.


Final verification: grab54 + legacy-grab38 + bag28 + terminal112 + chat50 + notifications16 + real Harmony compatibility6 = 304 executions, all passed. Final Release build0 errors/75 NU1603-Harmony003 warnings. Independent review confirmed blocking findings fixed, including notification/disconnect failure rejection. Live gameplay remains unverified. No publishing performed.
Final 1.0.1 package: 152368 bytes; ZIP SHA256 1855d45279721f6a03664299a1bc55a0cfba3e547c714ca914a0ee4c6be3477d; DLL SHA256 ece23530a5c3e316b39bc8d95ceeea4d9b056aa49f1cf22587777ee7bb93f1a1. Nine entries match staging and latest Release DLL; PNG unchanged256x256; ZIP integrity/manifest/locale JSON passed.
