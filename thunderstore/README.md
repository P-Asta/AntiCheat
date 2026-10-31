# AstaAntiCheat

[English](https://github.com/P-Asta/AntiCheat/blob/main/README.md) | [한국어](https://github.com/P-Asta/AntiCheat/blob/main/docs/README-ko.md)

A host-side anti-cheat mod for Lethal Company, forked from [chuxiaaaa/AntiCheat](https://github.com/chuxiaaaa/AntiCheat) and maintained by [P-Asta](https://github.com/P-Asta).

AstaAntiCheat provides configurable checks for player actions and game requests. Only the host needs to install it; joining players do not need the mod.

## Features

- Inventory checks for pickup distance, available slots, two-handed items and belt bags.
- Purchase, credit, moon-route and chat-author validation, including malformed request handling.
- Configurable checks for combat, item use, ship controls and world interactions.
- Detection messages, optional kicks for supported checks, and player action logs.
- Custom lobby prefixes, join messages and localized notifications.
- Host-only notices for repeated unusually long sprints.

### Changes in this fork

- Improved native utility-slot handling and one-handed jetpack compatibility.
- Reduced false positives from simultaneous grabs and inconsistent inventory state.
- Improved request validation, notification handling and compatibility with missing legacy game methods.
- Added repeated-sprint observations with exclusions for TZP effects, damage-related stamina recovery and uncertain movement.

Sprint notices are informational and never kick players or block movement. They do not prove that BetterStamina or another stamina mod is installed. See the [changelog](https://github.com/P-Asta/AntiCheat/blob/main/thunderstore/CHANGELOG.md) for release details.

## Installation

1. Install [BepInExPack](https://thunderstore.io/c/lethal-company/p/BepInEx/BepInExPack/) (package dependency: `5.4.2305`).
2. Disable or remove the original AntiCheat. For a manual installation, remove its `AntiCheat.dll`.
3. Extract the release ZIP's `BepInEx` folder into the game directory, keeping the DLL and localization files together as shown below.
4. Launch the game and host a lobby.

```text
BepInEx/plugins/AstaAntiCheat/
├─ AstaAntiCheat.dll
└─ locales/
   ├─ en_US.json
   ├─ ko_KR.json
   ├─ zh_CN.json
   └─ localization_cfg.json
```

The original AntiCheat is not a dependency. Both mods use the same plugin ID, so they must not be installed together. Existing `AntiCheat.cfg` settings remain compatible.

## Configuration

Launch the game once to generate `BepInEx/config/AntiCheat.cfg`. Enable checks and configure their available notification and kick options in this file.

| Setting | Description |
| --- | --- |
| `[DetectedMessageType] Type` | `GUI`, `HostChat` or `PublicChat`. Default: `PublicChat`. |
| `[RpcValidationSetting] Enable` | Invalid-request checks. Default: `true`. |
| `[RpcValidationSetting] Kick` | Kicks for invalid requests. Default: `false`; other checks have their own settings. |
| `[StaminaObservationSetting] Enable` | Repeated-sprint notices. Default: `true`; always local host chat, with no kick option. |

Unreadable request formats and unsafe indices are still rejected silently when the additional request checks are disabled. The sprint observer clears evidence around TZP use and residual effects, damage, and uncertain movement. It can miss modified stamina and can produce false positives.

### Language

Edit `BepInEx/plugins/AstaAntiCheat/locales/localization_cfg.json` and set `current_language` to `en_US`, `ko_KR` or `zh_CN`. Leave it empty to select a language from the system locale.

To add a translation, copy `en_US.json`, translate its messages while preserving the keys and placeholders, and set `current_language` to the new filename without `.json`.

## Compatibility

The current changes were checked against v81 game code. Some legacy checks are skipped when their game methods are unavailable. This mod cannot detect every cheat or guarantee compatibility with every mod; live multiplayer validation of the latest changes is still pending.

For false positives, try a separate profile with only BepInEx and AstaAntiCheat. Include reproduction steps, game and mod versions, relevant settings and logs when [reporting an issue](https://github.com/P-Asta/AntiCheat/issues).

## Contributing

Bug reports, translations and pull requests are welcome. Technical notes are available in the [inventory validation audit](https://github.com/P-Asta/AntiCheat/blob/main/docs/grab-validation-audit-ko.md) and [request and sprint observation audit](https://github.com/P-Asta/AntiCheat/blob/main/docs/rpc-validation-audit-ko.md) (Korean). Regression projects are in [tests](https://github.com/P-Asta/AntiCheat/tree/main/tests).

## Credits

- Original mod: [chuxiaaaa/AntiCheat](https://github.com/chuxiaaaa/AntiCheat) and its [contributors](https://github.com/chuxiaaaa/AntiCheat/graphs/contributors).
- Fork maintenance and Korean translation: [P-Asta](https://github.com/P-Asta).
- In-game translations: Chinese — chuxiaaaa, CoolLKKPS; English — DeathWrench, CoolLKKPS, glyphical; Korean — P-Asta.
- Original README translations: Chinese — chuxiaaaa, CoolLKKPS; English — NilaierMusic, CoolLKKPS; Russian — NilaierMusic; Korean — P-Asta.

Thanks to the original testers: 超级骇人鲸, Alan Backer, 沈阳最速傳說疾走の猛虎！貴物刀一郎, 柒小鸭 yz, 喜欢睡觉の极茶龙, 东南枝, Melissa and 我不吃牛肉.

The original [Simplified Chinese](https://github.com/P-Asta/AntiCheat/blob/main/docs/README-zh-CN.md) and [Russian](https://github.com/P-Asta/AntiCheat/blob/main/docs/README-ru.md) documentation is also preserved.
