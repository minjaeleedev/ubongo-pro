# Ubongo 3D Pro - Master Requirements Document

## 문서 정보
- 버전: 1.0
- 작성일: 2026-02-03
- 프로젝트: Ubongo 3D Unity Game

---

## 요구사항 문서 목록

| 번호 | 문서명 | 담당 역할 | 설명 |
|------|--------|-----------|------|
| 01 | game_rules_and_requirements.md | 기획자 | 게임 규칙, 요구사항 정의 |
| 02 | ui_ux_requirements.md | UI/UX 개발자 | 인터페이스 요구사항 |
| 03 | core_logic_requirements.md | Core 개발자 | 핵심 로직 요구사항 |
| 04 | design_requirements.md | 디자이너 | 디자인 요구사항 |

---

## 원본 Ubongo 3D 게임 규칙 요약

### 핵심 메커니즘
1. **목표**: 3D 폴리오미노 조각들을 지정된 영역에 **정확히 2층 높이**로 채움
2. **시간 제한**: 모래시계(약 60-90초) 내에 퍼즐 완성
3. **경쟁**: 가장 먼저 완성한 플레이어가 가장 좋은 보상 획득

### 게임 구성요소
- **플레이어**: 1-4명
- **조각**: 플레이어당 8개의 3D 폴리오미노 조각
- **퍼즐 카드**: 36개 카드, 4단계 난이도 (초록/노랑/파랑/빨강)
- **보석**: 빨강(4점), 파랑(3점), 초록(2점), 호박(1점)
- **주사위**: 10면체 - 사용할 조각 결정

### 라운드 진행
1. 퍼즐 카드 뒤집기
2. 주사위 굴려 사용할 조각 3-4개 결정
3. 타이머 시작
4. 조각 회전/뒤집기하여 배치
5. "Ubongo!" 외치며 완료 선언
6. 보석 획득 (순위별)

### 승리 조건
- 9라운드 후 가장 높은 보석 가치 보유

---

## 검토 결과 (Gaps & Issues)

### 원본 규칙 대비 누락 사항 (수정 완료)

| 항목 | 원본 규칙 | 이전 문서 상태 | 수정 상태 |
|------|-----------|----------------|-----------|
| 조각 뒤집기 | 허용됨 | "불가"로 잘못 기술 | ✅ 수정됨 |
| 조각 구성 | 3-4블록 조각 8개 | 8블록 Cube 포함 | ✅ 수정됨 |
| 주사위 시스템 | 10면체 주사위로 조각 결정 | 미구현 | 📝 문서화됨 |
| Second Chance Round | 전원 실패 시 재도전 라운드 | ❌ 누락됨 | ✅ 문서화됨 (2026-02-03) |
| Tiebreaker Rules | 동점 시 타이머 없는 결승전 | ⚠️ 불완전 | ✅ 확장됨 (2026-02-03) |
| Finite Gem Pool | 58개 보석 유한 풀 시스템 | ❌ 미명시 | ✅ 명세 추가 (2026-02-03) |

### 구현 완료 상태 (2026-02-03 업데이트)

| Gap | 상태 | 구현 파일 |
|-----|------|----------|
| 2층 높이 검증 | ✅ 완료 | `PuzzleValidator.cs`, `GameBoard.cs` |
| 보석 시스템 | ✅ 완료 | `GemSystem.cs` |
| 난이도 시스템 | ✅ 완료 | `DifficultySystem.cs` |
| 9라운드 시스템 | ✅ 완료 | `RoundManager.cs` |
| 8개 정확한 조각 | ✅ 완료 | `PieceDefinition.cs` |
| UI 개선 | ✅ 완료 | `UIManager.cs`, `GameHUD.cs`, `ResultPanel.cs` |
| 블록 비주얼 | ✅ 완료 | `BlockVisualizer.cs`, `GameColors.cs` |
| 보석 비주얼 | ✅ 완료 | `GemVisual.cs` |
| 테스트베드 | ✅ 완료 | `DebugPanel.cs` |

### 남은 작업 (Phase 3-4)

1. **Second Chance Round 구현** (HIGH) 🆕
   - 현재: 규칙 문서화됨, 미구현
   - 필요: `RoundManager.cs`에 SecondChance 상태 추가
   - 참조: `01_game_rules_and_requirements.md` Section 7.5

2. **Tiebreaker Round 구현** (HIGH) 🆕
   - 현재: 규칙 확장 문서화됨, 미구현
   - 필요: `GameManager.cs`에 Tiebreaker 상태 추가
   - 참조: `01_game_rules_and_requirements.md` Section 8.2.1

3. **Finite Gem Pool 구현** (MEDIUM) 🆕
   - 현재: 명세 추가됨, 미구현
   - 필요: `GemPoolManager.cs` 신규 생성
   - 참조: `03_core_logic_requirements.md` Section 6.4
   - 우선순위: 무한 모드 기본, 유한 모드 옵션

4. **주사위 시스템 UI** (Medium)
   - 현재: 로직은 있으나 시각적 주사위 미구현
   - 필요: 3D 주사위 애니메이션

5. **사운드/이펙트** (Medium)
   - 현재: 코드 구조만 존재
   - 필요: 실제 오디오 에셋

6. **멀티플레이어** (Low)
   - 현재: 싱글플레이어만 완전 지원
   - 필요: 네트워크 동기화

---

## 구현 우선순위 로드맵

### Phase 1: Core Mechanics ✅ 완료
- [x] 2층 높이 검증 로직 구현 (`PuzzleValidator.cs`)
- [x] 정확한 8개 조각 정의 및 구현 (`PieceDefinition.cs`)
- [x] 조각 회전/뒤집기 시스템 완성 (`RotationUtil`)
- [x] 기본 보석 시스템 구현 (`GemSystem.cs`)

### Phase 2: Game Structure ✅ 완료
- [x] 4단계 난이도 시스템 (`DifficultySystem.cs`)
- [x] 9라운드 게임 구조 (`RoundManager.cs`)
- [x] 주사위 시스템 로직 (`UIManager.cs`)
- [x] 퍼즐 카드 데이터 구조 (`TargetArea.cs`, `LevelGenerator.cs`)

### Phase 3: Polish & Feedback (진행 중)
- [x] 시각적 피드백 개선 (`BlockVisualizer.cs`, `GameHUD.cs`)
- [ ] 사운드 효과 추가 (에셋 필요)
- [x] 애니메이션 폴리시 (`GemVisual.cs`, `ResultPanel.cs`)
- [ ] 튜토리얼 구현
- [ ] Second Chance Round 구현 🆕 (HIGH)
- [ ] Tiebreaker Round 구현 🆕 (HIGH)
- [ ] Finite Gem Pool 구현 🆕 (MEDIUM - 옵션)

### Phase 4: Multiplayer & Extra (미시작)
- [ ] 로컬 멀티플레이어 (2-4인)
- [ ] 온라인 멀티플레이어 기반
- [ ] 리더보드
- [ ] 추가 게임 모드

---

## 기술 스택

- **Engine**: Unity 2022.3+ LTS
- **Language**: C# 10
- **UI**: Unity UI (uGUI) 또는 UI Toolkit
- **Network** (향후): Unity Netcode for GameObjects
- **Audio**: Unity Audio System
- **Build Targets**: PC, Mac, Mobile (iOS/Android)

---

## 참고 자료

### 게임 규칙 출처
- [Ubongo 3D Official Rules - UltraBoardGames](https://www.ultraboardgames.com/ubongo/ubongo-3d.php)
- [Ubongo Official Rules - UltraBoardGames](https://www.ultraboardgames.com/ubongo/game-rules.php)
- [Ubongo 3D - BoardGameGeek](https://boardgamegeek.com/boardgame/46396/ubongo-3d)
- [Ubongo 3D Review - What's Eric Playing](https://whatsericplaying.com/2022/01/24/ubongo-3d/)

### 기술 참고
- [Unity Documentation](https://docs.unity3d.com)
- [Unity Learn](https://learn.unity.com)

---

## 버전 히스토리

| 버전 | 날짜 | 변경 내용 |
|------|------|----------|
| 1.0 | 2026-02-03 | 초기 문서 작성, 4개 역할별 요구사항 문서 생성 |
| 1.0.1 | 2026-02-03 | 조각 정의 수정 (뒤집기 규칙, 8블록 Cube 제거) |
| 2.0 | 2026-02-03 | Phase 1-2 구현 완료, 18개 C# 파일 생성 |
| 2.1 | 2026-02-03 | Gap 분석 결과 반영: Second Chance Round, Tiebreaker Rules, GemPool 명세 추가 |

---

## 구현된 파일 목록

### Core (6개)
- `PieceDefinition.cs` - 8개 3D 폴리오미노 조각 + 24개 회전 행렬
- `TargetArea.cs` - 유연한 퍼즐 타겟 영역
- `PuzzleValidator.cs` - 2층 높이 검증 알고리즘
- `GameColors.cs` - 색상 팔레트 정의
- `BlockVisualizer.cs` - 블록 상태별 비주얼
- `GemVisual.cs` - 보석 비주얼 및 애니메이션

### Systems (3개)
- `GemSystem.cs` - 보석 획득/점수 시스템
- `RoundManager.cs` - 9라운드 진행 시스템
- `DifficultySystem.cs` - 4단계 난이도 시스템

### Managers (2개)
- `GameManager.cs` - 게임 상태 관리 (수정됨)
- `LevelGenerator.cs` - 퍼즐 생성 (수정됨)

### GameBoard (2개)
- `GameBoard.cs` - 게임 보드 (수정됨)
- `BoardCell.cs` - 보드 셀

### Pieces (1개)
- `PuzzlePiece.cs` - 퍼즐 조각 (수정됨)

### UI (4개)
- `UIManager.cs` - UI 관리 (수정됨)
- `GameHUD.cs` - 게임 HUD
- `ResultPanel.cs` - 결과 화면
- `DebugPanel.cs` - 테스트베드 UI

---

*Generated for Ubongo 3D Pro Unity Project*
