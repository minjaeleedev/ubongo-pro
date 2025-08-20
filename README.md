# Ubongo 3D - Unity Project

우봉고 3D 퍼즐 게임의 Unity 구현 프로젝트입니다.

## 🎮 게임 소개
3D 블록 조각들을 제한 시간 내에 보드의 지정된 영역에 맞춰 배치하는 퍼즐 게임입니다.

## 📁 프로젝트 구조
```
Assets/
├── Scripts/
│   ├── Core/           # 핵심 게임 로직
│   ├── GameBoard/       # 게임 보드 관련
│   ├── Pieces/          # 퍼즐 조각 관련
│   ├── UI/              # UI 관련
│   └── Managers/        # 게임 매니저
├── Prefabs/             # 프리팹
├── Materials/           # 머티리얼
├── Scenes/              # 씬 파일
└── Models/              # 3D 모델
```

## 🚀 Unity에서 시작하기

### 1. Unity 프로젝트 생성
1. Unity Hub 열기
2. "New Project" 클릭
3. 3D 템플릿 선택
4. 프로젝트 이름: "Ubongo3D"
5. 이 폴더를 프로젝트 위치로 선택

### 2. 씬 설정
1. 새 씬 생성 (File > New Scene)
2. 씬 저장 (Assets/Scenes/MainGame.unity)
3. 다음 GameObject들 생성:

#### Main Camera 설정
- Position: (0, 10, -10)
- Rotation: (45, 0, 0)
- Projection: Perspective

#### Directional Light
- 기본 설정 유지

#### GameManager
- 빈 GameObject 생성
- 이름: "GameManager"
- GameManager.cs 스크립트 추가

#### GameBoard
- 빈 GameObject 생성
- 이름: "GameBoard"
- Position: (0, 0, 0)
- GameBoard.cs 스크립트 추가

#### LevelGenerator
- 빈 GameObject 생성
- 이름: "LevelGenerator"
- LevelGenerator.cs 스크립트 추가

#### UIManager
- 빈 GameObject 생성
- 이름: "UIManager"
- UIManager.cs 스크립트 추가

### 3. UI 설정
1. Canvas 생성 (GameObject > UI > Canvas)
2. Canvas 아래에 다음 Panel들 생성:
   - MenuPanel
   - GamePanel
   - PausePanel
   - GameOverPanel
   - LevelCompletePanel

#### MenuPanel 구성
- Title Text: "Ubongo 3D"
- Start Button
- Quit Button

#### GamePanel 구성
- Timer Text (상단 중앙)
- Score Text (상단 우측)
- Level Text (상단 좌측)
- Pause Button (상단 우측 코너)

### 4. Layer 설정
Project Settings > Tags and Layers에서:
- Layer 8: "Board"
- Layer 9: "Piece"

### 5. 스크립트 연결
1. GameManager에 GameManager.cs 연결
2. GameBoard에 GameBoard.cs 연결
3. UIManager에 UIManager.cs 연결하고 UI 요소들 연결
4. LevelGenerator에 LevelGenerator.cs 연결

## 🎮 게임 플레이

### 조작법
- **마우스 드래그**: 퍼즐 조각 이동
- **Q/E**: Y축 회전
- **R**: X축 회전
- **F**: Z축 회전
- **ESC**: 일시정지

### 게임 규칙
1. 제한 시간 내에 모든 퍼즐 조각을 보드에 배치
2. 보드의 초록색 영역을 모두 채워야 레벨 클리어
3. 남은 시간에 비례해 보너스 점수 획득
4. 레벨이 올라갈수록 난이도 증가

## 🔧 커스터마이징

### 새로운 퍼즐 모양 추가
LevelGenerator.cs의 InitializeShapes() 메서드에서:
```csharp
new PieceShape("MyShape", new List<Vector3Int> {
    new Vector3Int(0, 0, 0),
    new Vector3Int(1, 0, 0),
    // 추가 블록 위치...
}, Color.red)
```

### 보드 크기 변경
GameBoard 컴포넌트의 Inspector에서:
- Width: 가로 크기
- Height: 세로 크기
- Depth: 깊이

## 📝 다음 단계 개발 계획
1. ✅ 기본 게임 메커니즘
2. ✅ 드래그 앤 드롭 시스템
3. ✅ 퍼즐 검증 로직
4. ✅ UI 시스템
5. ✅ 레벨 시스템
6. 🔲 사운드 효과
7. 🔲 파티클 효과
8. 🔲 더 많은 퍼즐 모양
9. 🔲 멀티플레이어 모드
10. 🔲 리더보드

## 🐛 알려진 이슈
- 퍼즐 조각이 빠르게 움직일 때 충돌 감지 문제 가능
- UI 스케일링이 다양한 해상도에서 조정 필요

## 📚 학습 리소스
- [Unity 공식 문서](https://docs.unity3d.com)
- [Unity Learn](https://learn.unity.com)
- [Brackeys YouTube](https://www.youtube.com/brackeys)

## 🤝 기여
웹 백엔드 개발자의 Unity 학습 프로젝트입니다.
피드백과 제안은 언제나 환영합니다!