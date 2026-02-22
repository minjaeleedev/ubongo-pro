---
description: "Ubongo Pro 프로젝트 아키텍처, 도메인 규칙, 파일 동기화 매트릭스 레퍼런스"
---

# Ubongo Pro Architecture Reference

## Project Structure

```
Assets/Scripts/
├── Core/           # Data definitions & validation
│   ├── BlockVisualizer.cs
│   ├── GameColors.cs        # Color palette (8 block, 4 gem, UI)
│   ├── GemVisual.cs
│   ├── PieceDefinition.cs   # 8 pieces, 24 rotations
│   ├── PuzzleValidator.cs   # Placement validation
│   └── TargetArea.cs
├── GameBoard/      # Board and cell management
│   ├── BoardCell.cs
│   └── GameBoard.cs         # Grid creation, colliders
├── Input/          # Player input handling
│   ├── InputManager.cs      # Event-driven, singleton
│   └── UbongoInputActions.cs
├── Managers/       # Game flow control
│   ├── GameManager.cs       # Main orchestrator, singleton
│   └── LevelGenerator.cs    # Difficulty configs, piece spawning
├── Pieces/         # Puzzle piece behavior
│   └── PuzzlePiece.cs       # Block creation, rotation, placement
├── Systems/        # Subsystems
│   ├── DifficultySystem.cs
│   ├── GemSystem.cs
│   ├── RoundManager.cs
│   └── TiebreakerManager.cs
└── UI/             # User interface
    ├── DebugPanel.cs
    ├── GameHUD.cs
    ├── ResultPanel.cs
    └── UIManager.cs
Assets/Editor/
    └── CameraSetupTool.cs   # Editor camera setup tool
Assets/Scenes/
    └── MainScene.unity      # Main game scene
```

## System Dependency Graph

```
GameManager (Orchestrator)
├── RoundManager        — round flow, timer
├── GemSystem           — gem rewards
├── DifficultySystem    — difficulty progression
├── TiebreakerManager   — tiebreaker logic
├── LevelGenerator      — level/piece creation
├── GameBoard           — board grid
├── InputManager        — player input events
└── UIManager           — UI updates
```

## Singletons

GameManager, InputManager, RoundManager, GemSystem, DifficultySystem, TiebreakerManager

## Data Flow

```
Input (mouse/keyboard)
  → InputManager (events: OnPieceSelectStart, OnPieceDrag, OnPieceRotate, OnPieceSelectEnd)
    → PuzzlePiece (movement, rotation)
      → GameBoard (cell overlap detection)
        → PuzzleValidator (placement validity check)
          → GameManager (state transitions)
```

## Key Enums

### GameState (11 states)
Menu, Setup, Playing, Paused, PuzzleSolved, RoundComplete, GameOver, Tiebreaker, Results, Tutorial, Loading

### PlacementState (7 states)
InHand, Hovering, Placed, Snapped, Invalid, Returning, Locked

### PlacementValidity (5 values)
Valid, OutOfBounds, Overlapping, NotOnTarget, InvalidHeight

## Ubongo 3D Domain Rules

- **Board**: 2 layers max (y=0 ground, y=1 second layer)
- **Pieces**: 8 standard pieces, each with unique color
- **Rotations**: 24 possible orientations (cube symmetry group)
- **Goal**: Fill target area completely with selected pieces
- **Difficulty levels**: Easy(3pc/12blk), Medium(4pc/16blk), Hard(5pc/20blk), Expert(6pc/24blk)
- **Gem system**: Rewards based on solve speed
- **Timer**: Countdown per difficulty level

## File Synchronization Matrix

This matrix shows which files must be updated together when a parameter changes.

| Parameter Change | Files Affected |
|---|---|
| Camera position/rotation/orthoSize | `GameManager.cs`, `CameraSetupTool.cs`, `MainScene.unity` |
| Cell size or spacing | `GameBoard.cs` |
| Cell height | `GameBoard.cs` (CreateDefaultCell + AddBoardCollider) |
| Block height | `PuzzlePiece.cs` (CreateBlock + CalculateBounds) |
| Layer offset | `PuzzlePiece.cs` (CreateBlock) — must keep gap >= 0.05f |
| Add/remove piece | `PieceDefinition.cs`, `GameColors.cs`, `LevelGenerator.cs` |
| Change piece shape | `PieceDefinition.cs` |
| Change piece color | `GameColors.cs` |
| Difficulty config | `LevelGenerator.cs` |
| Input bindings | `InputManager.cs`, `UbongoInputActions.cs` |
| UI layout | `UIManager.cs` + specific panel script |

## Current Visual Parameters

| Component | Parameter | Value |
|---|---|---|
| Camera | Position | `(0, 10, -7)` |
| Camera | Euler Rotation | `(35, 0, 0)` |
| Camera | Ortho Size | `5` |
| Cell | Size | `1f` |
| Cell | Spacing | `0.1f` |
| Cell | Scale | `(0.9, 0.3, 0.9)` |
| Board Collider | Size Y | `0.4f` |
| Board Collider | Center Y | `-0.2f` |
| Block | Scale | `(0.95, 0.35, 0.95)` |
| Block | Layer Offset | `y * 0.4f` |
| Piece Collider | Height | `0.8f` |
| Piece Collider | Center Y | `0.2f` |
| Input | Drag Height | `0.5f` |
| Level Gen | Spawn Spacing | `3f` |
