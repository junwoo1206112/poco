# Poko Engine CLI Puzzle Framework

**트리노드 AX 인턴 (프로덕트 엔지니어) 포트폴리오 — 포코팡 스타일 Line-Linker 퍼즐 + AI 게임 기획자 에이전트**

## Portfolio Summary

| 영역 | 내용 |
|------|------|
| **Core** | Hex grid Line-Linker 퍼즐 — 드래그 체인, 콤보, 피버, 폭탄, 적 전투, 스페셜 블록 |
| **AI Agent** | `IGameDesignerAgent` 인터페이스 기반 Heuristic + LLM 이중 전략 |
| **Pipeline** | CLI 12개 명령어로 레벨 생성 → 플레이 → 분석 → 튜닝 → 실험 → 승격까지 자동화 |
| **Tests** | 39개 유닛 테스트 (HexGrid, BoardBomb, BoardEnemy, Agent, TileType) |
| **DI** | ScriptableObject + Resources.Load 기반 데이터 제공자 (Enemy, Skill, BalanceProfile) |
| **Visual** | 100% procedural texture — 외부 이미지 에셋 불필요 |

## 프로토타입 실행

Unity에서 `Tools > Poko Puzzle > Create Prototype Scene` 실행 후 Play.

## CLI 명령어

```
tools\poko-cli.cmd <command> [--key value ...]
```

| 명령어 | 역할 |
|--------|------|
| `create-core-board` | 프로토타입 씬 생성 |
| `validate-core-board` | 보드 무결성 검증 |
| `analyze-board` | 보드 분석 (AI 난이도 평가) |
| `generate-level` | 레벨 Config 에셋 생성 |
| `apply-level` | 레벨 Config을 씬에 적용 |
| `analyze-playlog` | 플레이 로그 분석 (난이도 진단) |
| `retune-level` | 분석 결과로 레벨 재튜닝 |
| `plan-level-experiments` | 3개 실험 배리언트 생성 |
| `compare-level-experiments` | 실험 결과 비교 및 추천 |
| `promote-experiment-winner` | 추천 배리언트 승격 |
| `llm-design-review` | OpenAI GPT-4o-mini로 기획 리뷰 |
| `compare-agent-strategies` | Heuristic vs LLM 에이전트 비교 리포트 |
| `designer-loop-status` | 현재 디자이너 루프 상태 확인 |
| `convert-excel-data` | Excel → ScriptableObject 변환 |

## AI Game Designer Agent

두 가지 전략을 `IGameDesignerAgent` 인터페이스로 추상화:

- **HeuristicGameDesignerAgent**: 결정론적 규칙 7개 분기 (Rainbow, Fever, Combo, Easy, Hard, Normal 등)
- **LLMGameDesignerAgent**: GPT-4o-mini에 보드 텔레메트리를 전송, JSON 구조화된 튜닝 제안 수신
- API 키 미설정 시 자동으로 Heuristic 폴백
- `compare-agent-strategies` 명령어로 동일 입력에 대한 두 전략 비교 리포트 생성

## 포트폴리오 증거

- `md/agent-reports/` — AI 에이전트 분석 결과
- `md/experiment-reports/` — 실험 계획 및 비교
- `md/portfolio-milestones/` — 마일스톤별 성과
- `md/llm-reports/` — LLM 기획 리뷰 (API 키 설정 시)
- `openspec/changes/` — OpenSpec 변경 관리

## 기술 스택

Unity 6000.3, URP 2D, Unity Input System 1.19, Unity Test Framework 1.6.0, C# 9.0 (netstandard2.1)
