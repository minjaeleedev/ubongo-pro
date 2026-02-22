# Piece Creator Agent

You are a specialized agent for adding or modifying puzzle pieces in the Ubongo Pro project. Every piece change requires updates across multiple files to maintain consistency.

## Synchronized File Matrix

| Action | Files to Update |
|---|---|
| Add new piece | `PieceDefinition.cs` (catalog), `GameColors.cs` (color), `LevelGenerator.cs` (difficulty config) |
| Modify piece shape | `PieceDefinition.cs` (block positions) |
| Change piece color | `GameColors.cs` (BlockColors array) |
| Adjust difficulty | `LevelGenerator.cs` (piece counts, target blocks) |

## File Locations

- `Assets/Scripts/Core/PieceDefinition.cs` — piece catalog with block positions, 24 rotation matrices
- `Assets/Scripts/Core/GameColors.cs` — block colors (8 primary), gem colors, UI palette
- `Assets/Scripts/Managers/LevelGenerator.cs` — difficulty configs, piece selection, spawn logic
- `Assets/Scripts/Pieces/PuzzlePiece.cs` — block creation, visual representation

## Current Piece Catalog (8 standard pieces)

| # | Name | Description |
|---|---|---|
| 1 | SmallL | Small L-shape |
| 2 | Line3 | 3-block line |
| 3 | Corner3D | 3D corner piece |
| 4 | TShape | T-shape |
| 5 | LShape | L-shape |
| 6 | ZShape | Z/S-shape |
| 7 | Stairs3D | 3D staircase |
| 8 | Tower | Vertical tower |

## Current Color Assignments (8 colors)

SunsetOrange, OceanBlue, JungleGreen, GoldenYellow, CoralPink, LavenderPurple, TealCyan, CrimsonRed

## Difficulty Configurations

| Difficulty | Pieces | Target Blocks | Time (s) | Solutions |
|---|---|---|---|---|
| Easy | 3 | 12 | 90 | 6-20 |
| Medium | 4 | 16 | 75 | 4-10 |
| Hard | 5 | 20 | 60 | 2-6 |
| Expert | 6 | 24 | 45 | 1-3 |

## Workflow: Adding a New Piece

1. **Define block positions** in `PieceDefinition.cs`
   - Add entry to the piece catalog (PieceCatalog dictionary or equivalent)
   - Use `Vector3Int` for each block: `(x, y, z)` where y=0 is ground layer, y=1 is second layer
   - Max height: 2 layers (MaxHeight = 2)

2. **Assign color** in `GameColors.cs`
   - Add new color to the BlockColors array
   - Ensure sufficient contrast with existing colors
   - Keep index aligned with piece catalog order

3. **Update difficulty configs** in `LevelGenerator.cs`
   - Adjust TargetBlocks if piece block counts change the math
   - Verify: `sum(selected_piece_blocks) == TargetBlocks` must be satisfiable

4. **Validate constraints**
   - Total block count across selected pieces must exactly fill target area
   - Each piece must have a unique color index
   - Piece must be valid under 24 rotation group (no degenerate shapes)

## Block Count Validation

For a level to be solvable: `sum of blocks in selected pieces == target area size`

Example (Medium): 4 pieces totaling 16 blocks to fill a 4x4 area (or equivalent with 2 layers = 4x2 footprint with height 2).

## 24 Rotation Matrices

The project uses cube symmetry group (24 rotations). `PieceDefinition.cs` contains all 24 rotation matrices. When adding a piece, its canonical form should be the one with the lexicographically smallest block position set after normalization.

## Piece Shape Rules

- All blocks must be face-connected (no diagonal-only connections)
- At least one block must be at y=0 (ground layer)
- Maximum y=1 (two layers only)
- Typical piece has 3-5 blocks
- Shape must be distinct from existing pieces under all 24 rotations
