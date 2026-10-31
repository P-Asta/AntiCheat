# AstaAntiCheat

[English](../README.md) | **한국어**

[chuxiaaaa/AntiCheat](https://github.com/chuxiaaaa/AntiCheat)를 기반으로 [P-Asta](https://github.com/P-Asta)가 관리하는 Lethal Company 호스트용 안티치트 포크입니다.

플레이어의 행동과 게임 요청을 검사하고, 설정에 따라 알림·차단·강퇴를 적용합니다. 호스트에만 설치하면 되며, 참여하는 플레이어에게는 모드가 필요하지 않습니다.

## 기능

- 아이템 획득 거리, 빈 슬롯, 양손 아이템, 벨트 가방 검사
- 구매·크레딧·행성 이동·채팅 작성자 검증 및 손상된 요청 처리
- 전투·아이템 사용·함선 조작·월드 상호작용 검사
- 감지 알림, 지원되는 검사별 강퇴 설정, 플레이어 행동 로그
- 방 이름 접두사, 입장 메시지, 다국어 알림 설정
- 비정상적으로 긴 질주가 반복될 때 호스트 전용 관찰 알림

### 이 포크의 변경점

- 게임의 utility slot 처리와 한손 제트팩 호환성을 개선했습니다.
- 동시 아이템 획득과 불일치한 인벤토리 상태로 인한 오탐을 줄였습니다.
- 요청 검증과 알림 처리를 보완하고, 구형 게임 메서드가 없는 경우의 호환성을 개선했습니다.
- TZP 효과, 피해로 인한 스태미나 회복, 불확실한 이동 상태를 제외하는 반복 질주 관찰을 추가했습니다.

질주 관찰은 호스트에게 참고용으로 알리며 강퇴하거나 이동을 차단하지 않습니다. BetterStamina 등 특정 스태미나 모드의 설치를 증명하는 기능은 아닙니다. 버전별 변경 사항은 [변경 기록](../thunderstore/CHANGELOG.md)을 참고하세요.

## 설치

1. [BepInExPack](https://thunderstore.io/c/lethal-company/p/BepInEx/BepInExPack/)을 설치합니다. 패키지 의존성 버전은 `5.4.2305`입니다.
2. 원본 AntiCheat를 비활성화하거나 제거합니다. 수동으로 설치했다면 기존 `AntiCheat.dll`을 제거합니다.
3. 배포 ZIP의 `BepInEx` 폴더를 게임 폴더에 압축 해제합니다. 아래처럼 DLL과 언어 파일을 함께 배치하세요.
4. 게임을 실행하고 호스트로 방을 만듭니다.

```text
BepInEx/plugins/AstaAntiCheat/
├─ AstaAntiCheat.dll
└─ locales/
   ├─ en_US.json
   ├─ ko_KR.json
   ├─ zh_CN.json
   └─ localization_cfg.json
```

원본 AntiCheat는 의존성이 아닙니다. 원본과 이 포크는 같은 플러그인 ID를 사용하므로 함께 설치하지 마세요. 기존 `AntiCheat.cfg` 설정은 계속 사용할 수 있습니다.

## 설정

게임을 한 번 실행하면 `BepInEx/config/AntiCheat.cfg`가 생성됩니다. 각 검사의 활성화 여부와 지원되는 알림·강퇴 옵션을 이 파일에서 설정합니다.

| 설정 | 설명 |
| --- | --- |
| `[DetectedMessageType] Type` | `GUI`(호스트 경고창), `HostChat`(호스트 채팅), `PublicChat`(공개 채팅). 기본값: `PublicChat`. |
| `[RpcValidationSetting] Enable` | 비정상 요청 검사. 기본값: `true`. |
| `[RpcValidationSetting] Kick` | 비정상 요청 시 강퇴. 기본값: `false`. 다른 검사는 각각의 설정을 따릅니다. |
| `[StaminaObservationSetting] Enable` | 반복 질주 관찰 알림. 기본값: `true`. 항상 호스트 채팅에만 표시하며 강퇴 옵션은 없습니다. |

추가 요청 검사를 꺼도 게임이 읽을 수 없는 형식과 안전하지 않은 인덱스는 알림 없이 거절합니다. 질주 관찰은 TZP 사용·잔류 효과, 피해, 불확실한 이동 상태에서 누적 근거를 초기화합니다. 스태미나 변조를 놓치거나 정상 플레이를 의심하는 경우가 있을 수 있습니다.

### 언어

`BepInEx/plugins/AstaAntiCheat/locales/localization_cfg.json`의 `current_language`를 `en_US`, `ko_KR`, `zh_CN` 중 하나로 설정합니다. 비워 두면 시스템 언어에 따라 선택합니다.

새 번역을 추가하려면 `en_US.json`을 복사하고 키와 치환 문자를 유지한 채 메시지를 번역하세요. `current_language`에는 새 파일명에서 `.json`을 뺀 값을 지정합니다.

## 호환성

현재 변경 사항은 v81 게임 코드를 기준으로 확인했습니다. 대상 게임 메서드가 없는 구형 검사는 건너뜁니다. 모든 치트나 모드 조합을 지원하지 않으며, 최신 변경 사항의 실제 멀티플레이 검증은 아직 완료되지 않았습니다.

오탐이 발생하면 BepInEx와 AstaAntiCheat만 활성화한 별도 프로필에서 재현되는지 확인해 주세요. [문제 제보](https://github.com/P-Asta/AntiCheat/issues)에는 재현 순서, 게임·모드 버전, 관련 설정과 로그를 함께 남겨 주세요.

## 기여

버그 제보, 번역, 풀 리퀘스트를 환영합니다. 기술적인 내용은 [아이템 획득 검증 분석](grab-validation-audit-ko.md)과 [요청 검증·질주 관찰 분석](rpc-validation-audit-ko.md)에 정리되어 있습니다. 회귀 테스트 프로젝트는 [tests](../tests/)에서 확인할 수 있습니다.

## 기여자

- 원본 개발: [chuxiaaaa/AntiCheat](https://github.com/chuxiaaaa/AntiCheat) 및 [원본 기여자](https://github.com/chuxiaaaa/AntiCheat/graphs/contributors)
- 포크 관리 및 한국어 번역: [P-Asta](https://github.com/P-Asta)
- 게임 번역: 중국어 — chuxiaaaa, CoolLKKPS / 영어 — DeathWrench, CoolLKKPS, glyphical / 한국어 — P-Asta
- 원본 README 번역: 중국어 — chuxiaaaa, CoolLKKPS / 영어 — NilaierMusic, CoolLKKPS / 러시아어 — NilaierMusic / 한국어 — P-Asta

원본 테스터: 超级骇人鲸, Alan Backer, 沈阳最速傳說疾走の猛虎！貴物刀一郎, 柒小鸭 yz, 喜欢睡觉の极茶龙, 东南枝, Melissa, 我不吃牛肉.

원본의 [중국어](README-zh-CN.md)와 [러시아어](README-ru.md) 문서도 보존하고 있습니다.
