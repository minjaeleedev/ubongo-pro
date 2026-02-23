using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Ubongo.Core;
using Ubongo.Systems;

namespace Ubongo
{
    /// <summary>
    /// Configuration settings for each difficulty level (Level Generator specific).
    /// </summary>
    [System.Serializable]
    public class LevelDifficultyConfig
    {
        public DifficultyLevel Level;
        public int MinPieces;
        public int MaxPieces;
        public int TargetBlocks;
        public float TimeLimit;
        public int MinSolutions;
        public int MaxSolutions;

        public static LevelDifficultyConfig GetConfig(DifficultyLevel level)
        {
            return level switch
            {
                DifficultyLevel.Easy => new LevelDifficultyConfig
                {
                    Level = DifficultyLevel.Easy,
                    MinPieces = 3,
                    MaxPieces = 3,
                    TargetBlocks = 12,
                    TimeLimit = 90f,
                    MinSolutions = 6,
                    MaxSolutions = 20
                },
                DifficultyLevel.Medium => new LevelDifficultyConfig
                {
                    Level = DifficultyLevel.Medium,
                    MinPieces = 4,
                    MaxPieces = 4,
                    TargetBlocks = 16,
                    TimeLimit = 75f,
                    MinSolutions = 4,
                    MaxSolutions = 10
                },
                DifficultyLevel.Hard => new LevelDifficultyConfig
                {
                    Level = DifficultyLevel.Hard,
                    MinPieces = 4,
                    MaxPieces = 5,
                    TargetBlocks = 20,
                    TimeLimit = 60f,
                    MinSolutions = 3,
                    MaxSolutions = 6
                },
                DifficultyLevel.Expert => new LevelDifficultyConfig
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
        public List<SolutionPlacement> SolutionPlacements;
    }

    [System.Serializable]
    public struct SolutionPlacement
    {
        public int PieceIndex;
        public int RotationIndex;
        public Vector3Int Position;

        public SolutionPlacement(int pieceIndex, int rotationIndex, Vector3Int position)
        {
            PieceIndex = pieceIndex;
            RotationIndex = rotationIndex;
            Position = position;
        }
    }

    /// <summary>
    /// Generates solvable Ubongo 3D puzzles with the 8 standard pieces.
    /// </summary>
    public class LevelGenerator : MonoBehaviour
    {
        private const int MinGenerationAttempts = 1;
        private const int MaxHeight = 2;
        private const string PieceLayerName = "Piece";

        [Header("Piece Spawn Settings")]
        [SerializeField] private GameObject piecePrefab;
        [SerializeField] private Transform pieceSpawnArea;
        [SerializeField] private float spawnSpacing = 3f;
        [SerializeField] private float spawnDistance = 7f;
        [SerializeField] private float spawnHeight = 0.8f;

        [Header("Generation Settings")]
        [SerializeField] private int maxGenerationAttempts = 100;

        private List<GameObject> currentPieces = new List<GameObject>();
        private PieceDefinition[] allPieces;
        private int pieceLayerIndex = -1;
        private LevelData currentLevelData;

        public LevelData CurrentLevelData => currentLevelData;

        private void Awake()
        {
            InitializePieces();
            pieceLayerIndex = LayerMask.NameToLayer(PieceLayerName);
            if (pieceLayerIndex < 0)
            {
                Debug.LogWarning($"[{nameof(LevelGenerator)}] Layer '{PieceLayerName}' not found. Spawned pieces keep default layer.");
            }
        }

        private void OnValidate()
        {
            if (maxGenerationAttempts < MinGenerationAttempts)
            {
                maxGenerationAttempts = MinGenerationAttempts;
            }
        }

        private void InitializePieces()
        {
            allPieces = PieceCatalog.GetAllPieces();
        }

        private void EnsurePiecesInitialized()
        {
            if (allPieces == null)
            {
                InitializePieces();
            }
        }

        private int GetGenerationAttemptCount()
        {
            if (maxGenerationAttempts < MinGenerationAttempts)
            {
                Debug.LogWarning($"[{nameof(LevelGenerator)}] maxGenerationAttempts must be >= {MinGenerationAttempts}. Clamping value.");
                maxGenerationAttempts = MinGenerationAttempts;
            }

            return maxGenerationAttempts;
        }

        /// <summary>
        /// Generates a level with the specified difficulty.
        /// </summary>
        public void GenerateLevel(int levelNumber)
        {
            if (!TryCreateLevelData(levelNumber, out LevelData levelData))
            {
                Debug.LogError($"[{nameof(LevelGenerator)}] Failed to generate level {levelNumber}.");
                return;
            }

            SpawnFromLevelData(levelData);
        }

        public LevelData GenerateLevelData(int levelNumber)
        {
            if (TryCreateLevelData(levelNumber, out LevelData levelData))
            {
                return levelData;
            }

            Debug.LogError($"[{nameof(LevelGenerator)}] GenerateLevelData failed for level {levelNumber}.");
            return null;
        }

        public LevelData GenerateLevelData(int levelNumber, DifficultyLevel difficulty)
        {
            if (TryCreateLevelData(levelNumber, difficulty, out LevelData levelData))
            {
                return levelData;
            }

            Debug.LogError($"[{nameof(LevelGenerator)}] GenerateLevelData failed for level {levelNumber} ({difficulty}).");
            return null;
        }

        public bool TryCreateLevelData(int levelNumber, out LevelData levelData)
        {
            DifficultyLevel difficulty = GetDifficultyForLevel(levelNumber);
            return TryCreateLevelData(levelNumber, difficulty, out levelData);
        }

        public bool TryCreateLevelData(int levelNumber, DifficultyLevel difficulty, out LevelData levelData)
        {
            if (!TryCreateSolvablePuzzle(difficulty, levelNumber, out levelData))
            {
                currentLevelData = null;
                return false;
            }

            currentLevelData = levelData;
            return true;
        }

        /// <summary>
        /// Spawns pieces for the provided level payload.
        /// Contract: invalid payload is treated as no-op.
        /// Use <see cref="ClearSpawnedPieces"/> for explicit clear intent.
        /// </summary>
        public void SpawnFromLevelData(LevelData levelData)
        {
            if (levelData == null || levelData.Pieces == null || levelData.Pieces.Count == 0)
            {
                return;
            }

            ClearCurrentPieces();
            currentLevelData = levelData;
            SpawnPieces(levelData.Pieces);
        }

        /// <summary>
        /// Generates a solvable puzzle configuration.
        /// </summary>
        public LevelData GenerateSolvablePuzzle(DifficultyLevel difficulty, int levelNumber = 1)
        {
            if (TryCreateSolvablePuzzle(difficulty, levelNumber, out LevelData levelData))
            {
                return levelData;
            }

            Debug.LogError($"[{nameof(LevelGenerator)}] GenerateSolvablePuzzle failed for level {levelNumber} ({difficulty}).");
            return null;
        }

        private bool TryCreateSolvablePuzzle(DifficultyLevel difficulty, int levelNumber, out LevelData levelData)
        {
            levelData = null;
            EnsurePiecesInitialized();

            LevelDifficultyConfig config = LevelDifficultyConfig.GetConfig(difficulty);
            int generationAttempts = GetGenerationAttemptCount();

            for (int attempt = 0; attempt < generationAttempts; attempt++)
            {
                List<PieceDefinition> selectedPieces = SelectPiecesForDifficulty(config);
                if (selectedPieces == null || selectedPieces.Count == 0)
                {
                    continue;
                }

                int totalBlocks = selectedPieces.Sum(p => p.BlockCount);
                if (totalBlocks <= 0 || totalBlocks % MaxHeight != 0)
                {
                    continue;
                }

                // Calculate target area dimensions (must fit 2 layers exactly)
                int footprintSize = totalBlocks / MaxHeight;
                TargetArea targetArea = CalculateTargetArea(footprintSize, difficulty);
                if (targetArea == null || targetArea.TotalCells != totalBlocks)
                {
                    continue;
                }

                if (!TryFindSolution(selectedPieces, targetArea, out List<SolutionPlacement> solutionPlacements))
                {
                    continue;
                }

                levelData = new LevelData
                {
                    LevelNumber = levelNumber,
                    Difficulty = difficulty,
                    TimeLimit = config.TimeLimit,
                    Pieces = selectedPieces,
                    BoardSize = CalculateBoardSize(targetArea),
                    TargetArea = targetArea,
                    SolutionPlacements = solutionPlacements
                };

                return true;
            }

            Debug.LogWarning(
                $"[{nameof(LevelGenerator)}] Failed to create a solvable puzzle. " +
                $"level={levelNumber}, difficulty={difficulty}, attempts={generationAttempts}");
            return false;
        }

        /// <summary>
        /// Selects pieces based on difficulty configuration.
        /// </summary>
        private List<PieceDefinition> SelectPiecesForDifficulty(LevelDifficultyConfig config)
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

        private bool TryFindSolution(List<PieceDefinition> pieces, TargetArea targetArea, out List<SolutionPlacement> solutionPlacements)
        {
            solutionPlacements = null;

            if (pieces == null || pieces.Count == 0 || targetArea == null)
            {
                return false;
            }

            int totalPieceBlocks = pieces.Sum(p => p.BlockCount);
            if (totalPieceBlocks != targetArea.TotalCells)
            {
                return false;
            }

            var board = new bool[targetArea.Width + 4, MaxHeight, targetArea.Depth + 4];
            var workingPlacements = new List<SolutionPlacement>(pieces.Count);

            if (!TrySolve(board, pieces, 0, targetArea, workingPlacements))
            {
                return false;
            }

            solutionPlacements = new List<SolutionPlacement>(workingPlacements);
            return true;
        }

        private bool TrySolve(
            bool[,,] board,
            List<PieceDefinition> pieces,
            int pieceIndex,
            TargetArea targetArea,
            List<SolutionPlacement> workingPlacements)
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
                            workingPlacements.Add(new SolutionPlacement(pieceIndex, rotationIndex, position));

                            if (TrySolve(board, pieces, pieceIndex + 1, targetArea, workingPlacements))
                            {
                                return true;
                            }

                            workingPlacements.RemoveAt(workingPlacements.Count - 1);
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
            Vector3 spawnAnchor = CalculateSpawnAnchor();

            if (pieceSpawnArea == null)
            {
                GameObject spawnArea = new GameObject("PieceSpawnArea");
                spawnArea.transform.SetParent(transform);
                pieceSpawnArea = spawnArea.transform;
            }

            pieceSpawnArea.position = spawnAnchor;

            float totalWidth = (pieces.Count - 1) * spawnSpacing;
            Vector3 rowDirection = GetSpawnRowDirection();
            Vector3 startOffset = -rowDirection * (totalWidth * 0.5f);

            for (int i = 0; i < pieces.Count; i++)
            {
                PieceDefinition pieceDef = pieces[i];
                GameObject pieceObject = CreatePieceObject(pieceDef);

                Vector3 spawnPosition = pieceSpawnArea.position + startOffset + (rowDirection * i * spawnSpacing);
                pieceObject.transform.position = spawnPosition;
                pieceObject.transform.rotation = Quaternion.identity;

                currentPieces.Add(pieceObject);
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
            ApplyLayerRecursively(pieceObject, pieceLayerIndex);

            return pieceObject;
        }

        private Vector3 CalculateSpawnAnchor()
        {
            GameBoard board = FindAnyObjectByType<GameBoard>();
            Vector3 boardCenter = board != null ? board.GetBoardCenterWorld() : Vector3.zero;
            Vector3 leftDirection = GetSpawnLeftDirection();

            return boardCenter + (leftDirection * spawnDistance) + (Vector3.up * spawnHeight);
        }

        private Vector3 GetSpawnLeftDirection()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                return Vector3.left;
            }

            Vector3 projectedLeft = Vector3.ProjectOnPlane(-cam.transform.right, Vector3.up);
            if (projectedLeft.sqrMagnitude < 0.0001f)
            {
                return Vector3.left;
            }

            return projectedLeft.normalized;
        }

        private Vector3 GetSpawnRowDirection()
        {
            Vector3 leftDirection = GetSpawnLeftDirection();
            Vector3 rowDirection = Vector3.Cross(Vector3.up, leftDirection);

            if (rowDirection.sqrMagnitude < 0.0001f)
            {
                return Vector3.forward;
            }

            return rowDirection.normalized;
        }

        private static void ApplyLayerRecursively(GameObject target, int layerIndex)
        {
            if (target == null || layerIndex < 0)
            {
                return;
            }

            target.layer = layerIndex;
            foreach (Transform child in target.transform)
            {
                ApplyLayerRecursively(child.gameObject, layerIndex);
            }
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
                    UnityObjectUtility.SafeDestroy(piece);
                }
            }
            currentPieces.Clear();
        }

        /// <summary>
        /// Explicitly clears currently spawned piece objects.
        /// This is separated from spawn API to keep spawn contract side-effect free on invalid payload.
        /// </summary>
        public void ClearSpawnedPieces()
        {
            ClearCurrentPieces();
        }

        /// <summary>
        /// Gets level data for a specific level number.
        /// </summary>
        public LevelData GetLevelData(int levelNumber)
        {
            return GenerateLevelData(levelNumber);
        }

        /// <summary>
        /// Gets all available piece definitions.
        /// </summary>
        public PieceDefinition[] GetAllPieceDefinitions()
        {
            return allPieces ?? PieceCatalog.GetAllPieces();
        }

        public bool TryGetSpawnedPiecesBounds(out Bounds bounds)
        {
            bounds = default;
            bool hasBounds = false;

            foreach (GameObject piece in currentPieces)
            {
                if (piece == null)
                {
                    continue;
                }

                Renderer[] renderers = piece.GetComponentsInChildren<Renderer>();
                foreach (Renderer renderer in renderers)
                {
                    if (renderer == null || !renderer.enabled)
                    {
                        continue;
                    }

                    if (!hasBounds)
                    {
                        bounds = renderer.bounds;
                        hasBounds = true;
                    }
                    else
                    {
                        bounds.Encapsulate(renderer.bounds);
                    }
                }
            }

            return hasBounds;
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
