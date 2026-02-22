---
description: "런타임 코드와 Scene 파일 간 시각 파라미터 동기화 상태를 진단합니다"
---

# Check Sync Command

Read and compare visual parameters across all synchronized files. Report any mismatches.

## Steps

1. **Read all source files**:
   - `Assets/Scripts/Managers/GameManager.cs` — find `SetupCamera()` method
   - `Assets/Editor/CameraSetupTool.cs` — find camera setup values
   - `Assets/Scenes/MainScene.unity` — find Main Camera Transform and Camera components
   - `Assets/Scripts/GameBoard/GameBoard.cs` — find `CreateDefaultCell()` and `AddBoardCollider()`
   - `Assets/Scripts/Pieces/PuzzlePiece.cs` — find `CreateBlock()` and `CalculateBounds()`

2. **Extract and compare camera values** across 3 sources:
   - Position (x, y, z)
   - Rotation euler (x, y, z) — for scene file, convert quaternion back to euler to verify
   - Orthographic size
   - Report: MATCH or MISMATCH with specific values from each source

3. **Verify collider consistency**:
   - Cell height (CreateDefaultCell scale.y) vs board collider size.y — collider should be >= cell height
   - Block layer offset vs block height — gap should be >= 0.05f for z-fighting prevention
   - Piece collider height vs max layers * layer offset — should cover all layers

4. **Output format**:

```
## Sync Check Report

### Camera Parameters
| Parameter | GameManager | CameraSetupTool | MainScene.unity | Status |
|---|---|---|---|---|
| Position | (0, 10, -7) | (0, 10, -7) | (0, 10, -7) | ✅ MATCH |
| Rotation | (35, 0, 0) | (35, 0, 0) | (35, 0, 0) | ✅ MATCH |
| OrthoSize | 5 | 5 | 5 | ✅ MATCH |

### Collider Consistency
| Check | Value | Expected | Status |
|---|---|---|---|
| Z-fighting gap | 0.05f | >= 0.05f | ✅ OK |
| Board collider covers cells | 0.4f >= 0.3f | Yes | ✅ OK |
| Piece collider covers layers | 0.8f >= 2*0.4f | Yes | ✅ OK |

### Issues Found
- (list any mismatches with recommended fix)
```

5. **If mismatches found**, suggest which file(s) to update and the correct values.
