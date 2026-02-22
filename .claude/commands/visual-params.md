---
description: "현재 시각 파라미터(카메라, 셀, 블록, 콜라이더)를 한눈에 요약 출력합니다"
---

# Visual Params Command

Read all visual parameter source files and output a comprehensive summary table.

## Steps

1. **Read source files**:
   - `Assets/Scripts/Managers/GameManager.cs` — `SetupCamera()` method
   - `Assets/Scripts/GameBoard/GameBoard.cs` — serialized fields, `CreateDefaultCell()`, `AddBoardCollider()`
   - `Assets/Scripts/Pieces/PuzzlePiece.cs` — `CreateBlock()`, `CalculateBounds()`
   - `Assets/Scripts/Input/InputManager.cs` — `dragHeight`
   - `Assets/Scripts/Managers/LevelGenerator.cs` — `spawnSpacing`, spawn area position

2. **Output format**:

```
## Visual Parameters Summary

### Camera
| Parameter | Value |
|---|---|
| Position | (x, y, z) |
| Rotation (Euler) | (x, y, z) |
| Orthographic Size | n |
| Near/Far Clip | n / n |
| Background Color | (r, g, b) |

### Board Cells
| Parameter | Value |
|---|---|
| Cell Size | n |
| Cell Spacing | n |
| Cell Scale | (x, y, z) |
| Total Cell Pitch | cellSize + cellSpacing |

### Blocks
| Parameter | Value |
|---|---|
| Block Scale | (x, y, z) |
| Block Height | n |
| Layer Y Offset | formula (value per layer) |
| Z-Fighting Gap | n |

### Colliders
| Parameter | Board | Piece |
|---|---|---|
| Size Y (height) | n | n |
| Center Y | n | n |
| Type | BoxCollider | BoxCollider |

### Input & Spawning
| Parameter | Value |
|---|---|
| Drag Height | n |
| Spawn Spacing | n |
| Max Height (layers) | n |

### Scene File Sync
| Source | Position | Rotation | OrthoSize | Status |
|---|---|---|---|---|
| GameManager.cs | ... | ... | ... | — |
| CameraSetupTool.cs | ... | ... | ... | ✅/❌ |
| MainScene.unity | ... | ... | ... | ✅/❌ |
```

3. **Highlight any anomalies** (mismatched values, suspicious gaps, etc.) at the bottom.
