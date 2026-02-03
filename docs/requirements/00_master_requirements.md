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

### 현재 구현 상태 대비 Critical Gaps

1. **2층 높이 검증 로직 미구현** (Critical)
   - 현재: 1층만 검증
   - 필요: 정확히 2층 검증

2. **보석 시스템 미구현** (Critical)
   - 현재: 단순 점수만 존재
   - 필요: 4종 보석, 획득 로직

3. **난이도 시스템 미구현** (Critical)
   - 현재: 레벨만 존재
   - 필요: 4단계 난이도 (초록/노랑/파랑/빨강)

4. **9라운드 시스템 미구현** (High)
   - 현재: 무한 레벨 진행
   - 필요: 9라운드 게임 구조

5. **주사위 시스템 미구현** (High)
   - 현재: 랜덤 조각 선택
   - 필요: 주사위 기반 조각 결정

---

## 구현 우선순위 로드맵

### Phase 1: Core Mechanics (2-3주)
- [ ] 2층 높이 검증 로직 구현
- [ ] 정확한 8개 조각 정의 및 구현
- [ ] 조각 회전/뒤집기 시스템 완성
- [ ] 기본 보석 시스템 구현

### Phase 2: Game Structure (2-3주)
- [ ] 4단계 난이도 시스템
- [ ] 9라운드 게임 구조
- [ ] 주사위 시스템 UI/로직
- [ ] 퍼즐 카드 데이터 구조

### Phase 3: Polish & Feedback (2주)
- [ ] 시각적 피드백 개선
- [ ] 사운드 효과 추가
- [ ] 애니메이션 폴리시
- [ ] 튜토리얼 구현

### Phase 4: Multiplayer & Extra (3-4주)
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

---

*Generated for Ubongo 3D Pro Unity Project*
