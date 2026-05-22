# Poko Engine CLI Puzzle Framework

LLM designer review CLI notes: `md/llm-designer-review-cli.md`
Level experiment planning CLI notes: `md/level-experiment-planning-cli.md`
Promoted experiment winners write portfolio milestone notes under `md/portfolio-milestones/`.
Designer loop status CLI notes: `md/designer-loop-status-cli.md`

포코팡에서 영감을 받은 Line-Linker 퍼즐과 AI 게임 기획자 에이전트를 Unity 2D로 구현하는 포트폴리오용 프로토타입입니다.

## 현재 프로토타입

- 런타임에서 생성되는 7x7 Line-Linker 보드
- 같은 색 타일을 드래그해 체인 연결
- 3개 이상 연결 후 손을 떼면 제거, 점수 획득, 낙하, 리필 처리
- 로컬 휴리스틱 기반 `Game Designer Agent`가 보드 난이도를 분석하고 다음 레벨 튜닝 방향 제안
- 프로젝트 전용 Codex 스킬: `.codex/skills/poko-game-designer/SKILL.md`

## 프로토타입 씬 생성 방법

Unity에서 프로젝트를 연 뒤 다음 메뉴를 실행합니다.

`Tools > Poko Puzzle > Create Prototype Scene`

아래 씬이 생성되고 저장됩니다.

`Assets/Scenes/PokoPrototype.unity`

그 다음 Play를 누르고 같은 색 원형 타일을 드래그하면 됩니다.

## 포트폴리오 방향

이 프로젝트는 Unity 클라이언트 개발 역량과 AI 기반 게임 제작 역량을 함께 보여주기 위한 작업입니다.

- 플레이 가능한 캐주얼 퍼즐 코어
- 데이터 기반 레벨 튜닝
- AI 기획자 분석
- `md/` 폴더에 남는 읽기 쉬운 포트폴리오 증거

트리노드 AX 지원용 전체 계획은 `md/trinode-ax-portfolio-plan.md`를 참고합니다.
CLI/스킬 사용 전략은 `md/cli-skill-workflow.md`를 참고합니다.
OpenSpec 적용 기록은 `md/openspec-adoption.md`를 참고합니다.
Core Board & Input CLI 사용법은 `md/core-board-input-cli.md`를 참고합니다.
