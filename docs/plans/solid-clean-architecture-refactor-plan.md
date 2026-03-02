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

### T2-03 높이 상수(2) 단일화
Status: `DONE`  
Priority: `P1`  
Files:
1. `Assets/Scripts/Core/TargetArea.cs`
2. `Assets/Scripts/GameBoard/GameBoard.cs`
3. `Assets/Scripts/Core/PuzzleValidator.cs`
4. `Assets/Scripts/Managers/LevelGenerator.cs`
5. `Assets/Tests/EditMode/BoardPlacementServiceTests.cs`
6. `Assets/Tests/EditMode/BoardStateTests.cs`
7. `Assets/Tests/EditMode/BoardWinConditionServiceTests.cs`
8. `Assets/Tests/EditMode/GameBoardStateSyncTests.cs`
9. `Assets/Tests/EditMode/LevelGeneratorContractTests.cs`
10. `Assets/Tests/PlayMode/BoardHighlightPlayModeTests.cs`

작업:
1. 단일 기준 확정: Y축(층수) 규칙의 유일한 소스를 `TargetArea.RequiredHeight`로 고정
2. `GameBoard`의 `UbongoHeight` 제거 후 모든 높이 루프/경계/리사이즈를 `TargetArea.RequiredHeight` 참조로 치환
3. `PuzzleValidator`의 `MaxHeight` 제거 후 검증 로직/메시지에서 `TargetArea.RequiredHeight` 참조로 치환
4. `LevelGenerator`의 `MaxHeight` 제거 후 풋프린트 계산/보드 버퍼 크기/Y축 루프/경계를 `TargetArea.RequiredHeight`로 치환
5. `TargetArea.FillState`의 `totalTarget / 2` 하드코딩을 `TargetArea.RequiredHeight` 기반 계산으로 교체
6. `GameBoard.InitializeGrid(Vector3Int size)` 계약 명시:
   - `size.y`는 구조 규칙상 무시(고정 높이) 또는 불일치 경고 로깅 중 하나로 정책 확정
   - 확정 정책을 코드와 테스트에 반영
7. 용어 혼동 제거:
   - `LevelGenerator` 내 `depth = 2` 류 값은 Z축 풋프린트 깊이로 변수명/주석 분리 (`defaultFootprintDepth` 등)
   - 높이(Height, Y)와 깊이(Depth, Z)를 코드/주석에서 명확히 분리
8. 테스트 정합성 보강:
   - 높이 의미의 literal `2`를 `TargetArea.RequiredHeight` 참조로 전환
   - `LevelGeneratorContractTests`에 `BoardSize.y == TargetArea.RequiredHeight` 계약 검증 추가
   - 기존 테스트가 축 의미(높이 vs 깊이)를 혼동하지 않도록 값/이름 정리

수용 기준:
1. 런타임 코드에서 높이 전용 상수(`UbongoHeight`, `MaxHeight`)가 제거됨
2. Y축 규칙은 `TargetArea.RequiredHeight` 단일 소스만 사용
3. `FillState` 내부 층수 계산이 하드코딩 `2`에 의존하지 않음
4. `InitializeGrid(size.y)` 처리 정책이 문서/코드/테스트에서 일관됨
5. 높이/깊이 용어 혼동이 제거됨 (변수명/주석 기준)
6. EditMode/PlayMode 핵심 테스트 통과

검증 체크리스트:
1. `rg -n "UbongoHeight|MaxHeight" Assets/Scripts` 결과 0건
2. 높이 의미 하드코딩 `2` 잔존 여부 수동 점검 (`TargetArea.RequiredHeight = 2` 정의는 예외)
3. `rg -n "size\\.y" Assets/Scripts/GameBoard/GameBoard.cs`로 계약 구현 확인
4. 관련 테스트에서 높이 의미 값이 `TargetArea.RequiredHeight`를 참조하는지 확인

### T2-04 피스 정의 중복 제거
Status: `DONE`  
Priority: `P2`  
Files:
1. `Assets/Scripts/Pieces/PuzzlePiece.cs`
2. `Assets/Scripts/Core/PieceDefinition.cs`
3. `Assets/Tests/EditMode/GameplayMathTests.cs`

작업:
1. `GenerateDefaultShape()` 제거 또는 `PieceCatalog` 사용으로 대체
2. 기본 shape fallback 정책 단일화

수용 기준:
1. 피스 형상 정의 소스가 1곳만 유지

### T2-05 게임 모드/상태 enum 경계 정리
Status: `DONE`  
Priority: `P1`  
Files:
1. `Assets/Scripts/Managers/GameManager.cs`
2. `Assets/Scripts/Domain/GameMode.cs`
3. `Assets/Scripts/Application/Flow/GameState.cs`
4. `Assets/Scripts/Application/Policy/GameModePolicy.cs`
5. `Assets/Scripts/UI/UIManager.cs`
6. `Assets/Scripts/UI/GameHUD.cs`
7. `Assets/Tests/EditMode/CompositionRootSettingsTests.cs`
8. `Assets/Tests/PlayMode/DifficultyFlowPlayModeTests.cs`

작업:
1. `GameManager` 내부 enum 정의(`GameMode`, `GameState`) 제거
2. `GameMode`는 `Ubongo.Domain`, `GameState`는 `Ubongo.Application.Flow`로 분리
3. 정책/시스템/UI/테스트 참조 namespace 정합성 수정

수용 기준:
1. `GameMode`/`GameState`/`DifficultyLevel` 중복 정의 없음
2. `GameManager`는 경계 타입을 참조만 하고 자체 선언하지 않음

---

## Phase 3: 의존성 역전 + 계층 분리 (핵심 구조개선)
목표: Clean Architecture를 컴파일 경계로 강제한다.

### T3-01 런타임 asmdef 분리 (1차 골격)
Status: `DONE`  
Priority: `P1`

선행 정리(순환 참조 방지):
1. `Assets/Scripts/Managers/GameManager.cs`의 `Ubongo.Application.Bootstrap` 의존 제거
2. `Assets/Scripts/Application/Formatting/DifficultyDisplayFormatter.cs`가 `Ubongo.Systems.DifficultyConfig`를 직접 참조하지 않도록 시그니처 단순화
3. `Assets/Scripts/UI/UIManager.cs`, `Assets/Tests/EditMode/DifficultyDisplayFormatterTests.cs`를 formatter 시그니처 변경에 맞춰 수정
4. `TargetArea`/`PuzzleValidator`는 Domain asmdef 경계에 포함되도록 파일 위치를 조정 (namespace는 1차에서 유지 가능, 정합성 정리는 T4-02에서 처리)
5. `GameManager`의 `GameBoardFactory` 의존 제거 후 대체 경로를 명시:
   - 보드 생성 책임: `GameCompositionRoot`
   - 보드 참조 해석 책임: `GameManager` 내부(`SerializeField` 우선, 미할당 시 `FindAnyObjectByType<GameBoard>()`, 미발견 시 fail-fast 로그)

목표 asmdef 제안:
1. `Ubongo.Domain` (1차: Unity 참조 허용, 2차(T3-02): `noEngineReferences: true`)
2. `Ubongo.Application` (refs: Domain)
3. `Ubongo.Infrastructure` (refs: Domain, Application)
4. `Ubongo.Presentation` (refs: Application, Domain, Unity)
5. `Ubongo.Bootstrap` (composition root)

작업:
1. 기존 `Assets/Scripts/Ubongo.Runtime.asmdef`를 `Ubongo.Presentation` 역할로 전환 (GUID 유지, 파일 경로는 1차에서 유지)
2. `Assets/Scripts/Domain/Ubongo.Domain.asmdef` 생성
3. `Assets/Scripts/Application/Ubongo.Application.asmdef` 생성
4. `Assets/Scripts/Infrastructure/Ubongo.Infrastructure.asmdef` 생성
5. `Assets/Scripts/Application/Bootstrap/Ubongo.Bootstrap.asmdef` 생성 (nested asmdef)
6. 참조 방향 강제:
   - Application -> Domain
   - Infrastructure -> Domain, Application
   - Presentation -> Domain, Application, Infrastructure
   - Bootstrap -> Presentation, Infrastructure, Application, Domain
7. 테스트 asmdef(`Ubongo.Tests.EditMode`, `Ubongo.Tests.PlayMode`) 참조를 신규 경계에 맞게 갱신
   - 기본 참조: Presentation, Domain, Application, Infrastructure
   - `Application.Bootstrap` 타입을 사용하는 테스트는 Bootstrap 참조 포함 (`GameBoardStateSyncTests`, `GameplayMathTests`, `BoardHighlightPlayModeTests`, `CompositionRootSettingsTests`)
8. 빌드 검증 후 Task Status/Decision Log 업데이트
9. Unity 컴파일/테스트 검증(HITL):
   - Unity 에디터 리컴파일에서 asmdef 순환/해결 실패 0건
   - EditMode/PlayMode 핵심 테스트 실행 및 결과 기록
10. asmdef 생성/전환(1~5)은 단일 커밋으로 원자 적용 (중간 단계 컴파일 실패 상태를 커밋하지 않음)

수용 기준:
1. asmdef 경계가 생성되고 참조 방향 역전/순환 참조가 없음
2. `GameManager`가 `Bootstrap` asmdef를 직접 참조하지 않음
3. `Application` asmdef가 `Systems`/`Managers` 타입을 직접 참조하지 않음
4. EditMode/PlayMode 테스트 어셈블리 컴파일 유지
5. `GameManager`가 `GameBoard`를 정상적으로 참조 해석하며 보드 생성 책임은 `GameCompositionRoot`에만 남음
6. 기능 컴파일이 유지됨 (Domain noEngine 강제는 T3-02에서 완료)

검증 체크리스트:
1. `rg -n "using Ubongo\\.Application\\.Bootstrap" Assets/Scripts/Managers/GameManager.cs` 결과 0건
2. `rg -n "using Ubongo\\.Systems" Assets/Scripts/Application` 결과 0건
3. `dotnet build Ubongo.Tests.EditMode.csproj`
4. `dotnet build Ubongo.Tests.PlayMode.csproj`
5. Unity 에디터(또는 batchmode)에서 스크립트 리컴파일 성공 여부 확인
6. Unity Test Runner로 EditMode/PlayMode 핵심 시나리오 실행 결과 기록

### T3-02 Domain 타입에서 Unity 제거
Status: `TODO`  
Priority: `P1`  
Files:
1. `Assets/Scripts/Domain/Board/*`
2. `Assets/Scripts/Domain/Rules/*`
3. `Assets/Scripts/Domain/Ubongo.Domain.asmdef`
4. `Assets/Scripts/GameBoard/GameBoard.cs` (어댑터 경계 반영)
5. `Assets/Scripts/Managers/LevelGenerator.cs` (좌표 변환 경계 반영)

작업:
1. Domain 전용 좌표/크기 값 타입 도입 (`GridPos`, `GridSize`, `GridBounds` 등)
2. `Domain` 내부 `Vector3Int`/`Vector2Int`/`Mathf` 제거 (순수 C# 연산으로 치환)
3. `IBoardQuery`/`IBoardCommand`/`BoardState`/`PuzzleValidator`/`TargetArea` 시그니처를 도메인 값 타입 기준으로 통일
4. Presentation 계층(`GameBoard`, `LevelGenerator`)에 Unity <-> Domain 변환 어댑터를 두고 경계를 명시
5. `Ubongo.Domain` asmdef를 `noEngineReferences: true`로 전환

수용 기준:
1. Domain 네임스페이스 내 `using UnityEngine` 0건
2. Domain 공개 API에서 Unity 타입 노출 0건
3. Domain asmdef가 `noEngineReferences: true`로 빌드됨

검증 체크리스트:
1. `rg -n "using UnityEngine|Vector3Int|Vector2Int|Mathf" Assets/Scripts/Domain --glob '*.cs'` 결과 0건
2. `cat Assets/Scripts/Domain/Ubongo.Domain.asmdef`에서 `"noEngineReferences": true` 확인
3. Domain 관련 EditMode 테스트가 GameObject 생성 없이 통과

### T3-03 씬 컴포넌트 조립 고정 + 싱글턴 자동 생성 제거 (Scene-owned 확정)
Status: `IN_PROGRESS`  
Priority: `P1`  
Files:
1. `Assets/Scripts/Managers/GameManager.cs`
2. `Assets/Scripts/Systems/RoundManager.cs`
3. `Assets/Scripts/Systems/GemSystem.cs`
4. `Assets/Scripts/Systems/DifficultySystem.cs`
5. `Assets/Scripts/Systems/TiebreakerManager.cs`
6. `Assets/Scripts/Application/Bootstrap/GameCompositionRoot.cs`
7. `Assets/Scripts/UI/UIManager.cs`
8. `Assets/Scripts/UI/GameHUD.cs`
9. `Assets/Scripts/UI/ResultPanel.cs`
10. `Assets/Scripts/UI/DebugPanel.cs`
11. `Assets/Scripts/Application/DependencyInjection/*`
12. `Assets/Scenes/MainScene.unity`

수명 정책(고정):
1. 옵션 A(Scene-owned)만 허용한다. 옵션 B(Persistent-owned)는 본 계획 범위에서 제외한다.
2. `GameBoard`도 예외 없이 Scene-owned로 고정한다.
3. 본 문서를 단일 실행 계획 문서(Single Source of Truth)로 사용한다.

생성 책임(고정):
1. `GameManager`/`RoundManager`/`GemSystem`/`DifficultySystem`/`TiebreakerManager`/`InputManager`/`LevelGenerator`/`UIManager`/`GameBoard`는 `MainScene`에서 명시 배치
2. `GameCompositionRoot`는 생성기가 아니라 조립기이며, 누락/중복 검증 + 연결만 수행

작업:
1. `GameCompositionRoot`를 단일 조립 진입점으로 고정하고, 필수 그래프를 명시 필드로 검증 후 연결
2. `MonoBehaviour` 간 의존성 연결을 Root의 명시 연결 메서드(`Configure...`)로 통일
3. `[Inject]`/`[RequiredDependency]`/`[PostInject]` + 반사 주입기 경로 제거
4. `Instance` getter 내부 `new GameObject(...)` 경로 제거 및 no-create 조회로 고정
5. 필수 컴포넌트 누락/중복은 `Awake`에서 fail-fast로 중단
6. UI 계층의 반복 null-check는 조립 시점 검증으로 대체하고 런타임 분기 제거
7. `GameBoardFactory.ResolveOrCreate(...)` 및 `new GameObject("GameBoard")` 경로 제거
8. `GameCompositionRoot`는 `GameBoard` 누락 시 생성하지 않고 fail-fast
9. `DontDestroyOnLoad` 경로는 Scene-owned 정책과 일치하도록 제거/금지

수용 기준:
1. 런타임에 숨은 오브젝트 자동 생성 없음 (`GameBoard` 포함)
2. `Application/DependencyInjection` 네임스페이스가 런타임 경로에서 제거됨
3. `UIManager`/`GameHUD`/`ResultPanel`/`DebugPanel`가 Root 경유 없이 동작 시작하지 않음
4. `GameCompositionRoot`가 `GameBoard`를 자동 생성하지 않고, 누락 시 즉시 실패한다

검증 체크리스트:
1. `rg -n "\\[Inject|\\[RequiredDependency|\\[PostInject" Assets/Scripts --glob '*.cs'` 결과 0건
2. `rg -n "new GameObject\\(\"(GameManager|RoundManager|GemSystem|DifficultySystem|TiebreakerManager|InputManager|GameBoard)\"" Assets/Scripts --glob '*.cs'` 결과 0건
3. `rg -n "ResolveOrCreate\\(|GameBoardFactory" Assets/Scripts/Application/Bootstrap --glob '*.cs'` 결과 0건
4. `rg -n "DontDestroyOnLoad\\(" Assets/Scripts --glob '*.cs'` 결과 0건
5. Root 누락/필수 컴포넌트 누락(`GameBoard` 포함) 시 fail-fast 테스트 통과

### T3-04 GameBoard -> GameManager 직접 호출 제거
Status: `TODO`  
Priority: `P1`  
Files:
1. `Assets/Scripts/GameBoard/GameBoard.cs`
2. `Assets/Scripts/Managers/GameManager.cs` or Application use case layer

작업:
1. 보드 완료 이벤트만 발행
2. 상위 오케스트레이터가 라운드 완료 호출 담당

수용 기준:
1. `GameBoard`에서 `GameManager.Instance` 참조 제거

### T3-05 설정 저장 경로 통합
Status: `TODO`  
Priority: `P1`  
Files:
1. `Assets/Scripts/UI/UIManager.cs`
2. `Assets/Scripts/Infrastructure/Settings/*`
3. `Assets/Scripts/Application/Bootstrap/GameCompositionRoot.cs`
4. `Assets/Scripts/Managers/GameManager.cs`

작업:
1. `PlayerPrefs` 직접 호출 제거
2. `ISettingsStore` 또는 전용 `IProgressStore` 포트 사용

수용 기준:
