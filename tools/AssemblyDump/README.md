# AssemblyDump

게임 어셈블리에서 **실제 타입/멤버 이름**을 뽑아냅니다. 디컴파일러도, NuGet 패키지도,
어셈블리 로드도 필요 없습니다 — `System.Reflection.Metadata`(SDK 내장)로 메타데이터
테이블을 디스크에서 직접 읽습니다. 읽는 어셈블리의 코드는 **한 줄도 실행하지 않습니다.**

## 왜 필요한가

SPT 4.1이 클라이언트를 역난독화하면서 타입 이름이 대거 바뀌었습니다. 공식 위키의
매핑 표와 `assembly-tool`의 매핑 데이터가 있지만, **둘 다 특정 시점 기준**이라
설치본과 어긋날 수 있습니다. 이 도구는 설치본 자체에 물어봅니다.

## 사용법

```
cd tools/AssemblyDump

# 전체 타입 목록만
dotnet run -- "E:\SPT 4.1\EscapeFromTarkov_Data\Managed\Assembly-CSharp.dll"

# 특정 타입들의 멤버까지
dotnet run -- "E:\SPT 4.1\EscapeFromTarkov_Data\Managed\Assembly-CSharp.dll" TarkovApplication LampController
```

- `types.txt` — 어셈블리의 모든 타입 전체 이름 (중첩 타입은 `Outer+Inner`)
- `members.txt` — 이름에 인자가 포함된 타입들의 필드/속성/메서드

둘 다 `.gitignore` 되어 있습니다.

## 예

바뀐 이름 찾기:

```
findstr /i "Ragdoll" types.txt
findstr /i "InventoryLogic.Ammo" types.txt
```

패치 대상 메서드가 아직 있는지 확인:

```
dotnet run -- "...\Assembly-CSharp.dll" TarkovApplication
findstr "method_4" members.txt
```
