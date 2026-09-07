<26/08/30 상세 변경점>

- 빌드 경로가 원작자 로컬 폴더 구조(`..\..\..\Client_Dev\...` 상대경로)로 하드코딩
  되어 있어서 다른 환경에서 어셈블리를 못 찾던 문제 — SptRoot 속성으로 오버라이드
  가능하게 수정, 실제 로컬 SPT 설치 경로를 기본값으로 지정 (HollywoodGraphics와
  동일한 문제, 동일한 방식으로 수정)

- 결과: 정상적으로 빌드 가능

---

<26/09/07 상세 변경점 — SPT 4.1.5 대응>

**주의: 이 변경분은 컴파일 검증이 안 된 상태입니다.** 이 플러그인은 게임 어셈블리
(`Assembly-CSharp.dll` 등)를 참조해 빌드되는데 그건 실제 설치본에만 있습니다.
아래는 확실한 것(경로·안전장치)과 빌드해봐야 아는 것(게임 API 변화)이 섞여 있습니다.

- **빌드 경로를 `E:\SPT 4.1`로 변경.** 다른 경로면 `-p:SptRoot=<설치 루트>`

- **게임/BepInEx 위치를 가정하지 않고 탐색.** 4.1이 서버를 `SPT_Runtime\` 밑으로
  옮겼습니다. 게임과 BepInEx는 보통 루트에 그대로 남습니다(EscapeFromTarkov.exe 옆에
  BepInEx가 있어야 로더가 붙으니까) — 다만 "보통"은 "항상"이 아니고, 여기서 잘못
  찍으면 미해결 참조 20개가 뜹니다.

  그래서 루트 → `SPT_Runtime\` 순으로 찾고, 게임과 BepInEx를 **각각 따로** 찾습니다
  (둘이 같이 움직인다는 보장이 없어서). 루트 배치 / `SPT_Runtime` 배치 / 둘이 섞인
  배치 세 가지로 확인했습니다. `-p:GameRoot=` `-p:BepInExRoot=`로 직접 지정 가능

- 참조가 하나라도 안 잡히면 **어느 폴더에 뭐가 없는지 한 줄로** 말하고 멈춥니다.
  참조 20개가 전부 빨개지는 것보다 낫습니다

- 빌드 후 설치 복사를 `copy /Y`(cmd 내장) 대신 MSBuild `Copy`로 교체. 게임이 켜진
  채로 빌드하면 DLL이 잠기는데, 그걸로 빌드를 실패시킬 이유는 없어서 경고로 끝냅니다
  (`-p:AutoInstall=false`로 끌 수 있음)

- **번호가 붙은 패치 대상을 로그로 드러냄** — `TextureDecalsPainter.method_5`,
  `RagdollClass.method_1`. 이 이름들은 난독화기가 세어서 붙인 거라 BSG가 클래스에
  메서드를 넣고 빼면 번호가 밀립니다. 이게 세 가지로 깨지는데 하나만 시끄럽습니다:

  1. 번호가 아예 사라짐 → `GetMethod`가 null → 예외 (시끄러움)
  2. 번호가 다른 시그니처를 가리킴 → Harmony가 prefix 바인딩 거부 → 예외 (시끄러움)
  3. **번호가 모양이 같은 다른 메서드를 가리킴 → 아무 일도 안 일어나고 엉뚱한 코드에
     패치가 붙음** (조용함)

  3번은 어셈블리를 봐야 막을 수 있어서 여기서 못 막습니다. 대신 안 보이지는 않게
  했습니다 — 실제로 잡힌 메서드의 시그니처를 로그에 찍습니다. 업데이트 후에 "이게
  맞는 메서드에 붙었나"를 이분 탐색 대신 로그로 답할 수 있습니다

- **총구 이펙트가 매 발 예외를 던지지 않도록 방어** — `MuzzleManager`의
  `muzzleJet_0` / `muzzleFume_0` / `muzzleSmoke_0`을 문자열로 리플렉션해서 쓰는데,
  `_0` 접미사는 난독화기가 붙인 거라 게임 빌드에 종속된 이름입니다. null 체크가 없어서
  이름이 바뀌면 **쏠 때마다** NRE가 났습니다. 이제 한 번 로그 남기고 총구 이펙트만
  꺼집니다

- 나머지 패치 대상 20여 개는 전부 실제 이름 + `nameof(...)`이라 이름이 없어지면
  **컴파일 에러**로 잡힙니다. 즉 빌드가 통과하면 그쪽은 문제없습니다

**(같은 날 추가 — 공식 위키 확인 후)**

SPT 공식 위키의 [Client Mod Migration 4.0 to 4.1] 문서를 보고 위 내용을 대폭 보강했습니다.
위키가 알려준 핵심은 하나입니다:

> **4.1은 클라이언트를 역난독화했습니다.** 4.0에서 `GClass680` / `GStruct80` 같던 타입들이
> 진짜 이름과 진짜 네임스페이스를 갖게 됐고, 4.0 시절의 부분 별칭(`~Class` 접미사)들도
> 전부 바뀌었습니다. **4.0 클라이언트 모드는 전부 4.1로 재빌드해야 합니다.**

위키에 5,957줄짜리 4.0→4.1 이름 매핑 표가 있어서, 이 모드가 참조하는 모든 식별자를
그 표에 대조했습니다(중첩 타입은 표에서 `Outer+Inner` 형태라 마지막 세그먼트 기준으로도
한 번 더 훑음). **바뀐 이름 20개, 치환 38곳** — 전부 표 근거입니다:

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

타입이 네임스페이스 안으로 들어가서 `using`도 같이 추가했습니다
(`EFT.InventoryLogic`, `EFT.CameraControl`, `EFT.Ballistics`).

**그리고 이게 컴파일러가 절대 못 잡는 문제를 하나 드러냈습니다.** 이 모드는 private 필드를
문자열로 리플렉션해서 쓰는데, 그중 셋은 **필드 이름 자체가 타입 이름에서 나온 것**이었습니다:

- `BallisticsCalculator.gdelegate64_0` — 타입이 `ShotDelegate`가 됐으니 필드도 바뀌었을 것
- `Effects.lightAllocationPoolClass` — 타입이 `LightPool`이 됐으니 마찬가지
- `DeferredDecalRenderer.dictionary_0` / `dictionary_2` — 난독화기가 센 번호라 밀림

이름이 문자열이라 **빌드는 통과하고 라이드에서 터집니다.** 그런데 역난독화가 해결책도
같이 줬습니다 — **필드의 타입은 안 움직이고, 이 클래스들에서는 타입만으로 필드가 특정됩니다.**
`ObfuscatedField`가 이름으로 먼저 찾고(아직 맞으면 공짜로 정확함), 안 되면 그 타입인 필드를
찾고, 둘 다 실패할 때만 이름을 대며 에러를 남깁니다. 못 찾으면 해당 기능만 꺼지고
총알마다 예외를 던지지는 않습니다.

`gdelegate64_0`은 특히 중요한데, 임팩트·고어·트레이서 이펙트가 전부 이 델리게이트에
걸려 있어서 이게 실패하면 **모드가 조용히 아무것도 안 하게** 됩니다. 그래서 실패 시
"shot effects are off"라고 명시적으로 남깁니다.

**여전히 남은 위험** (어셈블리 없이는 못 잡음):
- `DecalPainter`의 `_renderer.method_6(...)` — 직접 호출이라 없어지면 컴파일 에러지만,
  같은 시그니처의 다른 메서드로 번호가 밀리면 조용히 엉뚱한 걸 부릅니다
- `TextureDecalsPainter.method_5`, `CorpseRagdoll.method_1` — 위의 `PatchTarget` 로그로 확인

**(같은 날 재차 추가 — SPT `assembly-tool` 소스 확인 후)**

역난독화를 실제로 수행하는 도구(`SP-Tushonka/assembly-tool`) 소스를 읽고, 위에서
"~일 것이다"로 적었던 것들을 **사실로 교정**했습니다.

도구의 `ObfuscatedFieldRenamer`는 난독화된 필드를 **그 필드의 타입 이름에서** 새 이름을
만듭니다. 단, 해당되는 건 이름이 난독화기 접두사로 시작하는 필드뿐입니다
(`DataProvider.ObfuscatedPrefixes`: Class, GClass, Struct, GStruct, Interface, GInterface,
Delegate, GDelegate, Exception, GException, GControl, GAttribute, method, smethod, vmethod
+ 뒤에 숫자).

이 기준으로 우리 필드 4종의 운명이 **갈립니다. 이름만 보고 짐작한 것과 다릅니다:**

| 필드 | 접두사 해당? | 결론 |
| --- | --- | --- |
| `BallisticsCalculator.gdelegate64_0` | ✅ `GDelegate` | 타입이 `ShotDelegate`가 됐으므로 **이름 사라짐 (확정)** |
| `Effects.lightAllocationPoolClass` | ❌ | 타입은 `LightPool`이 됐지만 **필드 이름은 그대로** |
| `DeferredDecalRenderer.dictionary_0` / `_2` | ❌ | **그대로** |
| `MuzzleManager.muzzleJet_0` 외 2 | ❌ | **그대로** |

즉 제가 위에서 "셋 다 바뀌었을 것"이라고 쓴 건 틀렸고, **실제로 확실히 깨지는 건
`gdelegate64_0` 하나**입니다. 그런데 그 하나가 하필 임팩트·고어·트레이서가 전부
매달려 있는 델리게이트라, 이 모드가 조용히 아무것도 안 하게 만드는 그 필드입니다.

`ObfuscatedField`는 어느 쪽이든 맞게 동작합니다 — 이름이 아직 유효하면 이름을 쓰고,
아니면 타입으로 찾습니다. 그래서 총구 필드 3개도 같은 경로로 옮겼습니다(이름이
살아있으면 공짜로 정확하고, 언젠가 바뀌면 타입이 받아줍니다).

**메서드는 자동 개명이 없습니다.** 도구에 `ObfuscatedFieldRenamer`는 있어도 그에
대응하는 자동 메서드 개명기는 없고, 명시적 `MethodRenames` 목록으로만 바뀝니다.
`TextureDecalsPainter`와 `CorpseRagdoll`에는 그 목록이 아예 없습니다 — 즉
`method_5` / `method_1`은 **SPT가 건드리지 않습니다.** 남은 변수는 BSG 쪽 번호가
밀렸는지 뿐이고, 그건 `PatchTarget` 로그로 확인합니다.

**(같은 날 정정 — 빌드 에러의 진짜 원인)**

위의 개명 작업은 **맞았습니다.** 빌드가 깨진 건 다른 이유였습니다 — **참조하던
`Assembly-CSharp.dll`이 SPT가 손대지 않은 BSG 원본**이었습니다.

`tools/AssemblyDump`로 실제 어셈블리를 덤프해서 확인한 결과 (타입 15,137개):

| 찾은 것 | 개수 |
| --- | --- |
| SPT 4.1 역난독화 이름 (`Ammo`, `CorpseRagdoll`, `ShotDelegate`…) | **0** |
| SPT 4.0 별칭 (`AmmoItemClass`, `RagdollClass`…) | **0** |
| de4dot 이름 (`GClass####`) | **0** |

SPT의 이름이 4.0 것도 4.1 것도 하나도 없고, 멤버 이름 상당수가 **출력 불가능한
유니코드**(덤프에 빈칸으로 나옴)였습니다. 원본 난독화 어셈블리라는 뜻입니다.

- **원인은 제가 짠 탐색 순서**입니다. 루트를 먼저 보게 했는데, 4.1은 게임이
  `SPT_Runtime\` 아래 있습니다. 설치본에 손대지 않은 `EscapeFromTarkov_Data`가
  루트에도 남아 있으면 **그쪽이 이깁니다.**

  그리고 이건 시끄럽게 실패하지 않습니다. BSG 원본 이름으로 컴파일되니 SPT가 바꾼
  이름은 전부 "찾을 수 없음"이 되고, **마치 마이그레이션 표가 틀린 것처럼 보입니다.**
  `TarkovApplication.method_41`과 `LampController.Awake`가 없다고 나온 게 그거였습니다
  (원본에선 `method_41`이라는 이름 자체가 없고, `Awake`는 private입니다)

- 수정: `SPT_Runtime\` → `SPT\` → 루트 순으로 탐색. 네 가지 배치로 검증했고, 둘 다
  있을 때 `SPT_Runtime`을 고르는 것까지 확인했습니다. 빌드할 때 어느 걸 골랐는지
  한 줄 찍습니다

- `AssemblyDump`에 스캔 모드 추가 — 설치본 아래 `Assembly-CSharp.dll`을 전부 찾아서
  각각이 역난독화된 것인지 판정합니다. 이름 목록 없이도 구분됩니다: 원본은 메서드
  이름 상당수가 출력 불가능한 문자거든요
