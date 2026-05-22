# Core Board & Input CLI

## 목적

포코팡 스타일의 Line-Linker 퍼즐에서 첫 번째 핵심 기능인 `퍼즐 보드 및 입력 시스템`을 Unity 메뉴가 아니라 CLI에서도 반복 생성하고 검증할 수 있게 만든다.

이 CLI는 포트폴리오에서 다음 메시지를 보여주기 위한 증거다.

- Unity 2D 퍼즐 보드를 코드로 생성할 수 있다.
- 보드는 기본적으로 홀수행 오프셋을 가진 육각형 격자를 사용한다.
- 마우스와 터치 입력은 Unity Input System API로 처리한다.
- 생성/검증 결과를 `md/cli-reports/`에 남겨 작업 증거로 쓸 수 있다.

## 명령

기본 육각형 보드 생성:

```cmd
tools\poko-cli.cmd create-core-board
```

명시적으로 육각형 보드 생성:

```cmd
tools\poko-cli.cmd create-core-board --layout hex --width 7 --height 7 --tileTypes 5 --spacing 0.95
```

생성된 씬 검증:

```cmd
tools\poko-cli.cmd validate-core-board
```

기획자 에이전트 분석:

```cmd
tools\poko-cli.cmd analyze-board --layout hex --width 7 --height 7 --tileTypes 5
```

기획자 에이전트 제안을 다음 레벨 데이터로 생성:

```cmd
tools\poko-cli.cmd generate-level --levelId level_001 --layout hex --width 7 --height 7 --tileTypes 5
```

생성된 레벨 데이터를 플레이 씬에 적용:

```cmd
tools\poko-cli.cmd apply-level --levelId level_001
```

플레이 로그를 기획자 에이전트가 분석:

```cmd
tools\poko-cli.cmd analyze-playlog
```

플레이 결과를 다음 레벨 데이터로 재튜닝:

```cmd
tools\poko-cli.cmd retune-level --levelId level_002
```

Optional LLM designer review from the latest playtest analysis JSON:

```cmd
tools\poko-cli.cmd llm-design-review
```

Without `OPENAI_API_KEY`, this command still saves a request packet and pending report under `md/llm-reports/`.

Level experiment planning from the latest playtest log:

```cmd
tools\poko-cli.cmd plan-level-experiments --experimentId exp_001
```

This command creates control, readability, and combo candidate `PokoLevelConfig` assets for the next comparison pass.

Compare played experiment candidates:

```cmd
tools\poko-cli.cmd compare-level-experiments --experimentId exp_001
```

Promote the winning candidate:

```cmd
tools\poko-cli.cmd promote-experiment-winner --experimentId exp_001 --applyScene true
```

Inspect the current designer loop stage:

```cmd
tools\poko-cli.cmd designer-loop-status --experimentId exp_001
```

리포트 경로 지정:

```cmd
tools\poko-cli.cmd create-core-board --reportPath md/cli-reports/core-board-input.md
tools\poko-cli.cmd validate-core-board --reportPath md/cli-reports/core-board-validation.md
```

특정 씬 경로 지정:

```cmd
tools\poko-cli.cmd create-core-board --scenePath Assets/Scenes/PokoPrototype.unity
tools\poko-cli.cmd validate-core-board --scenePath Assets/Scenes/PokoPrototype.unity
```

## 내부 동작

`tools/poko-cli.cmd`는 Unity를 batchmode로 실행하고 아래 에디터 메서드를 호출한다.

- `PokoPuzzle.Editor.PokoPuzzleCli.CreateCoreBoardScene`
- `PokoPuzzle.Editor.PokoPuzzleCli.ValidateCoreBoardScene`

생성 기능은 `PokoPrototypeSceneBuilder`를 사용해 카메라, 보드, 연결선, 점수 텍스트, AI 기획자 텍스트를 포함한 프로토타입 씬을 만든다.

검증 기능은 생성된 씬을 열고 아래 항목을 확인한다.

- `LineLinkerBoard` 컴포넌트 존재.
- 보드 크기가 최소 3x3 이상.
- 타일 종류 수가 3~6 범위.
- 타일 간격이 양수.
- 기본 레이아웃이 `hex`이면 6방향 육각 인접 판정 통과.
- 카메라, 연결선, 점수 텍스트, AI 기획자 텍스트 참조 연결.

## 포코팡 스타일 기준

- 보드는 기본 7x7에서 시작한다.
- 육각형 격자는 홀수행을 오른쪽으로 반 칸 밀어 배치한다.
- 짝수행과 홀수행은 서로 다른 6방향 이웃 오프셋을 사용한다.
- 같은 종류의 인접 타일만 드래그로 연결한다.
- 이미 연결한 직전 타일로 되돌아가면 마지막 선택을 취소한다.
- 3개 이상 연결한 뒤 손을 떼면 제거, 점수, 낙하, 리필이 발생한다.
- 1~2개만 연결하고 손을 떼면 보드는 유지되고 선택만 해제된다.
- 원본 포코팡의 보호받는 아트, 이름, 캐릭터, 사운드, 정확한 UI는 복사하지 않는다.

## 생성되는 리포트

CLI 실행 후 아래 파일이 생성될 수 있다.

- `md/cli-reports/core-board-input.md`
- `md/cli-reports/core-board-validation.md`
- `md/agent-reports/latest-board-analysis.md`
- `md/agent-reports/latest-board-analysis.json`
- `Assets/PokoPuzzle/Data/Generated/level_001.asset`
- `md/level-reports/level_001.md`
- `md/level-reports/level_001.json`
- `md/level-reports/level_001-applied.md`
- `md/playtest-logs/latest-playtest.jsonl`
- `md/agent-reports/latest-playtest-analysis.md`
- `md/agent-reports/latest-playtest-analysis.json`
- `Assets/PokoPuzzle/Data/Generated/level_002.asset`
- `md/level-reports/level_002-retune.md`
- `md/level-reports/level_002-retune.json`
- `md/llm-reports/latest-designer-request.json`
- `md/llm-reports/latest-designer-review.md`
- `md/llm-reports/latest-designer-response.json`
- `Assets/PokoPuzzle/Data/Generated/Experiments/exp_001_control.asset`
- `Assets/PokoPuzzle/Data/Generated/Experiments/exp_001_readability.asset`
- `Assets/PokoPuzzle/Data/Generated/Experiments/exp_001_combo.asset`
- `md/experiment-reports/exp_001.md`
- `md/experiment-reports/exp_001.json`
- `md/playtest-logs/by-level/exp_001_control-latest.jsonl`
- `md/playtest-logs/by-level/exp_001_readability-latest.jsonl`
- `md/playtest-logs/by-level/exp_001_combo-latest.jsonl`
- `md/experiment-reports/exp_001-comparison.md`
- `md/experiment-reports/exp_001-comparison.json`
- `Assets/PokoPuzzle/Data/Generated/Promoted/exp_001_winner.asset`
- `md/experiment-reports/exp_001-promotion.md`
- `md/portfolio-milestones/exp_001-designer-loop.md`
- `md/designer-loop/latest-status.md`
- `md/designer-loop/latest-status.json`

이 리포트는 "CLI로 Unity 퍼즐 프로토타입을 반복 생성/검증했다"는 포트폴리오 증거로 사용한다.

## 주의사항

Unity batchmode는 같은 프로젝트를 이미 열고 있는 Unity 에디터가 있으면 프로젝트 잠금 때문에 실패할 수 있다. 실제 CLI 실행 전에는 Unity 에디터를 닫는 것을 권장한다.

기본 Unity 경로는 `tools/poko-cli.cmd`에 Unity 6000.3.11f1 기준으로 들어 있다.

다른 Unity 버전을 사용할 경우 실행 전에 환경 변수를 지정한다.

```cmd
set UNITY_EXE=C:\Program Files\Unity\Hub\Editor\6000.3.11f1\Editor\Unity.exe
tools\poko-cli.cmd create-core-board
```
