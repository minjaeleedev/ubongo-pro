# Ubongo 3D - Core Logic Requirements

## Overview

This document defines the core logic requirements for the Ubongo 3D puzzle game implemented in Unity. The game challenges players to fill a designated 3D area exactly 2 layers high using polycube pieces within a time limit.

---

## 1. 3D Polyomino Block System

### 1.1 Standard Ubongo 3D Pieces (8 Unique Shapes)

The game uses 8 standardized 3D polycube pieces. Each piece is defined by its constituent unit cubes relative to an origin point.

| Piece ID | Name       | Block Count | Definition (Origin-Normalized)                                | Color    |
|----------|------------|-------------|---------------------------------------------------------------|----------|
| 1        | Small-L    | 3           | `[(0,0,0), (1,0,0), (0,0,1)]`                                | Red      |
| 2        | Line-3     | 3           | `[(0,0,0), (1,0,0), (2,0,0)]`                                | Blue     |
| 3        | Corner-3D  | 3           | `[(0,0,0), (1,0,0), (0,1,0)]`                                | Green    |
| 4        | T-Shape    | 4           | `[(0,0,0), (1,0,0), (2,0,0), (1,0,1)]`                       | Yellow   |
| 5        | L-Shape    | 4           | `[(0,0,0), (0,0,1), (0,0,2), (1,0,2)]`                       | Purple   |
| 6        | Z-Shape    | 4           | `[(0,0,0), (1,0,0), (1,0,1), (2,0,1)]`                       | Orange   |
| 7        | Stairs-3D  | 4           | `[(0,0,0), (1,0,0), (1,1,0), (2,1,0)]`                       | Cyan     |
| 8        | Tower      | 4           | `[(0,0,0), (1,0,0), (0,1,0), (1,1,0)]`                       | Brown    |

> **참고**: Ubongo 3D는 2층 높이를 정확히 채우는 게임이므로, 모든 조각은 3-4개 블록으로 구성됩니다.
> 원본 게임에서 8블록 Cube는 사용되지 않습니다 (2층 구조에 맞지 않음).

#### Data Structure

```csharp
[Serializable]
public struct PieceDefinition
{
    public string Id;
    public string Name;
    public Vector3Int[] Blocks;       // Origin-normalized positions
    public Color DefaultColor;
    public int SymmetryGroup;         // For rotation optimization
}
```

### 1.2 Block Rotation Logic (24 3D Rotations)

All 3D objects can be rotated into 24 unique orientations (the rotation group of a cube). The system must support:

#### Rotation Matrix Set

```
24 Orientations = 6 faces × 4 rotations per face
```

**Implementation Requirements:**

1. **Rotation Matrices**: Pre-compute all 24 rotation matrices for integer coordinates
2. **Rotation Operations**:
   - `RotateX90()`: 90° rotation around X-axis
   - `RotateY90()`: 90° rotation around Y-axis  
   - `RotateZ90()`: 90° rotation around Z-axis
3. **Composite Rotations**: All 24 orientations achievable by combining basic rotations

```csharp
public static class RotationUtil
{
    // 24 rotation matrices for cube symmetry group
    public static readonly Matrix4x4[] AllRotations = GenerateAll24Rotations();
    
    public static Vector3Int[] RotatePiece(Vector3Int[] blocks, int rotationIndex)
    {
        Matrix4x4 rotation = AllRotations[rotationIndex];
        return blocks.Select(b => ApplyRotation(b, rotation)).ToArray();
    }
    
    // Returns indices of unique rotations for pieces with symmetry
    public static int[] GetUniqueRotations(Vector3Int[] normalizedBlocks);
}
```

#### Rotation Optimization

- **Symmetry Detection**: Identify pieces with rotational symmetry to reduce redundant checks
- **Unique Orientations**: 
  - Line piece: 3 unique orientations
  - Plus piece: 3 unique orientations
  - Cube piece: 1 unique orientation (fully symmetric)
  - Other pieces: Up to 24 unique orientations

### 1.3 Block Normalization (Origin-Based)

All pieces must be normalized to ensure consistent comparison and storage.

**Normalization Algorithm:**

```csharp
public static Vector3Int[] NormalizeToOrigin(Vector3Int[] blocks)
{
    // 1. Find minimum coordinates
    int minX = blocks.Min(b => b.x);
    int minY = blocks.Min(b => b.y);
    int minZ = blocks.Min(b => b.z);
    
    // 2. Translate to origin
    Vector3Int offset = new Vector3Int(minX, minY, minZ);
    Vector3Int[] normalized = blocks
        .Select(b => b - offset)
        .OrderBy(b => b.x)
        .ThenBy(b => b.y)
        .ThenBy(b => b.z)
        .ToArray();
    
    return normalized;
}
```

**Canonical Form**: For comparison purposes, the canonical form is the lexicographically smallest representation across all 24 rotations.

---

## 2. Puzzle Validation System

### 2.1 "Exactly 2 Layers High" Validation Algorithm

The core Ubongo 3D rule requires that the target area be filled exactly 2 layers high.

**Validation Requirements:**

```csharp
public class PuzzleValidator
{
    public ValidationResult ValidateSolution(GameBoard board, TargetArea target)
    {
        var result = new ValidationResult();
        
        // Check each column in the target area
        foreach (var (x, z) in target.GetColumnPositions())
        {
            int layer0 = board.IsOccupied(x, 0, z) ? 1 : 0;
            int layer1 = board.IsOccupied(x, 1, z) ? 1 : 0;
            
            // Both layers must be filled
            if (layer0 == 0 || layer1 == 0)
            {
                result.AddError(ValidationError.IncompleteFill, x, z);
            }
            
            // No blocks above layer 1 (height > 2)
            if (board.IsOccupied(x, 2, z))
            {
                result.AddError(ValidationError.ExceedsHeight, x, z);
            }
        }
        
        return result;
    }
}
```

**Validation States:**

| State                    | Description                                  |
|--------------------------|----------------------------------------------|
| `Complete`               | All columns filled exactly 2 high           |
| `Incomplete`             | Some columns not fully filled               |
| `ExceedsHeight`          | Blocks present above layer 1                |
| `OutOfBounds`            | Blocks placed outside target area           |

### 2.2 Area Complete Fill Validation

```csharp
public bool IsAreaCompletelyFilled(TargetArea target)
{
    foreach (var cell in target.GetAllCells())
    {
        if (!board.IsOccupied(cell.x, cell.y, cell.z))
            return false;
    }
    return true;
}
```

**Requirements:**
- Track fill status per layer (layer 0 and layer 1)
- Real-time progress calculation: `filledCells / totalCells`
- Event notification when fill status changes

### 2.3 Protrusion Prevention Validation

Ensures no blocks extend beyond the defined target area boundaries.

```csharp
public bool CheckNoProtrusion(PuzzlePiece piece, Vector3Int placement)
{
    foreach (var block in piece.GetWorldBlockPositions(placement))
    {
        if (!targetArea.Contains(block.x, block.z))
            return false;
            
        if (block.y < 0 || block.y >= MAX_HEIGHT)
            return false;
    }
    return true;
}
```

---

## 3. Puzzle Generation System

### 3.1 Solvable Puzzle Auto-Generation

The system must generate puzzles that are guaranteed to have at least one valid solution.

**Generation Algorithm (Reverse Construction):**

```csharp
public class PuzzleGenerator
{
    public PuzzleConfiguration GeneratePuzzle(DifficultyLevel difficulty)
    {
        // 1. Select pieces for this puzzle
        var pieces = SelectPieces(difficulty);
        
        // 2. Determine target area size based on total blocks
        int totalBlocks = pieces.Sum(p => p.BlockCount);
        var targetArea = CalculateTargetArea(totalBlocks);
        
        // 3. Find valid arrangement (guaranteed solution)
        var solution = FindValidArrangement(pieces, targetArea);
        
        // 4. Verify multiple solutions exist (optional for variation)
        if (difficulty.RequireMultipleSolutions)
        {
            EnsureMultipleSolutions(pieces, targetArea);
        }
        
        return new PuzzleConfiguration
        {
            TargetArea = targetArea,
            AvailablePieces = pieces,
            KnownSolution = solution,
            TimeLimit = difficulty.TimeLimit
        };
    }
}
```

**Constraint Satisfaction Solver:**

```csharp
public class PuzzleSolver
{
    // Backtracking solver with constraint propagation
    public List<PlacementSolution> FindAllSolutions(
        Vector3Int[] targetCells,
        List<PieceDefinition> pieces,
        int maxSolutions = 10)
    {
        var solutions = new List<PlacementSolution>();
        var board = new bool[width, height, depth];
        
        Solve(board, pieces, 0, new List<Placement>(), solutions, maxSolutions);
        
        return solutions;
    }
    
    private void Solve(
        bool[,,] board,
        List<PieceDefinition> remainingPieces,
        int pieceIndex,
        List<Placement> currentPlacements,
        List<PlacementSolution> solutions,
        int maxSolutions)
    {
        if (solutions.Count >= maxSolutions) return;
        
        if (pieceIndex >= remainingPieces.Count)
        {
            if (IsBoardComplete(board))
                solutions.Add(new PlacementSolution(currentPlacements));
            return;
        }
        
        var piece = remainingPieces[pieceIndex];
        
        foreach (var rotation in piece.GetUniqueRotations())
        {
            foreach (var position in GetValidPositions(board, rotation))
            {
                PlacePiece(board, rotation, position);
                currentPlacements.Add(new Placement(piece.Id, rotation, position));
                
                Solve(board, remainingPieces, pieceIndex + 1, 
                      currentPlacements, solutions, maxSolutions);
                
                RemovePiece(board, rotation, position);
                currentPlacements.RemoveAt(currentPlacements.Count - 1);
            }
        }
    }
}
```

### 3.2 Difficulty-Based Puzzle Card Generation

| Difficulty | Pieces | Target Area | Time   | Solutions |
|------------|--------|-------------|--------|-----------|
| Easy       | 3      | 3x2 (12)    | 90s    | 6+        |
| Medium     | 4      | 4x2 (16)    | 75s    | 4-5       |
| Hard       | 4-5    | 5x2 (20)    | 60s    | 3-4       |
| Expert     | 5-6    | 6x2 (24)    | 45s    | 2-3       |

**Piece Selection Criteria:**

```csharp
public List<PieceDefinition> SelectPiecesForDifficulty(DifficultyLevel level)
{
    var pool = GetPiecePool();
    var selected = new List<PieceDefinition>();
    int targetBlocks = level.TargetArea.TotalCells;
    int currentBlocks = 0;
    
    while (currentBlocks < targetBlocks && pool.Count > 0)
    {
        var piece = SelectWeightedRandom(pool, level.PieceWeights);
        if (currentBlocks + piece.BlockCount <= targetBlocks)
        {
            selected.Add(piece);
            currentBlocks += piece.BlockCount;
            pool.Remove(piece);
        }
    }
    
    return selected;
}
```

### 3.3 Multiple Solution Guarantee

Every generated puzzle must have 3-6 valid solutions.

**Verification Process:**

```csharp
public bool VerifyMultipleSolutions(PuzzleConfiguration puzzle, int minSolutions = 3)
{
    var solver = new PuzzleSolver();
    var solutions = solver.FindAllSolutions(
        puzzle.TargetArea.GetAllCells(),
        puzzle.AvailablePieces,
        maxSolutions: minSolutions + 1
    );
    
    return solutions.Count >= minSolutions;
}
```

---

## 4. Game Board System

### 4.1 Variable Size 3D Grid

```csharp
public class GameBoard : MonoBehaviour
{
    // Grid dimensions (configurable per level)
    private int width;   // X-axis: 3-8 units
    private int height;  // Y-axis: Always 2 for Ubongo 3D standard
    private int depth;   // Z-axis: 2-4 units
    
    private BoardCell[,,] grid;
    
    public void InitializeGrid(Vector3Int size)
    {
        width = size.x;
        height = size.y;  // Fixed at 2 for standard Ubongo 3D
        depth = size.z;
        
        grid = new BoardCell[width, height, depth];
        
        for (int x = 0; x < width; x++)
        for (int y = 0; y < height; y++)
        for (int z = 0; z < depth; z++)
        {
            grid[x, y, z] = CreateCell(x, y, z);
        }
    }
}
```

### 4.2 Target Area Definition

The target area defines which cells must be filled.

```csharp
public class TargetArea
{
    private HashSet<Vector2Int> footprint;  // XZ positions
    private int requiredHeight = 2;
    
    public bool Contains(int x, int z) => footprint.Contains(new Vector2Int(x, z));
    
    public IEnumerable<Vector3Int> GetAllCells()
    {
        foreach (var pos in footprint)
        {
            for (int y = 0; y < requiredHeight; y++)
            {
                yield return new Vector3Int(pos.x, y, pos.y);
            }
        }
    }
    
    public int TotalCells => footprint.Count * requiredHeight;
}
```

**Target Area Shapes:**
- Rectangular (standard)
- L-shaped
- T-shaped
- Custom patterns

### 4.3 Layer-Based Fill State Tracking

```csharp
public class LayerFillTracker
{
    private int[,] layer0Fill;  // Count of filled cells per column
    private int[,] layer1Fill;
    
    public FillState GetFillState()
    {
        int layer0Count = CountFilled(layer0Fill);
        int layer1Count = CountFilled(layer1Fill);
        int totalTarget = targetArea.FootprintSize;
        
        return new FillState
        {
            Layer0Progress = (float)layer0Count / totalTarget,
            Layer1Progress = (float)layer1Count / totalTarget,
            TotalProgress = (float)(layer0Count + layer1Count) / (totalTarget * 2),
            IsComplete = layer0Count == totalTarget && layer1Count == totalTarget
        };
    }
    
    public event Action<FillState> OnFillStateChanged;
}
```

---

## 5. Physics and Collision System

### 5.1 Block Placement Snap

Pieces must snap to discrete grid positions.

```csharp
public class PlacementSnapper
{
    public Vector3 CalculateSnapPosition(Vector3 worldPosition, GameBoard board)
    {
        // Convert to grid coordinates
        Vector3Int gridPos = board.WorldToGrid(worldPosition);
        
        // Clamp to valid range
        gridPos.x = Mathf.Clamp(gridPos.x, 0, board.Width - 1);
        gridPos.y = Mathf.Clamp(gridPos.y, 0, board.Height - 1);
        gridPos.z = Mathf.Clamp(gridPos.z, 0, board.Depth - 1);
        
        // Convert back to world position (centered on cell)
        return board.GridToWorld(gridPos.x, gridPos.y, gridPos.z);
    }
    
    public Vector3Int FindLowestValidPosition(PuzzlePiece piece, int gridX, int gridZ)
    {
        // Gravity-like placement: find lowest Y where piece fits
        for (int y = 0; y < board.Height; y++)
        {
            Vector3Int testPos = new Vector3Int(gridX, y, gridZ);
            if (board.CanPlacePiece(piece, testPos))
            {
                return testPos;
            }
        }
        return new Vector3Int(-1, -1, -1); // Invalid
    }
}
```

### 5.2 Collision Detection

```csharp
public class CollisionDetector
{
    public CollisionResult CheckPlacement(PuzzlePiece piece, Vector3Int position, GameBoard board)
    {
        var result = new CollisionResult();
        var blocks = piece.GetBlockPositions();
        
        foreach (var block in blocks)
        {
            Vector3Int worldPos = position + block;
            
            // Boundary check
            if (!board.IsWithinBounds(worldPos))
            {
                result.AddCollision(CollisionType.OutOfBounds, worldPos);
                continue;
            }
            
            // Occupation check
            var cell = board.GetCell(worldPos.x, worldPos.y, worldPos.z);
            if (cell != null && cell.IsOccupied)
            {
                result.AddCollision(CollisionType.Occupied, worldPos);
            }
            
            // Target area check
            if (!targetArea.Contains(worldPos.x, worldPos.z))
            {
                result.AddCollision(CollisionType.OutsideTarget, worldPos);
            }
        }
        
        return result;
    }
}
```

### 5.3 Valid Placement Confirmation

```csharp
public enum PlacementValidity
{
    Valid,
    Collision,
    OutOfBounds,
    OutsideTarget,
    HeightExceeded
}

public PlacementValidity ValidatePlacement(PuzzlePiece piece, Vector3Int position)
{
    var blocks = piece.GetBlockPositions();
    
    foreach (var block in blocks)
    {
        Vector3Int worldPos = position + block;
        
        // Check bounds
        if (worldPos.x < 0 || worldPos.x >= width ||
            worldPos.z < 0 || worldPos.z >= depth)
        {
            return PlacementValidity.OutOfBounds;
        }
        
        // Check height constraint (Ubongo 3D: exactly 2 layers)
        if (worldPos.y < 0 || worldPos.y >= MAX_HEIGHT)
        {
            return PlacementValidity.HeightExceeded;
        }
        
        // Check target area
        if (!targetArea.Contains(worldPos.x, worldPos.z))
        {
            return PlacementValidity.OutsideTarget;
        }
        
        // Check occupation
        if (grid[worldPos.x, worldPos.y, worldPos.z].IsOccupied)
        {
            return PlacementValidity.Collision;
        }
    }
    
    return PlacementValidity.Valid;
}
```

---

## 6. State Management

### 6.1 Game State Machine

```csharp
public enum GameState
{
    MainMenu,
    LevelSelect,
    Playing,
    Paused,
    PuzzleSolved,
    TimeUp,
    RoundComplete,
    GameOver
}

public class GameStateMachine
{
    private GameState currentState;
    
    private readonly Dictionary<GameState, HashSet<GameState>> validTransitions = 
        new Dictionary<GameState, HashSet<GameState>>
    {
        { GameState.MainMenu,      new HashSet<GameState> { GameState.LevelSelect, GameState.Playing } },
        { GameState.LevelSelect,   new HashSet<GameState> { GameState.MainMenu, GameState.Playing } },
        { GameState.Playing,       new HashSet<GameState> { GameState.Paused, GameState.PuzzleSolved, GameState.TimeUp } },
        { GameState.Paused,        new HashSet<GameState> { GameState.Playing, GameState.MainMenu } },
        { GameState.PuzzleSolved,  new HashSet<GameState> { GameState.RoundComplete } },
        { GameState.TimeUp,        new HashSet<GameState> { GameState.GameOver, GameState.RoundComplete } },
        { GameState.RoundComplete, new HashSet<GameState> { GameState.Playing, GameState.MainMenu } },
        { GameState.GameOver,      new HashSet<GameState> { GameState.MainMenu } }
    };
    
    public bool TryTransition(GameState newState)
    {
        if (validTransitions[currentState].Contains(newState))
        {
            var previousState = currentState;
            currentState = newState;
            OnStateChanged?.Invoke(previousState, newState);
            return true;
        }
        return false;
    }
    
    public event Action<GameState, GameState> OnStateChanged;
}
```

### 6.2 Round Progression Logic

```csharp
public class RoundManager
{
    private int currentRound = 1;
    private const int RoundsPerGame = 9;  // Standard Ubongo game
    
    public void StartRound(int roundNumber)
    {
        currentRound = roundNumber;
        
        // Generate or load puzzle for this round
        var puzzle = puzzleProvider.GetPuzzleForRound(roundNumber);
        
        // Initialize board
        gameBoard.SetupPuzzle(puzzle);
        
        // Spawn pieces
        pieceSpawner.SpawnPieces(puzzle.AvailablePieces);
        
        // Start timer
        timer.Start(puzzle.TimeLimit);
        
        OnRoundStarted?.Invoke(roundNumber);
    }
    
    public void OnPuzzleSolved(float remainingTime)
    {
        // Calculate finish order (multiplayer)
        int finishPosition = multiplayer.RegisterCompletion(playerId, remainingTime);
        
        // Award gems based on position
        int gems = CalculateGemReward(finishPosition);
        scoreManager.AddGems(gems);
        
        // Check if more rounds remain
        if (currentRound < RoundsPerGame)
        {
            AdvanceToNextRound();
        }
        else
        {
            EndGame();
        }
    }
}
```

### 6.3 Score Calculation (Gem System)

The Ubongo game uses a gem-based scoring system.

```csharp
public class GemScoreSystem
{
    // Gems awarded based on finish position
    private static readonly int[] GemRewards = { 4, 3, 2, 1 }; // 1st, 2nd, 3rd, 4th

    public int CalculateReward(int finishPosition, int totalPlayers)
    {
        if (finishPosition < 0 || finishPosition >= GemRewards.Length)
            return 0;

        return GemRewards[finishPosition];
    }

    // Bonus gems for time remaining
    public int CalculateTimeBonus(float remainingTime, float totalTime)
    {
        float ratio = remainingTime / totalTime;
        if (ratio > 0.5f) return 2;  // > 50% time remaining
        if (ratio > 0.25f) return 1; // > 25% time remaining
        return 0;
    }
}
```

### 6.4 Gem Pool System (유한 보석 풀)

**원본 Ubongo 3D 규칙**: 보드게임에서는 천 주머니에 담긴 유한한 보석을 랜덤으로 뽑습니다.
총 58개의 보석으로 구성됩니다.

#### 6.4.1 보석 풀 구성

| 보석 유형 | 색상 | 포인트 | 초기 수량 |
|----------|------|--------|----------|
| Ruby | 빨강 | 4점 | 12개 |
| Sapphire | 파랑 | 3점 | 12개 |
| Emerald | 초록 | 2점 | 16개 |
| Amber | 호박 | 1점 | 18개 |
| **총합** | - | - | **58개** |

#### 6.4.2 데이터 구조

```csharp
[Serializable]
public struct GemPool
{
    public int Rubies;     // 초기: 12
    public int Sapphires;  // 초기: 12
    public int Emeralds;   // 초기: 16
    public int Ambers;     // 초기: 18

    public static GemPool CreateDefault() => new GemPool
    {
        Rubies = 12,
        Sapphires = 12,
        Emeralds = 16,
        Ambers = 18
    };

    public int TotalRemaining => Rubies + Sapphires + Emeralds + Ambers;
    public bool IsEmpty => TotalRemaining == 0;
}

public enum GemType
{
    Ruby,      // 4점
    Sapphire,  // 3점
    Emerald,   // 2점
    Amber      // 1점
}
```

#### 6.4.3 랜덤 보석 추출 로직

```csharp
public class GemPoolManager
{
    private GemPool pool;

    public GemPoolManager()
    {
        pool = GemPool.CreateDefault();
    }

    /// <summary>
    /// 풀에서 랜덤 보석을 추출합니다.
    /// 가중치 기반 랜덤 선택 (남은 보석 수량에 비례)
    /// </summary>
    public GemType? DrawRandomGem()
    {
        if (pool.IsEmpty) return null;

        int total = pool.TotalRemaining;
        int roll = Random.Range(0, total);

        // 가중치 기반 선택
        if (roll < pool.Rubies)
        {
            pool.Rubies--;
            return GemType.Ruby;
        }
        roll -= pool.Rubies;

        if (roll < pool.Sapphires)
        {
            pool.Sapphires--;
            return GemType.Sapphire;
        }
        roll -= pool.Sapphires;

        if (roll < pool.Emeralds)
        {
            pool.Emeralds--;
            return GemType.Emerald;
        }

        pool.Ambers--;
        return GemType.Amber;
    }

    /// <summary>
    /// 특정 유형의 보석을 추출합니다 (1등/2등 고정 보석용)
    /// </summary>
    public bool DrawSpecificGem(GemType type)
    {
        switch (type)
        {
            case GemType.Ruby:
                if (pool.Rubies <= 0) return false;
                pool.Rubies--;
                return true;
            case GemType.Sapphire:
                if (pool.Sapphires <= 0) return false;
                pool.Sapphires--;
                return true;
            case GemType.Emerald:
                if (pool.Emeralds <= 0) return false;
                pool.Emeralds--;
                return true;
            case GemType.Amber:
                if (pool.Ambers <= 0) return false;
                pool.Ambers--;
                return true;
            default:
                return false;
        }
    }

    public GemPool GetCurrentPool() => pool;
    public void Reset() => pool = GemPool.CreateDefault();
}
```

#### 6.4.4 보석 소진 시 처리

| 상황 | 처리 방식 |
|------|----------|
| 1등 고정 보석(사파이어) 소진 | 다음 높은 가치 보석으로 대체 (에메랄드) |
| 2등 고정 보석(앰버) 소진 | 보석 없이 진행 또는 최저 가치 보석 |
| 랜덤 보석 모두 소진 | 해당 라운드부터 보석 미지급 |
| 전체 보석 소진 | 남은 라운드는 점수 변동 없음 |

#### 6.4.5 구현 우선순위

> **참고**: 유한 보석 풀은 원본 보드게임의 규칙이지만, 디지털 게임에서는
> 무한 보석 모드를 기본으로 사용하고, 유한 모드를 옵션으로 제공할 수 있습니다.

| 모드 | 설명 | 구현 우선순위 |
|------|------|--------------|
| Infinite (무한) | 보석 소진 없음, 항상 지급 | HIGH (기본) |
| Finite (유한) | 58개 보석 풀, 원본 규칙 | MEDIUM (옵션) |
| Classic | 유한 + 엄격한 원본 규칙 | LOW (옵션)
```

---

## 7. Multiplayer Synchronization (Architecture Design)

### 7.1 State Synchronization

```csharp
public interface INetworkState
{
    // Synchronized data
    int RoundNumber { get; }
    float RemainingTime { get; }
    PlayerState[] PlayerStates { get; }
    PuzzleConfiguration CurrentPuzzle { get; }
}

public class PlayerState
{
    public int PlayerId;
    public bool HasCompleted;
    public float CompletionTime;
    public int GemCount;
    public PiecePlacement[] CurrentPlacements;
}

public class NetworkSyncManager
{
    private const float SyncInterval = 0.1f; // 100ms
    
    public void SyncPlayerPlacements(int playerId, List<PiecePlacement> placements)
    {
        // Send delta updates only
        var delta = CalculateDelta(previousState, placements);
        networkTransport.Send(new PlacementUpdateMessage(playerId, delta));
    }
    
    public void OnReceiveUpdate(PlacementUpdateMessage message)
    {
        // Apply to other players' boards (visual only)
        opponentBoardView.UpdatePlacements(message.PlayerId, message.Delta);
    }
}
```

### 7.2 Completion Order Determination

```csharp
public class CompletionOrderManager
{
    private readonly SortedList<float, int> completionOrder = new SortedList<float, int>();
    private readonly object lockObject = new object();
    
    public int RegisterCompletion(int playerId, float serverTimestamp)
    {
        lock (lockObject)
        {
            // Use server timestamp to prevent cheating
            completionOrder.Add(serverTimestamp, playerId);
            return completionOrder.IndexOfKey(serverTimestamp) + 1; // 1-based position
        }
    }
    
    public void BroadcastResults()
    {
        var results = completionOrder.Select((kvp, index) => 
            new CompletionResult(kvp.Value, index + 1, kvp.Key)).ToList();
        
        networkTransport.Broadcast(new RoundResultsMessage(results));
    }
}
```

### 7.3 Latency Handling

```csharp
public class LatencyCompensation
{
    private float estimatedLatency;
    private readonly Queue<float> latencySamples = new Queue<float>();
    
    public void UpdateLatency(float pingTime)
    {
        latencySamples.Enqueue(pingTime);
        if (latencySamples.Count > 10)
            latencySamples.Dequeue();
            
        estimatedLatency = latencySamples.Average();
    }
    
    public float AdjustTimestamp(float localTimestamp)
    {
        // Compensate for network latency
        return localTimestamp - (estimatedLatency / 2);
    }
    
    // Reconciliation for late updates
    public void ReconcileState(ServerState serverState, float serverTimestamp)
    {
        float localServerTime = ConvertToLocalTime(serverTimestamp);
        
        // Replay local inputs from localServerTime to now
        foreach (var input in inputBuffer.GetInputsSince(localServerTime))
        {
            ApplyInput(input);
        }
    }
}
```

---

## 8. Current Code Analysis and Refactoring Requirements

### 8.1 Current Implementation Status

| Component         | File                  | Status    | Issues                                    |
|-------------------|-----------------------|-----------|-------------------------------------------|
| GameManager       | GameManager.cs        | Partial   | Missing state machine, simple timer logic |
| GameBoard         | GameBoard.cs          | Partial   | No target area flexibility, basic grid    |
| BoardCell         | BoardCell.cs          | Basic     | Missing layer tracking                    |
| PuzzlePiece       | PuzzlePiece.cs        | Partial   | No rotation matrices, manual rotations    |
| LevelGenerator    | LevelGenerator.cs     | Basic     | No solver, random piece selection         |
| UIManager         | UIManager.cs          | Basic     | Standard UI, no multiplayer support       |

### 8.2 Refactoring Requirements

#### 8.2.1 PuzzlePiece.cs Refactoring

**Current Issues:**
- Manual rotation using `transform.Rotate()` instead of discrete 90-degree rotations
- No proper rotation matrix implementation
- Block positions calculated at runtime via quaternion multiplication

**Required Changes:**

```csharp
// Current (problematic)
public List<Vector3Int> GetBlockPositions()
{
    List<Vector3Int> rotatedPositions = new List<Vector3Int>();
    foreach (Vector3Int originalPos in blockPositions)
    {
        Vector3 rotated = transform.rotation * originalPos;  // Floating-point errors
        ...
    }
}

// Required (discrete rotations)
public List<Vector3Int> GetBlockPositions()
{
    return _cachedRotatedPositions[_currentRotationIndex];
}

public void RotateX() => SetRotation((_currentRotationIndex + 1) % 24);
public void RotateY() => SetRotation((_currentRotationIndex + 6) % 24);
public void RotateZ() => SetRotation((_currentRotationIndex + 12) % 24);
```

#### 8.2.2 GameBoard.cs Refactoring

**Current Issues:**
- Fixed target area (bottom layer only)
- No support for custom puzzle shapes
- Height is `height` but should be fixed at 2 for Ubongo 3D

**Required Changes:**

```csharp
// Add target area support
private TargetArea targetArea;

public void SetTargetArea(TargetArea area)
{
    targetArea = area;
    UpdateVisuals();
}

// Fix height to 2 for Ubongo 3D
private const int UBONGO_HEIGHT = 2;

// Add layer-based tracking
private FillState CalculateFillState()
{
    int layer0Filled = 0, layer1Filled = 0;
    foreach (var pos in targetArea.GetFootprint())
    {
        if (grid[pos.x, 0, pos.y]?.IsOccupied == true) layer0Filled++;
        if (grid[pos.x, 1, pos.y]?.IsOccupied == true) layer1Filled++;
    }
    // ...
}
```

#### 8.2.3 LevelGenerator.cs Refactoring

**Current Issues:**
- Random piece selection without solvability guarantee
- No puzzle solver integration
- Shapes don't match official Ubongo 3D pieces

**Required Changes:**

```csharp
// Add puzzle solver
private PuzzleSolver solver;

public PuzzleConfiguration GenerateSolvablePuzzle(DifficultyLevel difficulty)
{
    PuzzleConfiguration puzzle;
    int attempts = 0;
    
    do
    {
        puzzle = GenerateCandidate(difficulty);
        attempts++;
    } 
    while (!solver.HasSolution(puzzle) && attempts < MAX_ATTEMPTS);
    
    if (attempts >= MAX_ATTEMPTS)
        throw new PuzzleGenerationException("Could not generate solvable puzzle");
        
    return puzzle;
}
```

#### 8.2.4 GameManager.cs Refactoring

**Current Issues:**
- Simple enum-based state, no proper state machine
- Missing round/game progression logic
- No gem scoring system

**Required Changes:**

```csharp
// Replace simple state enum with state machine
private GameStateMachine stateMachine;

// Add round management
private RoundManager roundManager;

// Add gem scoring
private GemScoreSystem gemScoring;

// Proper state transitions
public void OnPuzzleSolved()
{
    if (stateMachine.CurrentState != GameState.Playing) return;
    
    stateMachine.TryTransition(GameState.PuzzleSolved);
    
    int gems = gemScoring.CalculateReward(GetFinishPosition(), GetPlayerCount());
    gems += gemScoring.CalculateTimeBonus(remainingTime, totalTime);
    
    playerScore.AddGems(gems);
    roundManager.OnPuzzleSolved(remainingTime);
}
```

### 8.3 New Components Required

| Component              | Purpose                                     | Priority |
|------------------------|---------------------------------------------|----------|
| `PuzzleSolver.cs`      | Constraint satisfaction solver              | High     |
| `RotationUtil.cs`      | 24 rotation matrices and utilities          | High     |
| `TargetArea.cs`        | Flexible target area definition             | High     |
| `PuzzleValidator.cs`   | Solution validation logic                   | High     |
| `GameStateMachine.cs`  | Proper state management                     | Medium   |
| `RoundManager.cs`      | Round progression logic                     | Medium   |
| `GemScoreSystem.cs`    | Scoring calculations                        | Medium   |
| `NetworkSyncManager.cs`| Multiplayer state sync                      | Low      |
| `LatencyCompensation.cs`| Network latency handling                   | Low      |

### 8.4 Architecture Improvements

**Current Structure:**
```
Assets/Scripts/
├── Core/
├── GameBoard/
│   ├── BoardCell.cs
│   └── GameBoard.cs
├── Managers/
│   ├── GameManager.cs
│   └── LevelGenerator.cs
├── Pieces/
│   └── PuzzlePiece.cs
└── UI/
    └── UIManager.cs
```

**Recommended Structure:**
```
Assets/Scripts/
├── Core/
│   ├── Pieces/
│   │   ├── PieceDefinition.cs
│   │   ├── PieceCatalog.cs
│   │   └── RotationUtil.cs
│   ├── Board/
│   │   ├── GameBoard.cs
│   │   ├── BoardCell.cs
│   │   └── TargetArea.cs
│   ├── Puzzle/
│   │   ├── PuzzleSolver.cs
│   │   ├── PuzzleGenerator.cs
│   │   └── PuzzleValidator.cs
│   └── State/
│       ├── GameStateMachine.cs
│       └── RoundManager.cs
├── Gameplay/
│   ├── PuzzlePiece.cs (MonoBehaviour)
│   ├── PieceController.cs
│   └── PlacementSnapper.cs
├── Scoring/
│   └── GemScoreSystem.cs
├── Network/ (future)
│   ├── NetworkSyncManager.cs
│   └── LatencyCompensation.cs
├── Managers/
│   └── GameManager.cs
└── UI/
    ├── UIManager.cs
    └── Components/
```

---

## Appendix A: 24 Rotation Matrices

```csharp
// Identity and 23 other rotations of a cube
public static readonly int[,][] RotationMatrices = new int[24][,]
{
    // Face 1 (Identity)
    new int[,] { {1,0,0}, {0,1,0}, {0,0,1} },   // 0: No rotation
    new int[,] { {1,0,0}, {0,0,-1}, {0,1,0} },  // 1: X+90
    new int[,] { {1,0,0}, {0,-1,0}, {0,0,-1} }, // 2: X+180
    new int[,] { {1,0,0}, {0,0,1}, {0,-1,0} },  // 3: X+270
    
    // Face 2 (Y+90)
    new int[,] { {0,0,1}, {0,1,0}, {-1,0,0} },  // 4: Y+90
    new int[,] { {0,1,0}, {0,0,1}, {1,0,0} },   // 5: Y+90, X+90
    // ... (20 more matrices)
};
```

## Appendix B: Standard Ubongo 3D Piece Visuals

> 모든 조각은 2층 높이 퍼즐에 맞게 3-4개 블록으로 구성됩니다.

```
Small-L (3 blocks):       Line-3 (3 blocks):        Corner-3D (3 blocks):
  Layer 0:                  Layer 0:                  Layer 0: [X][X]
  [X][X]                    [X][X][X]                 Layer 1: [X]
  [X]

T-Shape (4 blocks):       L-Shape (4 blocks):       Z-Shape (4 blocks):
  Layer 0:                  Layer 0:                  Layer 0:
  [X][X][X]                 [X]                       [X][X]
     [X]                    [X]                          [X][X]
                            [X][X]

Stairs-3D (4 blocks):     Tower (4 blocks):
  Layer 0: [X][X]           Layer 0: [X][X]
  Layer 1:    [X][X]        Layer 1: [X][X]
```

### 블록 수 요약
- 3블록 조각: Small-L, Line-3, Corner-3D
- 4블록 조각: T-Shape, L-Shape, Z-Shape, Stairs-3D, Tower

### 난이도별 조각 조합 예시
- Easy (12블록 = 3x2x2): 3블록×2 + 3블록×2 또는 4블록×3
- Medium (16블록 = 4x2x2): 4블록×4
- Hard (20블록 = 5x2x2): 4블록×5
