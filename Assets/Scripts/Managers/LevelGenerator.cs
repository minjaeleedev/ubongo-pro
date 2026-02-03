using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Ubongo.Core;

namespace Ubongo
{
    /// <summary>
    /// Difficulty level for puzzle generation.
    /// </summary>
    public enum DifficultyLevel
    {
        Easy,
        Medium,
        Hard,
        Expert
    }

    /// <summary>
    /// Configuration settings for each difficulty level.
    /// </summary>
    [System.Serializable]
    public class DifficultyConfig
    {
        public DifficultyLevel Level;
        public int MinPieces;
        public int MaxPieces;
        public int TargetBlocks;
        public float TimeLimit;
        public int MinSolutions;
        public int MaxSolutions;

        public static DifficultyConfig GetConfig(DifficultyLevel level)
        {
            return level switch
            {
                DifficultyLevel.Easy => new DifficultyConfig
                {
                    Level = DifficultyLevel.Easy,
                    MinPieces = 3,
                    MaxPieces = 3,
                    TargetBlocks = 12,
                    TimeLimit = 90f,
                    MinSolutions = 6,
                    MaxSolutions = 20
                },
                DifficultyLevel.Medium => new DifficultyConfig
                {
                    Level = DifficultyLevel.Medium,
                    MinPieces = 4,
                    MaxPieces = 4,
                    TargetBlocks = 16,
                    TimeLimit = 75f,
                    MinSolutions = 4,
                    MaxSolutions = 10
                },
                DifficultyLevel.Hard => new DifficultyConfig
                {
                    Level = DifficultyLevel.Hard,
                    MinPieces = 4,
                    MaxPieces = 5,
                    TargetBlocks = 20,
                    TimeLimit = 60f,
                    MinSolutions = 3,
                    MaxSolutions = 6
                },
                DifficultyLevel.Expert => new DifficultyConfig
                {
                    Level = DifficultyLevel.Expert,
                    MinPieces = 5,
                    MaxPieces = 6,
                    TargetBlocks = 24,
                    TimeLimit = 45f,
                    MinSolutions = 2,
                    MaxSolutions = 4
                },
                _ => GetConfig(DifficultyLevel.Easy)
            };
        }
    }

    /// <summary>
    /// Data for a generated puzzle level.
    /// </summary>
    [System.Serializable]
    public class LevelData
    {
        public int LevelNumber;
        public DifficultyLevel Difficulty;
        public float TimeLimit;
        public List<PieceDefinition> Pieces;
        public Vector3Int BoardSize;
        public TargetArea TargetArea;
    }

    /// <summary>
    /// Generates solvable Ubongo 3D puzzles with the 8 standard pieces.
    /// </summary>
    public class LevelGenerator : MonoBehaviour
    {
        private const int MaxHeight = 2;

        [Header("Piece Spawn Settings")]
        [SerializeField] private GameObject piecePrefab;
        [SerializeField] private Transform pieceSpawnArea;
        [SerializeField] private float spawnSpacing = 3f;

        [Header("Generation Settings")]
        [SerializeField] private int maxGenerationAttempts = 100;

        private List<GameObject> currentPieces = new List<GameObject>();
        private PieceDefinition[] allPieces;

        private void Awake()
        {
            InitializePieces();
        }

        private void InitializePieces()
        {
            allPieces = PieceCatalog.GetAllPieces();
        }

        /// <summary>
        /// Generates a level with the specified difficulty.
        /// </summary>
        public void GenerateLevel(int levelNumber)
        {
            ClearCurrentPieces();

            DifficultyLevel difficulty = GetDifficultyForLevel(levelNumber);
            LevelData levelData = GenerateSolvablePuzzle(difficulty, levelNumber);

            SpawnPieces(levelData.Pieces);
        }

        /// <summary>
        /// Generates a solvable puzzle configuration.
        /// </summary>
        public LevelData GenerateSolvablePuzzle(DifficultyLevel difficulty, int levelNumber = 1)
        {
            DifficultyConfig config = DifficultyConfig.GetConfig(difficulty);
            List<PieceDefinition> selectedPieces = null;
            TargetArea targetArea = null;
            int totalBlocks = 0;

            for (int attempt = 0; attempt < maxGenerationAttempts; attempt++)
            {
                selectedPieces = SelectPiecesForDifficulty(config);
                totalBlocks = selectedPieces.Sum(p => p.BlockCount);

                // Calculate target area dimensions (must fit 2 layers exactly)
                int footprintSize = totalBlocks / MaxHeight;
                targetArea = CalculateTargetArea(footprintSize, difficulty);

                if (targetArea.TotalCells == totalBlocks)
                {
                    // Verify the puzzle has at least one solution
                    if (HasSolution(selectedPieces, targetArea))
                    {
                        break;
                    }
                }
            }

            Vector3Int boardSize = CalculateBoardSize(targetArea);

            return new LevelData
            {
                LevelNumber = levelNumber,
                Difficulty = difficulty,
                TimeLimit = config.TimeLimit,
                Pieces = selectedPieces,
                BoardSize = boardSize,
                TargetArea = targetArea
            };
        }

        /// <summary>
        /// Selects pieces based on difficulty configuration.
        /// </summary>
        private List<PieceDefinition> SelectPiecesForDifficulty(DifficultyConfig config)
        {
            var selected = new List<PieceDefinition>();
            var availablePieces = new List<PieceDefinition>(allPieces);
            int currentBlocks = 0;
            int targetBlocks = config.TargetBlocks;

            // Shuffle available pieces
            ShuffleList(availablePieces);

            // Select pieces to reach target block count
            foreach (var piece in availablePieces)
            {
                if (currentBlocks + piece.BlockCount <= targetBlocks)
                {
                    selected.Add(piece);
                    currentBlocks += piece.BlockCount;

                    if (currentBlocks == targetBlocks)
                    {
                        break;
                    }

                    if (selected.Count >= config.MaxPieces)
                    {
                        break;
                    }
                }
            }

            // If we haven't reached target blocks, try to fill with remaining pieces
            if (currentBlocks < targetBlocks && selected.Count < config.MaxPieces)
            {
                foreach (var piece in availablePieces)
                {
                    if (!selected.Contains(piece) && currentBlocks + piece.BlockCount <= targetBlocks)
                    {
                        selected.Add(piece);
                        currentBlocks += piece.BlockCount;

                        if (currentBlocks == targetBlocks || selected.Count >= config.MaxPieces)
                        {
                            break;
                        }
                    }
                }
            }

            return selected;
        }

        /// <summary>
        /// Calculates the target area based on footprint size and difficulty.
        /// </summary>
        private TargetArea CalculateTargetArea(int footprintSize, DifficultyLevel difficulty)
        {
            // Determine width and depth based on footprint size
            // For Ubongo 3D, standard depth is 2
            int depth = 2;
            int width = Mathf.CeilToInt((float)footprintSize / depth);

            // Adjust if necessary to match exact footprint
            if (width * depth != footprintSize)
            {
                // Try different configurations
                for (int d = 2; d <= 4; d++)
                {
                    if (footprintSize % d == 0)
                    {
                        depth = d;
                        width = footprintSize / d;
                        break;
                    }
                }
            }

            return difficulty switch
            {
                DifficultyLevel.Easy => TargetArea.CreateRectangular(width, depth),
                DifficultyLevel.Medium => TargetArea.CreateRectangular(width, depth),
                DifficultyLevel.Hard => CreateVariedTargetArea(footprintSize),
                DifficultyLevel.Expert => CreateVariedTargetArea(footprintSize),
                _ => TargetArea.CreateRectangular(width, depth)
            };
        }

        /// <summary>
        /// Creates varied target areas for harder difficulties.
        /// </summary>
        private TargetArea CreateVariedTargetArea(int footprintSize)
        {
            int shapeType = Random.Range(0, 3);

            return shapeType switch
            {
                0 => CreateRectangularArea(footprintSize),
                1 => CreateLShapedArea(footprintSize),
                2 => CreateTShapedArea(footprintSize),
                _ => CreateRectangularArea(footprintSize)
            };
        }

        private TargetArea CreateRectangularArea(int footprintSize)
        {
            int depth = 2;
            int width = Mathf.CeilToInt((float)footprintSize / depth);

            // Adjust to match exact footprint
            while (width * depth < footprintSize && depth < 4)
            {
                depth++;
                width = Mathf.CeilToInt((float)footprintSize / depth);
            }

            return TargetArea.CreateRectangular(width, depth);
        }

        private TargetArea CreateLShapedArea(int footprintSize)
        {
            // Simple L-shape calculation
            int baseWidth = Mathf.CeilToInt(Mathf.Sqrt(footprintSize * 1.5f));
            int baseDepth = Mathf.CeilToInt((float)footprintSize / baseWidth);

            int cutWidth = baseWidth / 3;
            int cutDepth = baseDepth / 2;

            // Verify footprint size
            int actualFootprint = (baseWidth * baseDepth) - (cutWidth * cutDepth);
            if (actualFootprint != footprintSize)
            {
                return CreateRectangularArea(footprintSize);
            }

            return TargetArea.CreateLShaped(baseWidth, baseDepth, cutWidth, cutDepth);
        }

        private TargetArea CreateTShapedArea(int footprintSize)
        {
            // Simple T-shape calculation
            int topWidth = Mathf.CeilToInt(Mathf.Sqrt(footprintSize));
            int topDepth = 1;
            int stemWidth = topWidth / 3;
            if (stemWidth < 1) stemWidth = 1;
            int stemDepth = (footprintSize - (topWidth * topDepth)) / stemWidth;

            int actualFootprint = (topWidth * topDepth) + (stemWidth * stemDepth);
            if (actualFootprint != footprintSize)
            {
                return CreateRectangularArea(footprintSize);
            }

            return TargetArea.CreateTShaped(topWidth, topDepth, stemWidth, stemDepth);
        }

        /// <summary>
        /// Simple check if a puzzle configuration has at least one solution.
        /// Uses a basic backtracking approach.
        /// </summary>
        private bool HasSolution(List<PieceDefinition> pieces, TargetArea targetArea)
        {
            if (pieces == null || pieces.Count == 0 || targetArea == null)
            {
                return false;
            }

            int totalPieceBlocks = pieces.Sum(p => p.BlockCount);
            if (totalPieceBlocks != targetArea.TotalCells)
            {
                return false;
            }

            // Get all target cells
            var targetCells = targetArea.GetAllCells().ToList();
            var board = new bool[targetArea.Width + 4, MaxHeight, targetArea.Depth + 4];

            return TrySolve(board, pieces, 0, targetArea);
        }

        private bool TrySolve(bool[,,] board, List<PieceDefinition> pieces, int pieceIndex, TargetArea targetArea)
        {
            if (pieceIndex >= pieces.Count)
            {
                // Check if all target cells are filled
                foreach (var cell in targetArea.GetAllCells())
                {
                    if (!board[cell.x, cell.y, cell.z])
                    {
                        return false;
                    }
                }
                return true;
            }

            var piece = pieces[pieceIndex];
            var uniqueRotations = RotationUtil.GetUniqueRotations(piece.Blocks);

            foreach (int rotationIndex in uniqueRotations)
            {
                var rotatedBlocks = RotationUtil.RotatePiece(piece.Blocks, rotationIndex);

                foreach (var column in targetArea.GetColumnPositions())
                {
                    for (int y = 0; y < MaxHeight; y++)
                    {
                        Vector3Int position = new Vector3Int(column.x, y, column.y);

                        if (CanPlacePieceOnBoard(board, rotatedBlocks, position, targetArea))
                        {
                            PlacePieceOnBoard(board, rotatedBlocks, position, true);

                            if (TrySolve(board, pieces, pieceIndex + 1, targetArea))
                            {
                                return true;
                            }

                            PlacePieceOnBoard(board, rotatedBlocks, position, false);
                        }
                    }
                }
            }

            return false;
        }

        private bool CanPlacePieceOnBoard(bool[,,] board, Vector3Int[] blocks, Vector3Int position, TargetArea targetArea)
        {
            foreach (var block in blocks)
            {
                Vector3Int worldPos = position + block;

                // Check bounds
                if (worldPos.x < 0 || worldPos.x >= board.GetLength(0) ||
                    worldPos.y < 0 || worldPos.y >= MaxHeight ||
                    worldPos.z < 0 || worldPos.z >= board.GetLength(2))
                {
                    return false;
                }

                // Check target area
                if (!targetArea.Contains(worldPos.x, worldPos.z))
                {
                    return false;
                }

                // Check occupation
                if (board[worldPos.x, worldPos.y, worldPos.z])
                {
                    return false;
                }
            }

            return true;
        }

        private void PlacePieceOnBoard(bool[,,] board, Vector3Int[] blocks, Vector3Int position, bool place)
        {
            foreach (var block in blocks)
            {
                Vector3Int worldPos = position + block;
                board[worldPos.x, worldPos.y, worldPos.z] = place;
            }
        }

        /// <summary>
        /// Calculates the board size needed to accommodate the target area.
        /// </summary>
        private Vector3Int CalculateBoardSize(TargetArea targetArea)
        {
            return new Vector3Int(
                targetArea.Width,
                MaxHeight,
                targetArea.Depth
            );
        }

        /// <summary>
        /// Determines difficulty level based on level number.
        /// </summary>
        private DifficultyLevel GetDifficultyForLevel(int levelNumber)
        {
            if (levelNumber <= 3)
            {
                return DifficultyLevel.Easy;
            }
            else if (levelNumber <= 6)
            {
                return DifficultyLevel.Medium;
            }
            else if (levelNumber <= 9)
            {
                return DifficultyLevel.Hard;
            }
            else
            {
                return DifficultyLevel.Expert;
            }
        }

        /// <summary>
        /// Spawns piece GameObjects in the spawn area.
        /// </summary>
        private void SpawnPieces(List<PieceDefinition> pieces)
        {
            if (pieceSpawnArea == null)
            {
                GameObject spawnArea = new GameObject("PieceSpawnArea");
                spawnArea.transform.position = new Vector3(8f, 0f, 0f);
                pieceSpawnArea = spawnArea.transform;
            }

            float currentX = 0f;
            float currentZ = 0f;
            int piecesPerRow = 3;
            int currentPieceIndex = 0;

            foreach (PieceDefinition pieceDef in pieces)
            {
                GameObject pieceObject = CreatePieceObject(pieceDef);

                Vector3 spawnPosition = pieceSpawnArea.position + new Vector3(currentX, 0f, currentZ);
                pieceObject.transform.position = spawnPosition;

                currentPieces.Add(pieceObject);

                currentPieceIndex++;
                currentX += spawnSpacing;

                if (currentPieceIndex % piecesPerRow == 0)
                {
                    currentX = 0f;
                    currentZ -= spawnSpacing;
                }
            }
        }

        /// <summary>
        /// Creates a piece GameObject from a piece definition.
        /// </summary>
        private GameObject CreatePieceObject(PieceDefinition pieceDef)
        {
            GameObject pieceObject;

            if (piecePrefab != null)
            {
                pieceObject = Instantiate(piecePrefab);
            }
            else
            {
                pieceObject = new GameObject($"Piece_{pieceDef.Name}");
            }

            PuzzlePiece puzzlePiece = pieceObject.GetComponent<PuzzlePiece>();
            if (puzzlePiece == null)
            {
                puzzlePiece = pieceObject.AddComponent<PuzzlePiece>();
            }

            puzzlePiece.SetBlockPositions(pieceDef.Blocks.ToList());
            puzzlePiece.SetPieceColor(pieceDef.DefaultColor);

            return pieceObject;
        }

        /// <summary>
        /// Clears all currently spawned pieces.
        /// </summary>
        private void ClearCurrentPieces()
        {
            foreach (GameObject piece in currentPieces)
            {
                if (piece != null)
                {
                    Destroy(piece);
                }
            }
            currentPieces.Clear();
        }

        /// <summary>
        /// Gets level data for a specific level number.
        /// </summary>
        public LevelData GetLevelData(int levelNumber)
        {
            DifficultyLevel difficulty = GetDifficultyForLevel(levelNumber);
            return GenerateSolvablePuzzle(difficulty, levelNumber);
        }

        /// <summary>
        /// Gets all available piece definitions.
        /// </summary>
        public PieceDefinition[] GetAllPieceDefinitions()
        {
            return allPieces ?? PieceCatalog.GetAllPieces();
        }

        /// <summary>
        /// Shuffles a list in place using Fisher-Yates algorithm.
        /// </summary>
        private void ShuffleList<T>(List<T> list)
        {
            int n = list.Count;
            for (int i = n - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                T temp = list[i];
                list[i] = list[j];
                list[j] = temp;
            }
        }
    }
}
