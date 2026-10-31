# Progress

## 2026-10-31: Thunderstore publication

User explicitly requested upload, superseding ZIP-only preference. Submitted verified1.0.2 ZIP(158449 bytes, SHA2569fa827c4df650a45130d9c8bee33a428effdadc69733b5c1366527387ede104b) through the authenticated asta team. Upload UI Success confirmed; public https://thunderstore.io/c/lethal-company/p/asta/AstaAntiCheat/ shows Latest version1.0.2, installation/download links and revised English README with Korean link. Categories: AI Generated(existing), BepInEx, Bug Fixes, Mods, Server-side. No package changes, security permission changes, or duplicate submission. Publication screenshot saved to C:/Users/pAsta/.codex/visualizations/2026/10/30/01a18d89-ebd8-7ac2-830c-31433e5719aa/thunderstore-published-1.0.2.jpg.

## 2026-10-31: README language separation

Replaced mixed-language technical-report README with conventional English repository documentation and matching Korean docs/README-ko.md. Kept docs/README-en.md as navigation for old English links, retained original credits/tester names and existing Chinese/Russian documents, and linked detailed audits instead of copying counts/formulas into the introduction. Thunderstore README uses the same English text with absolute repository links.48 links, language separation, code fences/UTF-8 and diff check passed; independent review found no issues. No code changes/tests/build needed. Rebuilt ZIP:158449 bytes, SHA256 9fa827c4df650a45130d9c8bee33a428effdadc69733b5c1366527387ede104b; all9 entries verified, only README changed, version1.0.2 and all other package files preserved. No upload.

## 2026-10-31 follow-up: repeated sprint observation

User accepted suspicion-only host notifications. Implemented a pure tracker, a read-only native-state adapter, a separate local notification route, default-enabled setting and three locale messages. Updated version to1.0.2. Independent review found that replacing the tracker on parent changes discarded its120-second cooldown; reproduced and fixed while preserving evidence resets. Native audit also identified sticky remote teleport flags; handled by rising edge plus actual teleport event. Additional regressions cover subtle timescale changes and remote slime audio synchronization. No game fields or enforcement settings are modified by the observer.

Fresh executed results: grab54 + legacy-grab38 + bag28 + terminal112 + chat50 + notifications21 + legacy real-Harmony6 + tracker43 + observer44 + observation real-Harmony15 =411 passed. Production Release build succeeded with0 errors/75 pre-existing NU1603/Harmony003 warnings. Real observation Harmony hooks passed all9/mixed4/none2. Independent review has no remaining important findings. Actual Unity/multiplayer behavior remains untested.

Final package1.0.2:160155 bytes; ZIP SHA256 b3eb5421d4176fac60403e0214914cf9677617462d9545cd31ac276a1f88f353; DLL SHA256 48ba724dbc7e013f5f23c35170ffa458fb0c26b949b997cd216ee906a6a0b7ee. All9 entries match staging/current Release DLL/locales; manifest/locale JSON and ZIP integrity passed; user icon hash unchanged256x256. ILSpy verified plugin GUID AntiCheat/name AstaAntiCheat/version1.0.2. Documentation records limits and411 executed checks. No publishing performed.

Environment repair: cached net472 assets referenced an old CodexSandboxOffline profile, causing MSB3644/missing Harmony assemblies. Restoring that test project from the existing C:/Users/pAsta/.nuget/packages cache (NuGetAudit=false) corrected generated assets, without dependency or source changes. No uploads, commits or game installation changes.

## 2026-10-31 follow-up: invalid RPC guards

User requested BetterStamina feasibility with TZP/damage exclusions, then abnormal-action blocking/notifications inspired by SkidPwner and LobbyGuard. Root owns shared plan/config/locales/reporting; workers implemented terminal, chat and optional legacy compatibility. Independent reviewer identified packed-scalar double mismatch and legacy ChatReal Kick regression; corrected. Discount rounding edge is being finalized.

Fresh checks so far: grab54, legacy-grab38, beltbag28; chat50, notification14; real Harmony2 each none/all/mixed (6 total). Locale keys/placeholders valid for3 languages; diff check passed. Preliminary Release build0 errors,73 NU1603/Harmony003 warnings; final build/package pending latest review. Terminal worker105/105 before rounding follow-up. Prepare ZIP only, no upload.

Handled errors: legacy Thunderstore URL unavailable -> canonical URL; sandbox network blocked public download -> escalated read/download succeeded. Incorrect initial apply_patch file/context rejected before mutation -> corrected. Real Harmony empty-target fallback reproduced and gated. No game installation or security setting changes.

- Read project structure and grab validation flow.
- Investigating v81 signatures and native capacity selection before editing production code.
- dotnet --info reports SDK 10.0.203 but fails querying Windows services in sandbox; build availability still to be checked.
- Ran actual prefix against game boundary doubles: original failed four cases; fix passed all 12 initial cases.
- Independent review found an unintended enforcement expansion for a stowed two-handed jetpack. Added a failing regression and separated message-suppression state from original enforcement flags.
- Final v81 and missing-v81-API fallback runs each passed 13 cases (26 total executions).
- Network NuGet restore failed. Restore from existing local NuGet cache succeeded; requested v73 reference resolved to cached 81.0.5-ngd.0 with NU1603. No dependency declarations changed.
- Release build succeeded against cached v81 references; Harmony003 analyzer warnings remain. git diff --check passed.
- No game/multiplayer runtime test performed. User's precise occupied-utility-first sequence did not fail in original isolated prefix; see test README for runtime steps. No game installation files changed.

## 2026-10-31 ILSpy audit and follow-up fixes

- Inspected actual installed game and NGO code with ILSpy; recorded fingerprint and method evidence in findings and Korean audit report.
- Parallel read-only game/repository audits established native routing, synchronous host ClientRpc handling, local/world mismatch, false jetpack possession rule, and native bag contention rejection.
- Test-first grab pass: 37 cases, 19 failures against earlier production code; all passed after corrections. Bag pass: 28 cases, 14 failures before fix, all passed afterwards.
- Added utility-first occupancy matrix, unknown/stale data, and settings combinations. Two additional RED cases exposed already-stored object acceptance and disabled two-hand enforcement leaking into a kick; fixed both.
- Independent final review found slot lookup failures with vacant regular slots could still reach native ownership mutation. Reproduced the actual out-of-range slot and simulated API exception in RED, then made all lookup failures cancel without accusation.
- Strengthened legacy boundary: remove both item-aware API and utility field; 38 applicable cases passed. v81 suite expanded to 54, passed before final consolidated verification. Bag suite 28 passed.
- Release/Debug builds passed against cached v81 references; existing NU1603/Harmony003 warnings remain. No dependency edits, game installation writes, commits, or live multiplayer session.
- Planning resolver script was blocked by local execution policy. Root planning files were inspected/reused directly; no policy configuration changed.
- Final consolidated verification: v81 54/54, legacy fallback 38/38, bag 28/28; Release build exit 0, 55 warnings/0 errors; `git diff --check` clean. Fresh reviewer confirmed the only Important finding resolved.
- Release DLL SHA-256: `0B87D278EF5D2EFEEA3664EF30FB39DB3B69D1456D4E6DD50CE328E75A0A9941`. Artifact path `AntiCheat/bin/Release/netstandard2.1/AntiCheat.dll`.
- `GameNetworkManager.gameVersionNum` has a source default of 1 and is scene-configured; do not infer the installed game version from that C# initializer. Binary identity/build ID and known v81 API/reference contract are the recorded analysis anchors.

## AstaAntiCheat release

- Renamed assembly/product/display to AstaAntiCheat 1.0.0; retained AntiCheat GUID/config/log compatibility. Updated fork attribution and preserved upstream Chinese README/credits.
- Prepared `thunderstore/` with manifest, README, CHANGELOG, user icon, DLL, four locale JSON files and `thunderstore.zip`. New DLL metadata/plugin constants verified; Release build passed with existing warnings.
- Independent review found no publishing blocker. Prior browser upload was not completed: file access permission blocked chooser, native fallback stopped on URL verification.
- User replaced the icon; verified new 256x256 PNG and rebuilt/validated all nine ZIP entries against staged files. Original publishing request remains active.
- User subsequently requested ZIP only. Publication stopped before any submission; agent-created upload tab closed. Final ZIP: `thunderstore/thunderstore.zip`, 145426 bytes, SHA-256 `dfc8d75581a26b44b29ebf9465b16342c89d69c2503e2bfa9fe3c1e6e49c1bf6`. New icon and all nine staged files match the verified archive.


Final verification: grab54 + legacy-grab38 + bag28 + terminal112 + chat50 + notifications16 + real Harmony compatibility6 = 304 executions, all passed. Final Release build0 errors/75 NU1603-Harmony003 warnings. Independent review confirmed blocking findings fixed, including notification/disconnect failure rejection. Live gameplay remains unverified. No publishing performed.
Final 1.0.1 package: 152368 bytes; ZIP SHA256 1855d45279721f6a03664299a1bc55a0cfba3e547c714ca914a0ee4c6be3477d; DLL SHA256 ece23530a5c3e316b39bc8d95ceeea4d9b056aa49f1cf22587777ee7bb93f1a1. Nine entries match staging and latest Release DLL; PNG unchanged256x256; ZIP integrity/manifest/locale JSON passed.
