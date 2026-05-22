# AGENTS.md - Poko Engine CLI Puzzle Framework

Unity 6000.3 2D 포트폴리오 프로토타입 — 포코팡에서 영감을 받은 Line-Linker 퍼즐과 AI 게임 기획자 에이전트.
URP 2D, Unity Input System 1.19, Unity Test Framework 1.6.0.

## 빌드/린트/테스트 명령어

CLI 빌드/린트 파이프라인이 없습니다. 모든 작업은 Unity 에디터 내에서 실행합니다.

- **프로젝트 열기**: Unity Hub → 이 폴더
- **프로토타입 씬 생성**: `Tools > Poko Puzzle > Create Prototype Scene` 또는 아래 CLI
- **플레이**: Unity 에디터 Play 버튼
- **VS Code 디버그**: "Attach to Unity" 실행 구성 (vstuc 확장)

### 테스트 (Unity Test Framework)

- **Test Runner 열기**: `Window > General > Test Runner`
- **전체 테스트 실행**: Test Runner → "Run All"
- **단일 테스트 실행**: Test Runner → 테스트 선택 → "Run Selected"
- **CLI 테스트 러너 없음** — 테스트는 Unity 런타임 필요
- 테스트는 `Assets/Tests/`에 추가하고 `TestAssemblies`를 참조하는 어셈블리 정의 필요

### CLI (batchmode Unity)

```
tools\poko-cli.cmd <command> [--key value ...]
set UNITY_EXE=C:\Program Files\Unity\Hub\Editor\6000.3.11f1\Editor\Unity.exe
```

명령어: `create-core-board`, `validate-core-board`, `analyze-board`, `generate-level`, `apply-level`, `analyze-playlog`, `retune-level`, `plan-level-experiments`, `compare-level-experiments`, `promote-experiment-winner`, `designer-loop-status`, `llm-design-review`

### 중요 규칙

- **`Assets/Mirror/` 수정 금지** — Mirror는 읽기 전용 종속성
- **`.meta` 파일 수동 수정 금지** — Unity가 관리
- **주석 추가 금지** — 명시적으로 요청받은 경우만 예외
- **`GameObject.Find()` 사용 금지** — 직렬화 참조, 싱글턴, `FindFirstObjectByType<T>()` 사용
- **네트워크 동기화에 `Update()` 사용 금지** — `[SyncVar]`, `[ClientRpc]`, SyncObject 컬렉션 사용
- **파일당 클래스 하나** — 파일명이 클래스명과 정확히 일치해야 함

## 코드 스타일

### 네임스페이스 (폴더 기준)

| 폴더 | 네임스페이스 |
|--------|-----------|
| `Assets/PokoPuzzle/Scripts/Core/` | `PokoPuzzle.Core` |
| `Assets/PokoPuzzle/Scripts/AI/` | `PokoPuzzle.AI` |
| `Assets/PokoPuzzle/Scripts/Editor/` | `PokoPuzzle.Editor` |

### Using 문

순서: System → UnityEngine → UnityEditor → 로컬 프로젝트 네임스페이스.

```csharp
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using PokoPuzzle.Core;
```

### 중괄호 및 포맷팅

- **Allman 스타일** — 여는 중괄호는 새 줄에
- **4-space 들여쓰기** (탭 사용 금지)
- **메서드 사이에 빈 줄 한 줄**
- **`var`** — 타입이 명확한 모든 지역 변수에 사용
- **`for` 루프** — 인라인 변수 선언: `for (var index = 0; ...)`

### 명명 규칙

| 요소 | 규칙 | 예시 |
|---------|-----------|---------|
| 클래스 | PascalCase, 구체 클래스는 `sealed` | `sealed class LineLinkerBoard` |
| 인터페이스 | IPascalCase | `IGameDesignerAgent` |
| 구조체 | PascalCase, `readonly` | `readonly struct BoardTelemetry` |
| 열거형 | PascalCase (타입 + 값) | `PokoTileType.Red` |
| private 필드 | `camelCase` (밑줄 없음) | `selectedTiles`, `moveLimit` |
| Serialized 필드 | `[SerializeField] private camelCase` | `[SerializeField] private int width` |
| Public 필드 (인스펙터) | `camelCase` | `public int targetScore` |
| 프로퍼티 | PascalCase, 표현식 본문 | `public int Width => width;` |
| 메서드 | PascalCase | `BuildBoard()`, `AreAdjacent()` |
| 매개변수 | camelCase | `int column, int row` |
| 상수 | PascalCase | `VerticalSpacingRatio` |
| 지역 변수 | camelCase | `chainLength`, `gainedScore` |

### 에디터 스크립트

모든 에디터 코드는 `#if UNITY_EDITOR ... #endif`로 감쌉니다. 메뉴 항목에는 `[MenuItem]`을 사용합니다.

### 에러 처리

- **Guard clauses** (early return) — 런타임 코드에서 try-catch보다 선호
- `Fail(string)` 헬퍼 — CLI 메서드용, 에러 로그 후 throw
- `Debug.Log` — 진행 상황, `Debug.LogWarning` — 복구 가능한 문제
- `Debug.LogError` — 실패, 포맷: `$"[ClassName] message"`

### C# 언어 기능

- 문자열 보간: `$"Score {score}/{targetScore}"`
- 표현식 본문 멤버: `public int Width => width;`
- Null 조건부: `spriteRenderer?.color = color;`
- 대상 형식 `new()`: `new()`, `new(column, row, type, sprite)`
- `is` 패턴 매칭: `if (tile is null)`
- `readonly struct` — 데이터 계약용
- `nameof()` — 문자열 참조가 필요한 곳에서 사용
- C# 9.0, 대상 프레임워크: netstandard2.1

### 로깅 포맷

```
Debug.Log($"[ClassName] 이벤트 설명");
Debug.LogWarning($"[ClassName] 복구 가능한 문제");
Debug.LogError($"[ClassName] 실패 상세");
```

### 인스펙터 직렬화 패턴

```csharp
[Header("그룹 이름")]
[SerializeField] private int someField;
[SerializeField] private SomeType someReference;
```

### OpenSpec 워크플로우 (필수)

- **변경 제안**: `openspec/config.yaml` 및 `openspec/changes/` 참고
- **검증**: 완료 전 `openspec.cmd validate <change-name>` 실행
- **증거**: 설계 결정, 보고서, 플레이 로그를 `md/` 아래에 저장
- **포트폴리오**: 각 마일스톤은 다음을 답해야 함 — 리뷰어가 무엇을 플레이할 수 있는가, AI가 무엇을 분석하는가, 어떤 엔지니어링 결정을 보여주는가, 증거는 어디에 저장되었는가
