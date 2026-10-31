# Changelog

## 1.0.2

- 반복 질주 의심 관찰을 기본 활성화로 추가했습니다. `[StaminaObservationSetting] Enable`로 끌 수 있으며 강퇴 설정은 없습니다.
- 실행 중 질주 시간·이동 속도 기준의 넉넉한 시간·거리 조건을 모두 넘긴 뒤, 안전한 2초 휴식으로 확인된 구간이 300초 안에 3회 누적되어야 알립니다. 알림 간격은 최소 120초이며 새 근거가 필요합니다.
- TZP·피해·불확실한 이동 상태는 누적 관찰을 초기화합니다. 처음 또는 초기화 후 안전한 2초 휴식을 요구하며, 피해·TZP 이후에는 `max(10초, 2 × sprintTime)` 유예를 적용합니다.
- 의심 알림은 다른 감지의 `PublicChat` 설정과 관계없이 호스트의 로컬 채팅에만 표시합니다. 이동 차단·강퇴·스태미나 및 체력 변경은 하지 않습니다.

- Add an enabled-by-default repeated-sprint suspicion observer, controlled by `[StaminaObservationSetting] Enable`, with no kick option.
- Require both duration above `2 × sprintTime` and horizontal distance above `2 × sprintTime × movementSpeed × 2.25`, using runtime values. Confirm each bout after two seconds of safe rest; require three separate bouts within 300 seconds and a 120-second notification cooldown with fresh evidence.
- Reset accumulated evidence for TZP, damage and uncertain movement/state. Require two seconds of safe rest to start or resume and an additional `max(10 seconds, 2 × sprintTime)` grace period after damage/TZP.
- Send notices only to local host chat, ignoring other detections' `PublicChat` setting. Never block movement, kick or change stamina/health through this observer.

이 알림은 진단용 추정이며 BetterStamina 설치나 치트를 증명하지 않습니다. 작은 변경, 회복 속도만 바꾸는 모드, 무거운 짐을 든 플레이어 또는 쉬지 않는 단일 질주는 놓칠 수 있고, 오탐 가능성도 남습니다.

Notices are diagnostic heuristics, not proof of cheating or BetterStamina installation. Small changes, faster recovery alone, heavily loaded players and a single uninterrupted sprint may be missed; false positives remain possible.

검증 / Validation: 회귀 검증 411회 통과, Release 빌드 오류 0개·기존 경고 75개. 411 regression executions passed, including real Harmony observation hooks with all/some/no optional targets. Release build succeeded with zero errors and 75 existing NU1603/Harmony003 warnings. Live multiplayer validation remains pending.

## 1.0.1

- Validate purchase, vehicle, unlockable and moon IDs before indexing host content.
- Reject malformed/truncated purchase and chat payloads; bound encoded arrays and strings before allocation.
- Add configurable invalid-request notifications and optional kicks (disabled by default).
- Check chat author identity against the authenticated sender; preserve legitimate text, languages and cooldown settings.
- Preserve original reader positions, normal free-item/free-route/warranty flows and host exemptions.
- Display actual game tips in GUI mode and limit repeated notifications to once per ten seconds.
- Skip five unavailable legacy RPC handlers with compatibility warnings, retaining other patches. These checks do not cover their replacement RPCs on newer versions.
- Do not infer stamina cheating from remote sprint meters or sprint duration; TZP and damage recovery remain outside these guards.

Validation: 304 regression executions passed; Release build succeeded with 75 NU1603/Harmony003 warnings and no errors. Live multiplayer validation remains pending. This release does not implement transport-wide or voice-packet protection.

## 1.0.0

- First AstaAntiCheat release, forked from chuxiaaaa's AntiCheat.
- Use the game's item-aware slot lookup, including the native utility slot.
- Avoid overflow accusations for contradictory or duplicate inventory references.
- Correct false two-hand enforcement caused by carrying a one-handed jetpack.
- Treat contested grabs and belt-bag requests as normal game rejections.
- Compare synchronized and rendered player positions in world space for distance checks.
- Respect RPC ownership, preserve read cursors, and cancel failed grabs with the original item reference.
- Cancel failed slot lookups without accusing or kicking the player.
- Keep existing AntiCheat configuration compatibility and Korean, English and Chinese localization.

Validation: 54 current-game grab checks, 38 legacy API fallback checks and 28 belt-bag checks passed. Live multiplayer verification is still pending.
