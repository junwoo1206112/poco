# OpenSpec 적용 기록

## 적용한 도구

- 저장소: [Fission-AI/OpenSpec](https://github.com/Fission-AI/OpenSpec)
- 적용 버전: `openspec.cmd --version` 기준 `1.3.1`
- 적용 명령: `openspec.cmd init --tools codex --force`
- 첫 변경안: `openspec/changes/line-linker-mvp-ai-designer/`

## 왜 적용했는가

포코팡 스타일 퍼즐 포트폴리오는 기능이 쉽게 넓어질 수 있다. 보드, 입력, 연쇄 제거, 레벨 데이터, AI 기획자, 리포트, 포트폴리오 문서가 한꺼번에 섞이면 Codex에게 매번 긴 맥락을 다시 설명해야 한다.

OpenSpec은 이 문제를 줄이기 위해 적용했다. 앞으로 큰 기능을 만들기 전에 `proposal.md`, `design.md`, `specs/`, `tasks.md`를 먼저 작성해서 "무엇을 왜 만들고, 어디까지 만들지"를 고정한다.

## 이 프로젝트에서의 역할

- `openspec/config.yaml`: 프로젝트 맥락, 기술 스택, IP 주의사항, 산출물 규칙 저장.
- `openspec/changes/`: 앞으로 만들 기능 단위의 변경안 저장.
- `openspec/specs/`: 완료 후 확정된 기능 요구사항 저장.
- `md/`: 사람이 읽기 쉬운 포트폴리오 설명과 AI 기획자 리포트 저장.
- `.codex/skills/poko-game-designer/SKILL.md`: Codex가 OpenSpec과 프로젝트 전용 규칙을 함께 따르도록 하는 스킬.

## 현재 만든 변경안

변경안 이름: `line-linker-mvp-ai-designer`

포함 내용:

- Line-Linker 퍼즐 MVP 요구사항.
- AI Game Designer Agent 요구사항.
- 현재 구현된 작업과 다음 작업 체크리스트.
- Unity Input System 호환성 요구사항.

## 사용 규칙

큰 기능을 시작할 때:

1. `openspec.cmd new change <change-name>`로 변경안을 만든다.
2. `proposal.md`에 왜 하는지 쓴다.
3. `design.md`에 어떻게 구현할지 쓴다.
4. `specs/<capability>/spec.md`에 요구사항과 시나리오를 쓴다.
5. `tasks.md`에 구현 체크리스트를 쓴다.
6. `openspec.cmd validate <change-name>`로 검증한다.
7. 구현 후 완료된 변경은 `openspec.cmd archive <change-name>`로 확정 스펙에 반영한다.

작은 수정에는 OpenSpec을 쓰지 않아도 된다. 예를 들어 오탈자, 단순 주석, 명백한 1줄 버그는 바로 고친다.

## 주의사항

PowerShell에서 `openspec`를 직접 실행하면 `.ps1` 실행 정책 때문에 막힐 수 있다. 이 프로젝트에서는 `openspec.cmd`를 사용한다.

초기화 중 Codex 스킬 자동 설치는 `.codex/skills/openspec-explore` 생성 단계에서 권한 오류가 발생했다. 대신 현재 프로젝트의 기존 스킬인 `.codex/skills/poko-game-designer/SKILL.md`에 OpenSpec 사용 규칙을 직접 연결했다.
