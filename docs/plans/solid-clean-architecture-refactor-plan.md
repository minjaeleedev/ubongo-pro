# SOLID + Clean Architecture Refactor Plan
Last updated: 2026-02-26  
Repository: `ubongo-pro`  
Scope: `Assets/Scripts` runtime code, minimal editor/test support updates

## 목적
현재 코드베이스를 기능 회귀 없이 점진적으로 리팩터링하여 다음을 달성한다.

1. 논리 결함(P0) 제거
2. SOLID 위반 축소
3. Clean Architecture 경계 강제 (컴파일 레벨)
4. 컨텍스트 단절 후에도 재개 가능한 실행 계획 유지

## 시작 전 핵심 사실
현재 구조의 대표 문제:

1. 싱글턴 + Service Locator 결합이 강함 (`*.Instance` 다수)
2. Domain 계층이 `UnityEngine` 타입에 직접 의존
3. 대형 클래스에 책임 집중 (`GameManager`, `GameBoard`, `PuzzlePiece`, `UIManager`, `ResultPanel`)
4. 모델/상수 중복 (`GemType`, 난이도 enum, 높이 상수 2)
5. P0 논리 결함 존재 (입력 이벤트 해제, 레벨 생성 실패 처리 등)

## 작업 원칙
1. Big-bang 금지: 단계적 마이그레이션
2. 매 단계마다 컴파일/테스트 가능한 상태 유지
3. 동작 변경이 있는 작업은 테스트 먼저 추가 또는 동시 추가
4. 기존 런타임 자동 생성 경로는 단계적으로 축소하되, T3-03 완료 시점에 전면 제거한다
5. 모든 작업은 작은 커밋으로 분리 (`fix:`, `refactor:`, `test:`, `docs:`)
6. DI 경계 고정: 순수 C# 타입만 생성자 DI 사용, `MonoBehaviour`는 `GameCompositionRoot`에서 명시 연결
7. Reflection 기반 자동 주입(`[Inject]` 등)은 임시 단계로만 허용하고 최종 상태에서는 제거
8. Scene-owned 고정: `GameBoard` 포함 모든 런타임 컴포넌트는 씬에서 소유하며 런타임 자동 생성 금지

## 완료 정의 (Definition of Done)
아래 8개를 모두 만족하면 본 계획 완료:

1. P0 버그 100% 해결
2. `Domain` asmdef가 `noEngineReferences: true`로 빌드 가능
3. `GameBoard` 포함 `new GameObject` 기반 런타임 자동 생성 제거
4. `UIManager`/`GameManager` 상태 전이 누락 없음
5. 중복 enum/상수 제거 완료
6. 핵심 흐름(EditMode + PlayMode 핵심 시나리오) 테스트 통과
7. `MonoBehaviour` 간 의존성 연결이 `GameCompositionRoot` 명시 연결로 통일됨
8. 순수 C# 오케스트레이션/규칙 로직이 생성자 DI로 조립되고 Unity 수명주기와 분리됨

## 실행 순서 (Phase)

## Phase 0: 안전장치와 기준선 확보
목표: 리팩터링 전 기준선과 실패 감지 장치를 만든다.

### T0-01 테스트 실행 환경 점검
Status: `DONE`  
Priority: `P0`

작업:
1. Unity 에디터 중복 실행 상태 확인
2. EditMode/PlayMode 테스트 배치 실행 스크립트 점검
3. 로그 경로 표준화 (`Logs/editmode.xml`, `Logs/playmode.xml`)

검증:
1. `Ubongo.Tests.EditMode` 실행 가능
2. `Ubongo.Tests.PlayMode` 실행 가능

현재 상태:
1. Licensing 초기화 실패 재현 경로는 해소됨
2. Unity 에디터가 프로젝트를 열고 있으면 batchmode 테스트는 동시 실행 제한으로 실패 가능
3. EditMode/PlayMode 테스트는 HITL 실행 기준 통과 확인

### T0-02 리팩터링 트래킹 템플릿 생성
Status: `DONE`  
Priority: `P1`

작업:
1. 본 문서의 Task Status를 매 작업 후 업데이트
2. 각 Task별 완료 커밋 SHA 기록 섹션 유지

검증:
1. 최소 1개 Task 완료 시 SHA 기록 확인

---

## Phase 1: 즉시 수정해야 할 논리 결함 (P0)
목표: 사용자 경험/정합성을 해치는 결함을 먼저 제거한다.

### T1-01 InputManager 디버그 액션 unsubscribe 버그 수정
Status: `DONE`  
Priority: `P0`  
Files:
1. `Assets/Scripts/Input/InputManager.cs`

문제:
1. `+= ctx => ...` / `-= ctx => ...`가 서로 다른 delegate라 해제가 동작하지 않음

작업:
1. 디버그 액션마다 명시적 핸들러 메서드 생성
2. `SubscribeToDebugActions`에서 메서드 참조 등록
3. `UnsubscribeFromDebugActions`에서 동일 메서드 참조 해제

수용 기준:
1. `OnEnable/OnDisable` 반복 시 이벤트 중복 호출 없음
2. 메모리 누수/중복 토글 재현 불가

테스트:
1. EditMode: 이벤트 등록/해제 반복 테스트 추가
2. PlayMode: 디버그 토글 키 연타 시 콜백 호출 횟수 검증

### T1-02 LevelGenerator 실패 처리 보강
Status: `DONE`  
Priority: `P0`  
Files:
1. `Assets/Scripts/Managers/LevelGenerator.cs`

문제:
1. 해 탐색 실패해도 빈 `SolutionPlacements`로 반환 가능
2. `maxGenerationAttempts <= 0`에서 `targetArea` null 경로 존재

작업:
1. `maxGenerationAttempts` 최소값 강제 (`OnValidate` 또는 실행 시 guard)
2. 생성 루프 실패 시 명시적 실패 처리:
   - 옵션 A: 예외 throw
   - 옵션 B: fallback deterministic puzzle 생성
   - 옵션 C: `TryCreate...` 패턴으로 bool 반환
3. 호출자(`GameManager` 등)에 실패 대응 경로 추가

수용 기준:
1. 실패 시 "성공처럼 보이는 레벨" 반환 금지
2. null 참조 크래시 경로 제거

테스트:
1. EditMode: `maxGenerationAttempts = 0` 방어 테스트
2. EditMode: 생성 실패 시 반환 계약 테스트

### T1-03 UIManager 상태 전이 누락 보강
Status: `DONE`  
Priority: `P0`  
Files:
1. `Assets/Scripts/UI/UIManager.cs`
2. `Assets/Scripts/Managers/GameManager.cs`

문제:
1. `GameState` 대비 UI switch case 누락 (`GameComplete`, `RoundFailed`, `SecondChance`, `Tiebreaker`, `TiebreakerComplete`)

작업:
1. 모든 `GameState`를 명시적으로 처리
2. 패널 노출 정책 표 정의 후 코드 반영
3. default 분기에서 경고 로그 추가

수용 기준:
1. 어떤 상태에서도 패널 공백/죽은 화면 없음

테스트:
1. EditMode 또는 PlayMode: 상태별 패널 가시성 검증

### T1-04 UI 난이도 선택과 실제 게임 난이도 연결
Status: `DONE`  
Priority: `P0`  
Files:
1. `Assets/Scripts/UI/UIManager.cs`
2. `Assets/Scripts/Managers/GameManager.cs`
3. `Assets/Scripts/Systems/DifficultySystem.cs` (필요 시)

문제:
1. UI 난이도 enum이 게임 난이도 시스템과 분리됨
2. `OnStartGame`가 `StartGame()` 기본 경로로 호출됨

작업:
1. `UIManager.Difficulty` 제거 또는 매핑 테이블 도입
2. 시작 시 `GameManager.StartGame(DifficultyLevel)` 명시 호출
3. 화면 난이도 표시도 동일 소스 사용

수용 기준:
1. UI에서 선택한 난이도가 실제 라운드 규칙에 반영됨

테스트:
1. PlayMode: 난이도별 시간 제한/피스 수 변화 검증

### T1-05 Zen 모드 힌트 플래그 누수 방지
Status: `DONE`  
Priority: `P1`  
Files:
1. `Assets/Scripts/Managers/GameManager.cs`

문제:
1. `StartZenMode`에서 `enableHints = true` 후 초기화 불명확

작업:
1. 모드 전환/게임 시작 시 `enableHints` 초기화 정책 명시
2. `ResetGameState` 또는 모드별 설정 함수 분리

수용 기준:
1. Zen 이후 Classic/TimeAttack에서 힌트 상태가 의도대로 복원

### T1-06 멀티플레이어 타이브레이커 TODO 해소
Status: `DONE`  
Priority: `P1`  
Files:
1. `Assets/Scripts/Managers/GameManager.cs`
2. `Assets/Scripts/Systems/TiebreakerManager.cs` (필요 시)

문제:
1. `CheckForTiebreaker`가 TODO 상태로 실질 우회

작업:
1. 임시 정책 확정: 옵션 A 적용 (멀티플레이어 비활성화 명시)
2. 정책에 맞는 UI/상태 전이 적용

수용 기준:
1. 멀티플레이어 종료 흐름이 TODO 없이 동작/차단 정책이 명확

---

## Phase 2: 중복 모델/규칙 통합 (P1)
목표: 분산된 규칙 소스를 단일화하여 회귀 위험을 낮춘다.

### T2-01 `GemType` 단일화
Status: `DONE`  
Priority: `P1`  
Files:
1. `Assets/Scripts/Core/GameColors.cs`
2. `Assets/Scripts/Systems/GemSystem.cs`
3. `Assets/Scripts/Core/GemVisual.cs`
4. `Assets/Scripts/Domain/GemType.cs`
5. `Assets/Scripts/Core/GemDefinitionCatalog.cs`
6. `Assets/Tests/EditMode/GemDefinitionCatalogTests.cs`

작업:
1. `GemType` 정의를 단일 위치로 이동 (`Ubongo.Domain`) (`DONE`)
2. 컬러/포인트/아이콘은 매핑 테이블로 분리 (`DONE`)
3. 컴파일 오류 기준으로 참조 전부 정리 (`DONE`)

수용 기준:
1. 프로젝트 내 `GemType` 정의 1개만 존재
2. 컬러/포인트/아이콘 규칙이 중복 switch 없이 단일 매핑 소스로 유지됨

### T2-02 난이도 타입 단일화
Status: `DONE`  
Priority: `P1`  
Files:
1. `Assets/Scripts/UI/UIManager.cs`
2. `Assets/Scripts/Systems/DifficultySystem.cs`
3. `Assets/Scripts/Managers/LevelGenerator.cs`
4. `Assets/Scripts/Domain/DifficultyLevel.cs`

작업:
1. `UIManager` 자체 `Difficulty` enum 제거 (`DONE`)
2. `DifficultyLevel` 단일 사용 + 공용 정의를 `Ubongo.Domain`으로 이동 (`DONE`)
3. 표시 문자열은 formatter/service로 분리 (`DONE`)

수용 기준:
1. 난이도 enum 정의 1개만 존재
2. 난이도 표시 문자열이 UI 내부 하드코딩 switch가 아닌 formatter/service 경유로 생성됨
