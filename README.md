HollywoodFX — SPT 4.1.5 대응 포크

원작: https://github.com/SleepingPills/HollywoodFX
이 포크: SPT 4.1.5 (직전 4.0.10 대응 상태에서 이관)

**원작에는 README가 없습니다.** 그래서 아래 <모드 설명>은 번역이 아니라 코드와 설정
항목에서 제가 정리한 것이고, 원작자가 쓴 글이 아닙니다. 그 아래 <상세 변경점>이
이 포크에서 실제로 바꾼 내용입니다.

---

<모드 설명 — 원작에 README가 없어 코드에서 정리한 것>

전투 시각 효과를 통째로 갈아끼우는 클라이언트 플러그인입니다. 서버 모드는 없고
BepInEx 플러그인 하나로 동작합니다.

- **총구** — 화염 제트, 스파크, 연기. 무기 종류별로 다른 블라스트를 쓰고, 소음기
  유무에 따라 갈립니다. 총구 광원에 그림자를 켤 수 있습니다
- **탄착** — 재질별 임팩트 이펙트와 데칼을 게임 기본값 대신 자체 세트로 교체합니다.
  예광탄은 별도 임팩트를 씁니다
- **고어** — 혈흔 분사, 동맥 출혈, 피니셔 샷, 시신에 남는 상처 데칼, 환경에 튀는
  혈흔. 각각 크기와 방출량을 따로 조절합니다
- **폭발** — 자체 블라스트 시스템. 실내 여부를 반영하고, 뇌진탕 효과와 제압 효과가
  붙습니다
- **래그돌** — 시체 물리를 다시 잡습니다. 시네마틱 래그돌, 사망 시 무기 드롭
- **탄피 물리** — 튕김 회전과 충돌음 처리 강화 (기본 꺼짐)
- **화면** — 스코프 피사계 심도, 전투 중 블러
- **전장 분위기** — 먼지와 파편 파티클

F12 설정에서 거의 모든 항목을 조절할 수 있고, `(RESTART)` 표시가 붙은 것은 게임을
다시 켜야 반영됩니다. 데칼 한도, 컴퓨트 정밀도, 이펙트 품질 바이어스처럼 성능에
직접 영향을 주는 항목도 있습니다.

**빌드** — .NET SDK와 SPT 4.1 설치본이 필요합니다.

```
dotnet build
dotnet build -p:SptRoot="D:\내 SPT 경로"     # 기본값이 아닐 때
dotnet build -p:AutoInstall=false            # 설치본에 복사하지 않기
```

빌드가 끝나면 `BepInEx\plugins\HollywoodFX\`로 자동 복사됩니다. 게임이 켜져 있으면
DLL이 잠겨서 복사가 실패하는데, 빌드를 실패시키지 않고 경고만 남깁니다.

---

<26/08/30 상세 변경점>

- 게임 어셈블리 참조가 원작자 로컬 폴더 구조(`..\..\..\Client_Dev\...` 상대경로)로
  하드코딩되어 있어서 다른 환경에서는 어셈블리를 못 찾던 문제 — `SptRoot` 속성으로
  오버라이드 가능하게 수정 (HollywoodGraphics와 동일한 문제, 동일한 방식으로 수정)

- 결과: 정상적으로 빌드 가능

---

<26/09/07 상세 변경점 — SPT 4.1.5 대응>

**SPT 4.1은 클라이언트를 역난독화했습니다.** 4.0에서 `GClass680` 같던 타입들이 진짜
이름과 네임스페이스를 갖게 됐고, 4.0 시절의 부분 별칭(`~Class` 접미사)도 전부
바뀌었습니다. **4.0 클라이언트 모드는 4.1에서 하나도 로드되지 않습니다.**

## 이름 바뀐 것

공식 위키의 5,957줄짜리 매핑 표에 이 모드의 모든 식별자를 대조했습니다. 중첩 타입은
표에서 `Outer+Inner` 형태라 마지막 세그먼트 기준으로도 한 번 더 훑었습니다.

**타입 20개 / 38곳:**

| 4.0 | 4.1 |
| --- | --- |
| `AmmoItemClass` | `EFT.InventoryLogic.Ammo` |
| `AssaultRifleItemClass` / `MarksmanRifleItemClass` / `SniperRifleItemClass` | `EFT.InventoryLogic.AssaultRifle` / `MarksmanRifle` / `SniperRifle` |
| `PistolItemClass` / `RevolverItemClass` / `ShotgunItemClass` / `SmgItemClass` | `EFT.InventoryLogic.Pistol` / `Revolver` / `Shotgun` / `Smg` |
| `EftBulletClass` | `EFT.Ballistics.Shot` |
| `LayerMasksDataAbstractClass` | `EFT.Ballistics.BallisticsCalculatorConstants` |
| `CameraClass` | `EFT.CameraControl.CameraManager` |
| `GDelegate64` | `EFT.ShotDelegate` |
| `WeaponManagerClass` | `EFT.Firearms` |
| `NotificationManagerClass` | `EFT.Communications.NotificationManager` |
| `RagdollClass` | `EFT.Interactive.CorpseRagdoll` |
| `LightAllocationPoolClass` | `Systems.Effects.LightPool` |
| `LayerMaskClass` | `LayersMaskController` |
| `BodyRendererDataStruct` | `BodyRenderer` |
| `DeferredDecalRenderer+DeferredDecalMeshDataClass` | `DeferredDecalRenderer+ManagedMesh` |
| `DeferredDecalRenderer+DeferredDecalBufferClass` | `DeferredDecalRenderer+CameraData` |

**멤버 9개** — 위키 표는 타입만 다루고 메서드는 안 다룹니다. 번호가 밀린 이름들은
어셈블리를 덤프해서 **시그니처로 짝을 맞췄고**, 전부 후보가 하나뿐이었습니다:

| 4.0 | 4.1 | 근거 |
| --- | --- | --- |
| `TextureDecalsPainter.method_5` | `IsCorrectRenderer` | `bool (Renderer)` 유일 |
| `CorpseRagdoll.method_1` | `StopRigidbody` | `void (Rigidbody)` 유일 |
| `CorpseRagdoll.Func_0` | `_checkCorpseIsStill` | `Func<bool, float, bool>` 유일 |
| `CorpseRagdoll.RigidbodySpawner_0` | `_rigidbodySpawners` | `RigidbodySpawner[]` 유일 |
| `DeferredDecalRenderer.method_6` | `AddCubeToMesh` | 파라미터 5개 일치 |
| `DeferredDecalRenderer.method_7` | `CreateDecalMesh` | `void (SingleDecal)` 유일 |
| `DeferredDecalRenderer.dictionary_0` | `_meshesDict` | `Dictionary<Material, ManagedMesh>` |
| `DeferredDecalRenderer.dictionary_2` | `_cameras` | `Dictionary<Camera, CameraData>` |
| `Firearms.FirearmsEffects_0` | `FirearmsEffects` (속성) | |

**리플렉션 필드 5개** — 문자열이라 컴파일러가 못 잡고 라이드에서 터지는 것들:

| 4.0 | 4.1 | 타입만으로 찾을 수 있나 |
| --- | --- | --- |
| `BallisticsCalculator.gdelegate64_0` | `_shotDelegate` | ✅ |
| `Effects.lightAllocationPoolClass` | `_lightPool` | ✅ |
| `MuzzleManager.muzzleJet_0` | `__muzzleJets` (밑줄 **두 개**) | ✅ |
| `MuzzleManager.muzzleSmoke_0` | `_muzzleSmokes` | ✅ |
| `MuzzleManager.muzzleFume_0` | `_muzzleFumes` | ❌ `_launcherFumes`와 타입 동일 |

`ObfuscatedField`가 이름으로 먼저 찾고, 실패하면 타입으로 찾습니다. 마지막 줄이 이
장치를 만든 이유입니다 — `MuzzleFume[]` 필드가 둘이라 타입으로는 못 고릅니다. 그
경우 찍지 않고 "특정 불가"를 로그에 남기고 총구 이펙트만 끕니다. `__muzzleJets`의
밑줄 두 개도 함정입니다 (옆에 `GameObject[] _muzzleJets`가 따로 있음).

**Harmony 필드 주입 3개** — 파라미터 이름이라 컴파일러가 아예 못 봅니다:

| 4.0 | 4.1 | 근거 |
| --- | --- | --- |
| `AmmoPoolObject.float_0` | `c` | 그 클래스의 유일한 `float` |
| `WeaponPrefab.iplayer_0` | `_player` | 유일한 `IPlayer` |
| `Shell.vector3_2` | `_rotationVector` | 유일한 `Vector3` |

## 구조 수정: 패치 하나가 나머지를 죽이던 문제

`Enable()` 호출이 `Awake()`에서 줄줄이 이어져 있어서, **첫 실패가 메서드를 끝내고 그
아래 패치가 전부 실행되지 않았습니다.** 실제로 `AmmoPoolObject.float_0` 하나 때문에
**18개가 통째로 빠진 채로 돌았고**, 로그는 멀쩡해 보였습니다. 없는 기능은 자기가
없다고 로그를 남기지 않으니까요.

이제 각 `Enable()`을 감쌉니다. 못 붙는 패치는 이름과 함께 에러를 남기고 **나머지는
계속 붙습니다.** 마지막에 실패 목록을 한 줄로 모아 찍습니다 — 이 크기의 로그에서
개별 에러는 스크롤에 묻히고, 정작 봐야 할 건 "이 모드의 일부가 안 돌고 있다"입니다.

## 빌드 경로

`SptRoot` 기본값을 SPT 4.1 설치본으로 바꾸고, 게임과 BepInEx 위치를 추측하지 않고
탐색합니다 (루트 → `SPT_Runtime\` → `SPT\`, 게임과 BepInEx를 각각 따로). 참조를
못 찾으면 **어느 폴더에 뭐가 없는지 한 줄로** 말하고 멈춥니다. 빌드할 때 어느
어셈블리를 골랐는지도 한 줄 찍습니다.

설치본 복사를 `copy /Y`(cmd 내장) 대신 MSBuild `Copy`로 교체 — 게임이 켜져 DLL이
잠겼을 때 빌드를 실패시키지 않고 경고로 끝냅니다.

## tools/AssemblyDump

이번 작업의 실질적인 결과물입니다. 게임 어셈블리에서 **실제 타입·멤버 이름과
시그니처**를 뽑습니다. 디컴파일러도 NuGet도 필요 없고, `System.Reflection.Metadata`로
메타데이터를 디스크에서 직접 읽습니다 (어셈블리를 로드하지 않으므로 코드가 실행되지
않습니다).

```
dotnet run -- --scan "E:\SPT 4.1"                     # 어느 사본이 역난독화됐는지
dotnet run -- "...\Assembly-CSharp.dll" 타입이름들      # 이름 + 시그니처
```

**주의: 역난독화는 설치가 아니라 게임을 한 번 메인 메뉴까지 띄웠을 때 일어납니다**
(SPT 공식 위키 Client Modding Quick Guide, Step 1-5). 안 띄운 상태에서 빌드하면
BSG 원본 이름으로 컴파일되고, SPT가 바꾼 이름은 전부 "찾을 수 없음"이 되며,
**마치 마이그레이션 표가 틀린 것처럼 보입니다.** 이번에 실제로 그 함정에 빠졌습니다.

## 검증

라이드 실측: 패치 25/25 적용, 실패 0, Hollywood 관련 에러·경고 0. 설정으로 꺼둔
3개(탄피 물리, 래그돌 무기 드롭)는 미적용이 정상입니다. 총구·폭발·래그돌·고어 육안 확인.

`LootItem._currentPhysicsTime` 하나만 미검증입니다 — 래그돌 무기 드롭을 켜야 그
패치가 돌아갑니다. 난독화기 이름이 아니라 실제 이름이라 위험은 낮고, 틀려도 이제는
그 패치 하나만 실패하고 로그에 이름이 나옵니다.

## 남은 위험

**없다시피 합니다.** 난독화기 스타일 이름이 코드에 0개고 전부 진짜 이름이라, 다음
EFT 업데이트에서 뭔가 바뀌면 **라이드가 아니라 빌드에서** 터집니다. 4.0에서는 같은
변화가 조용히 지나가고 게임 안에서만 이상하게 동작했습니다.
