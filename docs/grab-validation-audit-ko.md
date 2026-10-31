# 아이템 획득 오탐 분석 — 2026-10-31

설치된 게임 코드를 ILSpy로 확인하고 아이템 획득, 슬롯 전환, 가방 검사를 수정했다. 사용자가 보고한 **utility slot을 먼저 채운 뒤 아이템을 주울 때의 정확한 사건**은 현재 로그에서 재현 근거를 찾지 못했다. 아래의 코드상 문제와 방어 조치를 해당 사건의 확정 원인으로 혼동하지 않는다.

## 분석 대상

- Steam 설치 게임의 `Lethal Company_Data/Managed/Assembly-CSharp.dll`, Steam build `22825947`.
- SHA-256: `5f7db5538b78dc408845a3002907619785ac9f9c6b6059d13dc9a602d9b65731`.
- MVID: `aca1e98d-6f84-4d3f-85cd-22b6f7be2f9b`.
- ILSpy/ICSharpCode.Decompiler `11.0.0.9375`. 설치 로그의 BepInEx는 `5.4.23.5`.
- 분석 타입: `PlayerControllerB`, `GrabbableObject`, `JetpackItem`, `BeltBagItem`, 설치된 Unity Netcode의 `NetworkBehaviour`, `FastBufferReader`.
- REA 관리형 메타데이터 증거: `ev_976fe5f3e38cce5e48f7896e99c75dea3ff16283844792d75fd3ec0eacb4e793`.
- 전체 디컴파일 파일은 로컬 `%TEMP%/anticheat-grab-audit-20261031`에만 보관했다. 저장소에 게임 소스를 복제하지 않는다.

## 확인 결과와 변경

| 확인한 흐름 | 문제와 변경 |
| --- | --- |
| `FirstEmptyItemSlot(GrabbableObject)` | 도구가 적합하면 utility slot `50`, 그 외 빈 일반 슬롯을 반환한다. 이 게임 함수를 그대로 사용하며 고정 최대 개수로 추측하지 않는다. 해당 API가 없는 구버전은 일반 슬롯을 확인한다. |
| `BeginGrabObject`, `Interact_performed` | 한손 제트팩의 단순 소지는 양손 아이템 획득 금지 조건이 아니다. 제트팩을 갖고 있다는 이유만으로 차단하던 조건을 제거했다. 실제 활성 양손 아이템과 추가 양손 아이템 검사는 유지한다. |
| `GrabObjectServerRpc`, `GrabObjectClientRpc` | 이미 다른 요청이 선점한 아이템은 게임이 정상 거절한다. 용량·양손·거리 위반으로 중복 판정하지 않고 원본에 넘긴다. |
| RPC 수신 핸들러의 소유권 검사 | 안티치트 prefix도 sender/target 소유권과 플레이어 일치를 먼저 확인한다. 다른 플레이어 대상으로 들어온 요청에 잘못된 알림·강퇴·취소를 적용하지 않는다. |
| `SwitchToItemSlot`, `SetObjectAsNoLongerHeld` | 슬롯 참조와 서버 보유 상태가 모순되거나 같은 객체가 중복 기록된 상태는 확정 초과로 취급하지 않는다. 공간이 없고 제한이 켜져 있으면 요청만 취소하며 치트 알림·강퇴를 하지 않는다. 이미 보유 목록에 있는 객체의 재획득도 조용히 취소한다. |
| 슬롯 조회 실패 | 잘못된 현재 슬롯 등으로 게임의 조회 함수가 예외를 내면 즉시 실패 응답을 보낸다. 원본이 먼저 소유권을 바꾼 뒤 같은 조회에서 다시 실패하는 상황을 막는다. |
| 위치 송신·보간 코드 | `serverPlayerPosition`은 보통 부모 기준 로컬 좌표다. 월드 좌표로 변환하고 현재 렌더링 위치에서도 100m를 초과해야 거리 위반으로 판단한다. 기존 착륙·출발·텔레포트 예외를 유지한다. |
| 실패한 획득 응답 | 원래 아이템 참조로 `GrabObjectClientRpc(false, reference)`를 호출한다. private RPC 단계 값을 강제로 바꾸거나 ServerRpc를 재호출하지 않는다. |
| `BeltBagItem.TryAddObjectToBagServerRpc` | 다른 플레이어·적·가방이 선점한 아이템은 정상 경쟁으로 처리한다. 실제 scrap/금지 아이템 제한은 유지하고 취소 대상 인덱스는 실제 송신자와 검증한다. 가방 RPC의 `RequireOwnership=false`를 존중한다. |
| `FastBufferReader` | 복사본도 내부 커서를 공유한다. 검사 후 항상 진입 시점의 커서로 복원하며 0으로 고정하지 않는다. |

호스트의 ClientRpc는 확인한 Netcode 구현에서 동기 실행된다. 따라서 단순히 “호스트의 ClientRpc가 늦어서 슬롯이 어긋난다”는 가정으로 별도 인벤토리 장부나 타이머를 추가하지 않았다. 불일치 상태 처리는 방어 조치이며, 실제 네트워크 지연이 이번 사건의 원인이라는 뜻은 아니다.

## 설정과 검증 범위

기존 설정 키와 메시지 키를 유지한다. `GrabObject_TwoHand` 및 전체 기능을 끄면 해당 슬롯 전환 제한도 적용하지 않는다. 명확한 위반의 알림은 `GrabObject_SendLog`, 차단은 해당 제한, 강퇴는 `GrabObject_Kick` 설정을 따른다. 판정할 수 없는 상태를 이유로 강퇴하지 않는다.

검증 명령:

```powershell
dotnet run --project tests/GrabObjectRegression --no-restore
dotnet run --project tests/GrabObjectRegression --no-restore -p:LegacySlots=true
dotnet run --project tests/BeltBagRegression --no-restore
dotnet build AntiCheat/AntiCheat.csproj -c Release --no-restore
git diff --check
```

테스트는 실제 생산 prefix를 컴파일하고 Unity/네트워크 경계만 대체한다. API가 없는 구버전 경로도 별도로 확인하지만 실제 v73 게임 실행을 대신하지는 않는다. 실제 Unity의 destroyed-object null 동작, Harmony 로딩, 멀티플레이 전체 동기화는 실게임 확인이 남아 있다. 재현 순서는 `tests/GrabObjectRegression/README.md`와 `tests/BeltBagRegression/README.md`에 있다.

최종 실행 결과: v81 경로 54개, 구버전 대체 경로 38개, 가방 경로 28개가 모두 통과했다. Release 빌드는 오류 0개, 경고 55개로 성공했고 `git diff --check`도 통과했다. 독립 리뷰의 슬롯 조회 실패 문제를 추가 테스트로 재현한 뒤 수정했다.

결과물: `AntiCheat/bin/Release/netstandard2.1/AntiCheat.dll`.
SHA-256: `0B87D278EF5D2EFEEA3664EF30FB39DB3B69D1456D4E6DD50CE328E75A0A9941`.

프로젝트는 v73 참조를 요청하지만 로컬 캐시의 `81.0.5-ngd.0`으로 빌드되어 `NU1603`이 발생한다. 기존 프로젝트의 `Harmony003` 경고도 남아 있다. 의존성 선언과 게임 설치 파일은 변경하지 않았다.
