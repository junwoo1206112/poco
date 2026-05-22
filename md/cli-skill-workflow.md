# 포코팡 스타일 퍼즐 개발을 위한 CLI/스킬 사용 전략

## 목적

이 문서는 포코팡 스타일 Line-Linker 퍼즐을 만들 때 Codex CLI와 스킬을 어떻게 나눠 쓰는지 정리한다. 목표는 단순히 코드를 빨리 작성하는 것이 아니라, 트리노드 AX 지원 포트폴리오에서 "AI/CLI를 활용해 기획, 구현, 튜닝 루프를 설계했다"는 증거를 남기는 것이다.

## 추천 결론

CLI는 실제 Unity 프로젝트를 읽고 고치는 실행 도구로 사용한다. 스킬은 Codex가 프로젝트의 방향을 잊지 않도록 잡아주는 작업 규칙으로 사용한다. AI 기획자 에이전트는 게임 내부 기능이면서 동시에 포트폴리오 메시지의 중심이므로, 플레이 로그와 튜닝 리포트를 `md/`에 계속 남긴다.

## CLI를 쓰기 좋은 작업

- 코드베이스 탐색: `rg`, `rg --files`, `Get-Content`로 현재 구조와 오류 위치 확인.
- Unity 오류 진단: `Editor.log`에서 예외명, 파일, 라인 번호 확인.
- 게임플레이 구현: 보드 생성, 드래그 입력, 연결 검증, 제거/낙하/리필, 점수 계산.
- 데이터 구조 정리: 레벨 설정, 타일 스폰 가중치, 목표 점수, 이동 횟수.
- 기획자 에이전트 실행: 보드 텔레메트리를 분석하고 Markdown/JSON 튜닝 리포트 생성.
- 반복 수정: 작은 단위로 수정하고 Unity에서 바로 Play 테스트.

## 포코팡 기능별 CLI 매핑

| 기능 | CLI | 결과물 |
| --- | --- | --- |
| 퍼즐 보드 및 입력 | `tools\poko-cli.cmd create-core-board --layout hex` | 육각형 Line-Linker 프로토타입 씬 |
| 보드 검증 | `tools\poko-cli.cmd validate-core-board` | `md/cli-reports/core-board-validation.md` |
| 기획자 에이전트 분석 | `tools\poko-cli.cmd analyze-board --layout hex --width 7 --height 7 --tileTypes 5` | `md/agent-reports/latest-board-analysis.md`, `md/agent-reports/latest-board-analysis.json` |
| 다음 레벨 생성 | `tools\poko-cli.cmd generate-level --levelId level_001 --layout hex` | `Assets/PokoPuzzle/Data/Generated/level_001.asset`, `md/level-reports/level_001.md` |
| 레벨 씬 적용 | `tools\poko-cli.cmd apply-level --levelId level_001` | `Assets/Scenes/PokoPrototype.unity`, `md/level-reports/level_001-applied.md` |
| 플레이 로그 분석 | `tools\poko-cli.cmd analyze-playlog` | `md/agent-reports/latest-playtest-analysis.md`, `md/agent-reports/latest-playtest-analysis.json` |
| 플레이 기반 재튜닝 | `tools\poko-cli.cmd retune-level --levelId level_002` | `Assets/PokoPuzzle/Data/Generated/level_002.asset`, `md/level-reports/level_002-retune.md` |

이렇게 나누면 기능 구현 CLI와 기획 판단 CLI가 분리된다. 즉, `create-core-board`는 플레이 가능한 보드를 만들고, `analyze-board`는 그 보드 규칙을 기준으로 기획자가 볼 만한 난이도와 다음 튜닝 액션을 남긴다.
`generate-level`은 그 제안을 Unity가 읽을 수 있는 레벨 데이터로 바꾸고, `apply-level`은 그 데이터를 실제 플레이 씬에 연결한다. 이후 `analyze-playlog`가 실제 플레이 결과를 읽고, `retune-level`이 그 결과를 다음 레벨 에셋으로 되돌린다.

## 스킬을 쓰기 좋은 작업

### `poko-game-designer`

이 프로젝트의 기본 스킬이다. Line-Linker 퍼즐의 범위, AI 기획자 에이전트 계약, 포트폴리오 기준을 유지하기 위해 사용한다.

사용 이유:

- 작업이 넓어져서 일반 match-3나 다른 장르로 새는 것을 막는다.
- 포코팡 원본 아트/캐릭터/UI를 복사하지 않고, 메커닉과 감각만 참고하도록 기준을 세운다.
- AI 기획자 에이전트가 반드시 입력, 출력, 저장 증거를 남기도록 강제한다.

### `skill-creator`

프로젝트 전용 스킬을 새로 만들거나 갱신할 때만 사용한다.

사용 이유:

- 스킬 파일의 형식과 설명을 Codex가 다시 사용할 수 있게 유지한다.
- 장기적으로 "AI 기획자", "레벨 튜너", "포트폴리오 리포터" 같은 세부 스킬로 나눌 수 있다.

### `openai-docs`

OpenAI API, Agents SDK, 모델 선택, 프롬프트 업그레이드처럼 최신 공식 문서가 필요한 단계에서 사용한다.

사용 이유:

- AI 에이전트를 실제 LLM 기반으로 확장할 때 API 사용법이 바뀔 수 있다.
- 포트폴리오에 "최신 OpenAI 공식 문서를 기준으로 구현했다"는 설명을 붙일 수 있다.

### `imagegen`

포트폴리오용 독자 타일, 캐릭터, 배경 콘셉트 이미지가 필요할 때 사용한다.

사용 이유:

- 포코팡의 보호받는 아트, 이름, 캐릭터, UI를 복사하지 않기 위해서다.
- 원본을 피하면서도 캐주얼 퍼즐다운 밝고 읽기 쉬운 시각 방향을 빠르게 탐색할 수 있다.

### `browser:browser`

Unity WebGL 빌드, 로컬 웹 대시보드, 포트폴리오 페이지를 실제 화면에서 검증할 때 사용한다.

사용 이유:

- 화면이 비어 있거나 UI가 겹치는 문제는 코드만 봐서는 놓치기 쉽다.
- 최종 포트폴리오 페이지나 WebGL 데모를 검수할 때 필요하다.

## 추천 개발 루프

1. `poko-game-designer` 기준으로 이번 작업 목표를 좁힌다.
2. CLI로 현재 코드, 문서, 로그를 확인한다.
3. 작은 게임플레이 단위를 구현한다.
4. Unity에서 Play 테스트하고 콘솔 오류를 확인한다.
5. 오류가 있으면 CLI로 `Editor.log`와 관련 파일을 다시 읽고 고친다.
6. `tools\poko-cli.cmd analyze-board`로 AI 기획자 에이전트 리포트를 생성한다.
7. `tools\poko-cli.cmd generate-level`로 다음 레벨 설정 에셋과 리포트를 생성한다.
8. `tools\poko-cli.cmd apply-level`로 생성된 레벨 설정을 플레이 씬에 적용한다.
9. Unity에서 플레이해서 `md/playtest-logs/latest-playtest.jsonl`을 남긴다.
10. `tools\poko-cli.cmd analyze-playlog`로 실제 플레이 기반 기획자 리포트를 생성한다.
11. `tools\poko-cli.cmd retune-level`로 플레이 결과 기반 다음 레벨 에셋을 생성한다.
12. Markdown 리포트는 사람이 읽고, JSON/ScriptableObject는 다음 레벨 설정에 사용한다.
13. `md/`에 무엇을 왜 바꿨는지 짧게 기록한다.

## 포트폴리오에서 보여줄 문장

이 프로젝트는 Codex CLI를 코드 탐색, Unity 오류 진단, 반복 구현에 사용하고, 프로젝트 전용 스킬을 통해 포코팡 스타일 Line-Linker 퍼즐의 기획 범위와 AI 게임 기획자 에이전트의 출력 계약을 유지했습니다.

## 현재 적용한 내용

- `.codex/skills/poko-game-designer/SKILL.md`에 CLI 사용 기준과 함께 쓸 스킬 목록을 추가했다.
- 이 문서에 각 도구를 왜 쓰는지 정리했다.
- README에서 이 문서를 참조하도록 연결했다.
- `analyze-board` CLI로 기획자 에이전트 분석 리포트와 JSON 튜닝 제안을 저장할 수 있게 했다.
- `generate-level` CLI로 기획자 에이전트 제안을 Unity `PokoLevelConfig` 에셋으로 변환할 수 있게 했다.
- `apply-level` CLI로 생성된 `PokoLevelConfig`를 프로토타입 씬의 `LineLinkerBoard`에 연결할 수 있게 했다.
- 런타임 플레이 로그와 `analyze-playlog` CLI로 실제 플레이 결과를 기획자 에이전트가 다시 분석할 수 있게 했다.
- `retune-level` CLI로 플레이 결과를 다음 `PokoLevelConfig` 에셋으로 다시 변환할 수 있게 했다.
