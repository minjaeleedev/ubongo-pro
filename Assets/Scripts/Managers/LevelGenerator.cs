using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using Ubongo.Core;
using Ubongo.Domain;

namespace Ubongo
{
    /// <summary>
    /// Configuration settings for each difficulty level (Level Generator specific).
    /// </summary>
    [System.Serializable]
    public class LevelDifficultyConfig
    {
        public DifficultyLevel Level;
        public int PieceCount;
        public int MinFootprintCells;
        public int MaxFootprintCells;
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
                    PieceCount = 3,
                    MinFootprintCells = 6,
                    MaxFootprintCells = 7,
                    MinPieces = 3,
                    MaxPieces = 3,
                    TargetBlocks = 14,
                    TimeLimit = 90f,
                    MinSolutions = 6,
                    MaxSolutions = 20
                },
                DifficultyLevel.Hard => new LevelDifficultyConfig
                {
                    Level = DifficultyLevel.Hard,
                    PieceCount = 4,
                    MinFootprintCells = 8,
                    MaxFootprintCells = 8,
                    MinPieces = 4,
                    MaxPieces = 4,
                    TargetBlocks = 16,
                    TimeLimit = 60f,
                    MinSolutions = 3,
                    MaxSolutions = 6
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
        private const int MinPieceCells = 2;
        private const int PreferredSolutionSamplesPerSizeSet = 24;
        private const string PieceLayerName = "Piece";

        private static readonly string[][] EasyFootprintPatterns =
        {
            new[] { "xxx", "xxx" },
            new[] { "x..", "xx.", "xxx" },
            new[] { "xx.", "xxx", ".x." },
            new[] { "xxx", "x..", "xx." },
            new[] { "xxxx", "xxx." },
            new[] { "xx.", "xxx", "xx." },
            new[] { ".xx", "xxx", "xx." },
            new[] { "x..", "xx.", "xxx", ".x." }
        };

        private static readonly string[][] HardFootprintPatterns =
        {
            new[] { "xxxx", "xxxx" },
            new[] { "xxx", "xxx", "xx." },
            new[] { "xx.", "xxx", "xxx" },
            new[] { "x..", "xx.", "xxx", ".xx" },
            new[] { "xx..", "xxxx", ".xx." },
            new[] { "xxx.", ".xxx", ".xx." }
        };

        private sealed class PartitionCandidate
        {
            public List<List<Vector3Int>> Partitions { get; }
            public int Score { get; }

            public PartitionCandidate(List<List<Vector3Int>> partitions, int score)
            {
                Partitions = partitions;
                Score = score;
            }
        }

        private sealed class PartitionSearchState
        {
            public int ExploredSolutions { get; set; }
            public PartitionCandidate BestCandidate { get; set; }
        }

        private readonly struct PieceShapeProfile
        {
            public int BlockCount { get; }
            public int SizeX { get; }
            public int SizeY { get; }
            public int SizeZ { get; }
            public bool IsStraightLine { get; }
            public bool IsSolidPrism { get; }
            public bool HasVerticalVariation { get; }
            public int BranchNodeCount { get; }
            public string CanonicalKey { get; }
            public bool IsComplexLargePiece =>
                BlockCount >= 5 &&
                !IsStraightLine &&
                !IsSolidPrism &&
                (HasVerticalVariation || BranchNodeCount > 0 || (SizeX > 1 && SizeZ > 1));

            public PieceShapeProfile(
                int blockCount,
                int sizeX,
                int sizeY,
                int sizeZ,
                bool isStraightLine,
                bool isSolidPrism,
                bool hasVerticalVariation,
                int branchNodeCount,
                string canonicalKey)
            {
                BlockCount = blockCount;
                SizeX = sizeX;
                SizeY = sizeY;
                SizeZ = sizeZ;
                IsStraightLine = isStraightLine;
                IsSolidPrism = isSolidPrism;
                HasVerticalVariation = hasVerticalVariation;
                BranchNodeCount = branchNodeCount;
                CanonicalKey = canonicalKey;
            }
        }

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

        public bool TryCreateLevelData(int levelNumber, DifficultyLevel difficulty, TargetArea targetArea, int pieceCount, out LevelData levelData)
        {
            EnsurePiecesInitialized();

            if (!TryCreateLevelDataForTargetArea(
                    levelNumber,
                    difficulty,
                    LevelDifficultyConfig.GetConfig(difficulty).TimeLimit,
                    targetArea,
                    new List<int> { pieceCount },
                    shufflePieceCounts: false,
                    out levelData))
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

            Debug.Log($"[RoundFlow][F{Time.frameCount}] LevelGenerator.SpawnFromLevelData: incoming pieces={levelData.Pieces.Count}, existing pieces={currentPieces.Count}");
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

            List<TargetArea> targetAreas = BuildAutomaticTargetAreas(difficulty);
            if (targetAreas.Count == 0)
            {
                Debug.LogWarning($"[{nameof(LevelGenerator)}] No target area templates configured for {difficulty}.");
                return false;
            }

            List<int> pieceCountCandidates = new List<int> { config.PieceCount };
            List<TargetArea> shuffledTargetAreas = new List<TargetArea>(targetAreas);
            ShuffleList(shuffledTargetAreas);

            for (int attempt = 0; attempt < generationAttempts; attempt++)
            {
                TargetArea targetArea = shuffledTargetAreas[attempt % shuffledTargetAreas.Count];
                if (!TryCreateLevelDataForTargetArea(
                        levelNumber,
                        difficulty,
                        config.TimeLimit,
                        targetArea,
                        pieceCountCandidates,
                        shufflePieceCounts: false,
                        out levelData))
                {
                    continue;
                }

                return true;
            }

            if (TryCreateSolvablePuzzleDeterministic(difficulty, levelNumber, config, targetAreas, out levelData))
            {
                return true;
            }

            Debug.LogWarning(
                $"[{nameof(LevelGenerator)}] Failed to create a solvable puzzle. " +
                $"level={levelNumber}, difficulty={difficulty}, attempts={generationAttempts}");
            return false;
        }

        private bool TryCreateSolvablePuzzleDeterministic(
            DifficultyLevel difficulty,
            int levelNumber,
            LevelDifficultyConfig config,
            IReadOnlyList<TargetArea> targetAreas,
            out LevelData levelData)
        {
            levelData = null;

            if (targetAreas == null || targetAreas.Count == 0)
            {
                return false;
            }

            List<int> pieceCountCandidates = new List<int> { config.PieceCount };
            foreach (TargetArea targetArea in targetAreas)
            {
                if (!TryCreateLevelDataForTargetArea(
                        levelNumber,
                        difficulty,
                        config.TimeLimit,
                        targetArea,
                        pieceCountCandidates,
                        shufflePieceCounts: false,
                        out levelData))
                {
                    continue;
                }

                return true;
            }

            return false;
        }

        private bool TryCreateLevelDataForTargetArea(
            int levelNumber,
            DifficultyLevel difficulty,
            float timeLimit,
            TargetArea targetArea,
            IReadOnlyList<int> pieceCountCandidates,
            bool shufflePieceCounts,
            out LevelData levelData)
        {
            levelData = null;

            TargetArea normalizedTargetArea = NormalizeTargetAreaToOrigin(targetArea);
            if (normalizedTargetArea == null || normalizedTargetArea.FootprintSize == 0)
            {
                return false;
            }

            if (pieceCountCandidates == null || pieceCountCandidates.Count == 0)
            {
                return false;
            }

            List<int> candidateCounts = new List<int>(pieceCountCandidates.Where(count => count > 0 && count <= normalizedTargetArea.TotalCells));
            if (candidateCounts.Count == 0)
            {
                return false;
            }

            if (shufflePieceCounts)
            {
                ShuffleList(candidateCounts);
            }

            for (int i = 0; i < candidateCounts.Count; i++)
            {
                int pieceCount = candidateCounts[i];
                if (!TryPartitionTargetArea(normalizedTargetArea, pieceCount, out List<PieceDefinition> pieces, out List<SolutionPlacement> solutionPlacements))
                {
                    continue;
                }

                levelData = CreateLevelData(levelNumber, difficulty, timeLimit, pieces, normalizedTargetArea, solutionPlacements);
                return true;
            }

            return false;
        }

        private List<List<PieceDefinition>> BuildPieceCombinationCandidates(LevelDifficultyConfig config, int targetBlocks)
        {
            var candidates = new List<List<PieceDefinition>>();
            var working = new List<PieceDefinition>();
            BuildPieceCombinationCandidatesRecursive(0, 0, config, targetBlocks, working, candidates);
            return candidates;
        }

        private void BuildPieceCombinationCandidatesRecursive(
            int startIndex,
            int currentBlocks,
            LevelDifficultyConfig config,
            int targetBlocks,
            List<PieceDefinition> working,
            List<List<PieceDefinition>> candidates)
        {
            if (working.Count > config.MaxPieces || currentBlocks > targetBlocks)
            {
                return;
            }

            if (working.Count >= config.MinPieces &&
                working.Count <= config.MaxPieces &&
                currentBlocks == targetBlocks)
            {
                candidates.Add(new List<PieceDefinition>(working));
                return;
            }

            if (working.Count == config.MaxPieces)
            {
                return;
            }

            for (int i = startIndex; i < allPieces.Length; i++)
            {
                PieceDefinition piece = allPieces[i];
                working.Add(piece);
                BuildPieceCombinationCandidatesRecursive(i + 1, currentBlocks + piece.BlockCount, config, targetBlocks, working, candidates);
                working.RemoveAt(working.Count - 1);
            }
        }

        private int ResolveReachableTargetBlocks(LevelDifficultyConfig config)
        {
            List<int> reachableTotals = GetReachableBlockTotals(config);
            if (reachableTotals.Count == 0)
            {
                return config.TargetBlocks;
            }

            int resolvedTarget = reachableTotals[0];
            if (resolvedTarget != config.TargetBlocks)
            {
                Debug.LogWarning(
                    $"[{nameof(LevelGenerator)}] TargetBlocks={config.TargetBlocks} is unreachable with current piece catalog for {config.Level}. " +
                    $"Using reachable target={resolvedTarget}.");
            }

            return resolvedTarget;
        }

        private List<int> GetReachableBlockTotals(LevelDifficultyConfig config)
        {
            var totals = new HashSet<int>();
            CollectReachableBlockTotals(0, 0, 0, config, totals);

            return totals
                .Where(total => total > 0 && total % TargetArea.RequiredHeight == 0)
                .OrderBy(total => Mathf.Abs(total - config.TargetBlocks))
                .ThenByDescending(total => total)
                .ToList();
        }

        private void CollectReachableBlockTotals(
            int startIndex,
            int pieceCount,
            int totalBlocks,
            LevelDifficultyConfig config,
            HashSet<int> totals)
        {
            if (pieceCount > config.MaxPieces)
            {
                return;
            }

            if (pieceCount >= config.MinPieces)
            {
                totals.Add(totalBlocks);
            }

            if (pieceCount == config.MaxPieces)
            {
                return;
            }

            for (int i = startIndex; i < allPieces.Length; i++)
            {
                CollectReachableBlockTotals(
                    i + 1,
                    pieceCount + 1,
                    totalBlocks + allPieces[i].BlockCount,
                    config,
                    totals);
            }
        }

        private List<TargetArea> BuildAutomaticTargetAreas(DifficultyLevel difficulty)
        {
            string[][] sourcePatterns = difficulty switch
            {
                DifficultyLevel.Easy => EasyFootprintPatterns,
                DifficultyLevel.Hard => HardFootprintPatterns,
                _ => Array.Empty<string[]>()
            };

            var areas = new List<TargetArea>();
            var emitted = new HashSet<string>();

            for (int i = 0; i < sourcePatterns.Length; i++)
            {
                TargetArea candidate = CreateTargetAreaFromRows(sourcePatterns[i]);
                if (candidate == null || !IsAutomaticTargetAreaValid(candidate, difficulty))
                {
                    continue;
                }

                string key = GetTargetAreaKey(candidate);
                if (!emitted.Add(key))
                {
                    continue;
                }

                areas.Add(candidate);
            }

            return areas;
        }

        private bool IsAutomaticTargetAreaValid(TargetArea targetArea, DifficultyLevel difficulty)
        {
            if (targetArea == null)
            {
                return false;
            }

            LevelDifficultyConfig config = LevelDifficultyConfig.GetConfig(difficulty);
            if (targetArea.FootprintSize < config.MinFootprintCells || targetArea.FootprintSize > config.MaxFootprintCells)
            {
                return false;
            }

            if (IsSingleRowOrColumn(targetArea) || !AreFootprintColumnsConnected(targetArea))
            {
                return false;
            }

            return true;
        }

        private TargetArea CreateTargetAreaFromRows(IReadOnlyList<string> rows)
        {
            if (rows == null || rows.Count == 0)
            {
                return null;
            }

            int width = rows.Max(row => row?.Length ?? 0);
            bool[,] mask = new bool[width, rows.Count];

            for (int z = 0; z < rows.Count; z++)
            {
                string row = rows[z] ?? string.Empty;
                for (int x = 0; x < row.Length; x++)
                {
                    mask[x, z] = row[x] == 'x' || row[x] == 'X';
                }
            }

            return TargetArea.CreateFromMask(mask);
        }

        private string GetTargetAreaKey(TargetArea targetArea)
        {
            return string.Join(
                ";",
                targetArea.GetColumnPositions()
                    .OrderBy(position => position.x)
                    .ThenBy(position => position.y)
                    .Select(position => $"{position.x}:{position.y}"));
        }

        private bool IsSingleRowOrColumn(TargetArea targetArea)
        {
            List<Vector2Int> columns = targetArea.GetColumnPositions().ToList();
            if (columns.Count <= 1)
            {
                return true;
            }

            bool singleRow = columns.All(position => position.y == columns[0].y);
            bool singleColumn = columns.All(position => position.x == columns[0].x);
            return singleRow || singleColumn;
        }

        private bool AreFootprintColumnsConnected(TargetArea targetArea)
        {
            List<Vector2Int> columns = targetArea.GetColumnPositions().ToList();
            if (columns.Count == 0)
            {
                return false;
            }

            var remaining = new HashSet<Vector2Int>(columns);
            var queue = new Queue<Vector2Int>();
            Vector2Int start = columns[0];
            queue.Enqueue(start);
            remaining.Remove(start);

            while (queue.Count > 0)
            {
                Vector2Int current = queue.Dequeue();
                Vector2Int[] neighbors =
                {
                    new Vector2Int(current.x + 1, current.y),
                    new Vector2Int(current.x - 1, current.y),
                    new Vector2Int(current.x, current.y + 1),
                    new Vector2Int(current.x, current.y - 1)
                };

                for (int i = 0; i < neighbors.Length; i++)
                {
                    if (!remaining.Remove(neighbors[i]))
                    {
                        continue;
                    }

                    queue.Enqueue(neighbors[i]);
                }
            }

            return remaining.Count == 0;
        }

        private LevelData CreateLevelData(
            int levelNumber,
            DifficultyLevel difficulty,
            float timeLimit,
            List<PieceDefinition> pieces,
            TargetArea targetArea,
            List<SolutionPlacement> solutionPlacements)
        {
            return new LevelData
            {
                LevelNumber = levelNumber,
                Difficulty = difficulty,
                TimeLimit = timeLimit,
                Pieces = new List<PieceDefinition>(pieces),
                BoardSize = CalculateBoardSize(targetArea),
                TargetArea = targetArea,
                SolutionPlacements = new List<SolutionPlacement>(solutionPlacements)
            };
        }

        private List<int> GetPieceCountCandidates(LevelDifficultyConfig config, int totalBlocks)
        {
            var candidates = new List<int>();
            if (config == null || totalBlocks <= 0)
            {
                return candidates;
            }

            for (int pieceCount = config.MinPieces; pieceCount <= config.MaxPieces; pieceCount++)
            {
                if (pieceCount <= 0 || pieceCount > totalBlocks)
                {
                    continue;
                }

                candidates.Add(pieceCount);
            }

            return candidates;
        }

        private TargetArea NormalizeTargetAreaToOrigin(TargetArea targetArea)
        {
            if (targetArea == null)
            {
                return null;
            }

            List<Vector2Int> columns = targetArea.GetColumnPositions().ToList();
            if (columns.Count == 0)
            {
                return new TargetArea();
            }

            int minX = columns.Min(column => column.x);
            int minZ = columns.Min(column => column.y);

            if (minX == 0 && minZ == 0)
            {
                return targetArea.Clone();
            }

            return new TargetArea(columns.Select(column => new Vector2Int(column.x - minX, column.y - minZ)));
        }

        private bool TryPartitionTargetArea(
            TargetArea targetArea,
            int pieceCount,
            out List<PieceDefinition> pieces,
            out List<SolutionPlacement> solutionPlacements)
        {
            pieces = null;
            solutionPlacements = null;

            if (targetArea == null || pieceCount <= 0)
            {
                return false;
            }

            List<Vector3Int> allCells = targetArea
                .GetAllCells()
                .OrderBy(cell => cell.x)
                .ThenBy(cell => cell.y)
                .ThenBy(cell => cell.z)
                .ToList();

            if (allCells.Count == 0 || pieceCount > allCells.Count)
            {
                return false;
            }

            List<List<int>> pieceSizeCandidates = BuildPieceSizeCandidates(allCells.Count, pieceCount);
            PartitionCandidate bestCandidate = null;

            for (int i = 0; i < pieceSizeCandidates.Count; i++)
            {
                PartitionSearchState searchState = FindBestPartitionForSizes(allCells, pieceSizeCandidates[i]);
                if (searchState.BestCandidate == null)
                {
                    continue;
                }

                if (bestCandidate == null || searchState.BestCandidate.Score > bestCandidate.Score)
                {
                    bestCandidate = searchState.BestCandidate;
                }
            }

            if (bestCandidate == null)
            {
                return false;
            }

            BuildGeneratedPieces(bestCandidate.Partitions, out pieces, out solutionPlacements);
            return true;
        }

        private List<List<int>> BuildPieceSizeCandidates(int totalCells, int pieceCount)
        {
            var candidates = new List<List<int>>();
            if (pieceCount <= 0 || totalCells < pieceCount * MinPieceCells)
            {
                return candidates;
            }

            BuildPieceSizeCandidatesRecursive(totalCells, pieceCount, totalCells, new List<int>(), candidates);

            return candidates
                .Where(IsPieceSizeCandidateValid)
                .OrderByDescending(EvaluatePieceSizeCandidate)
                .ThenByDescending(sizes => sizes.Max())
                .ToList();
        }

        private void BuildPieceSizeCandidatesRecursive(
            int remainingCells,
            int remainingPieces,
            int maxNextSize,
            List<int> working,
            List<List<int>> candidates)
        {
            if (remainingPieces == 0)
            {
                if (remainingCells == 0)
                {
                    candidates.Add(new List<int>(working));
                }

                return;
            }

            int minRequiredForRest = (remainingPieces - 1) * MinPieceCells;
            int upperBound = Mathf.Min(maxNextSize, remainingCells - minRequiredForRest);
            for (int nextSize = upperBound; nextSize >= MinPieceCells; nextSize--)
            {
                working.Add(nextSize);
                BuildPieceSizeCandidatesRecursive(
                    remainingCells - nextSize,
                    remainingPieces - 1,
                    nextSize,
                    working,
                    candidates);
                working.RemoveAt(working.Count - 1);
            }
        }

        private bool IsPieceSizeCandidateValid(List<int> pieceSizes)
        {
            if (pieceSizes == null || pieceSizes.Count == 0)
            {
                return false;
            }

            if (pieceSizes.Any(size => size < MinPieceCells))
            {
                return false;
            }

            if (pieceSizes.Contains(2) && pieceSizes.Max() < 5)
            {
                return false;
            }

            return true;
        }

        private int EvaluatePieceSizeCandidate(List<int> pieceSizes)
        {
            if (!IsPieceSizeCandidateValid(pieceSizes))
            {
                return int.MinValue;
            }

            int distinctSizeCount = pieceSizes.Distinct().Count();
            int sizeRange = pieceSizes[0] - pieceSizes[pieceSizes.Count - 1];
            int score = 0;

            score += distinctSizeCount * 12;
            score += sizeRange * 6;

            if (distinctSizeCount == 1)
            {
                score -= 40;
            }

            if (pieceSizes.Contains(2))
            {
                score += 8;
            }

            if (pieceSizes.Any(size => size >= 5))
            {
                score += 5;
            }

            return score;
        }

        private PartitionSearchState FindBestPartitionForSizes(List<Vector3Int> allCells, List<int> pieceSizes)
        {
            PartitionSearchState state = new PartitionSearchState();
            TryPartitionCells(
                new HashSet<Vector3Int>(allCells),
                pieceSizes,
                new List<List<Vector3Int>>(pieceSizes.Count),
                state);
            return state;
        }

        private void TryPartitionCells(
            HashSet<Vector3Int> remainingCells,
            List<int> remainingPieceSizes,
            List<List<Vector3Int>> partitions,
            PartitionSearchState state)
        {
            if (state == null || remainingPieceSizes == null || partitions == null)
            {
                return;
            }

            if (state.ExploredSolutions >= PreferredSolutionSamplesPerSizeSet)
            {
                return;
            }

            if (remainingPieceSizes.Count == 0)
            {
                if (remainingCells.Count == 0)
                {
                    EvaluatePartitionCandidate(partitions, state);
                }

                return;
            }

            if (remainingCells == null || remainingCells.Count == 0)
            {
                return;
            }

            int requiredCells = 0;
            for (int i = 0; i < remainingPieceSizes.Count; i++)
            {
                requiredCells += remainingPieceSizes[i];
            }

            if (requiredCells != remainingCells.Count)
            {
                return;
            }

            int targetRegionSize = remainingPieceSizes[0];
            if (remainingPieceSizes.Count == 1)
            {
                if (remainingCells.Count != targetRegionSize || !IsConnected(remainingCells))
                {
                    return;
                }

                partitions.Add(SortCells(remainingCells));
                EvaluatePartitionCandidate(partitions, state);
                partitions.RemoveAt(partitions.Count - 1);
                return;
            }

            Vector3Int seed = GetFirstCell(remainingCells);
            HashSet<Vector3Int> region = new HashSet<Vector3Int> { seed };
            HashSet<Vector3Int> frontier = BuildFrontier(region, remainingCells);
            TryGrowPartitionRegion(remainingCells, remainingPieceSizes, targetRegionSize, region, frontier, partitions, state);
        }

        private void TryGrowPartitionRegion(
            HashSet<Vector3Int> remainingCells,
            List<int> remainingPieceSizes,
            int targetRegionSize,
            HashSet<Vector3Int> region,
            HashSet<Vector3Int> frontier,
            List<List<Vector3Int>> partitions,
            PartitionSearchState state)
        {
            if (state.ExploredSolutions >= PreferredSolutionSamplesPerSizeSet)
            {
                return;
            }

            if (region.Count == targetRegionSize)
            {
                HashSet<Vector3Int> leftover = new HashSet<Vector3Int>(remainingCells);
                leftover.ExceptWith(region);

                List<int> nextPieceSizes = remainingPieceSizes.Skip(1).ToList();
                if (!CanRemainingCellsSupportPieces(leftover, nextPieceSizes))
                {
                    return;
                }

                partitions.Add(SortCells(region));
                TryPartitionCells(leftover, nextPieceSizes, partitions, state);
                partitions.RemoveAt(partitions.Count - 1);
                return;
            }

            int remainingRequiredCells = 0;
            for (int i = 1; i < remainingPieceSizes.Count; i++)
            {
                remainingRequiredCells += remainingPieceSizes[i];
            }

            if ((remainingCells.Count - region.Count) < remainingRequiredCells)
            {
                return;
            }

            if (GetReachableCellCount(region, remainingCells) < targetRegionSize)
            {
                return;
            }

            List<Vector3Int> orderedCandidates = frontier
                .OrderByDescending(cell => ScorePartitionGrowthCandidate(cell, region, remainingCells))
                .ThenBy(cell => CountAvailableNeighbors(cell, remainingCells))
                .ThenBy(cell => cell.x)
                .ThenBy(cell => cell.y)
                .ThenBy(cell => cell.z)
                .ToList();

            for (int i = 0; i < orderedCandidates.Count; i++)
            {
                Vector3Int candidate = orderedCandidates[i];
                if (!region.Add(candidate))
                {
                    continue;
                }

                HashSet<Vector3Int> nextFrontier = BuildFrontier(region, remainingCells);
                TryGrowPartitionRegion(remainingCells, remainingPieceSizes, targetRegionSize, region, nextFrontier, partitions, state);
                region.Remove(candidate);

                if (state.ExploredSolutions >= PreferredSolutionSamplesPerSizeSet)
                {
                    return;
                }
            }
        }

        private int ScorePartitionGrowthCandidate(
            Vector3Int candidate,
            HashSet<Vector3Int> region,
            HashSet<Vector3Int> availableCells)
        {
            int score = 0;
            if (region.Any(cell => cell.y != candidate.y))
            {
                score += 6;
            }

            if (region.Any(cell => cell.x != candidate.x))
            {
                score += 3;
            }

            if (region.Any(cell => cell.z != candidate.z))
            {
                score += 3;
            }

            int neighborsInRegion = CountNeighbors(candidate, region);
            if (neighborsInRegion == 1)
            {
                score += 4;
            }
            else if (neighborsInRegion == 2)
            {
                score += 1;
            }

            score -= CountAvailableNeighbors(candidate, availableCells);
            return score;
        }

        private bool CanRemainingCellsSupportPieces(HashSet<Vector3Int> remainingCells, List<int> remainingPieceSizes)
        {
            if (remainingPieceSizes == null)
            {
                return false;
            }

            if (remainingPieceSizes.Count == 0)
            {
                return remainingCells == null || remainingCells.Count == 0;
            }

            if (remainingCells == null || remainingCells.Count == 0)
            {
                return false;
            }

            int requiredCells = 0;
            int minPieceSize = int.MaxValue;
            for (int i = 0; i < remainingPieceSizes.Count; i++)
            {
                requiredCells += remainingPieceSizes[i];
                if (remainingPieceSizes[i] < minPieceSize)
                {
                    minPieceSize = remainingPieceSizes[i];
                }
            }

            if (requiredCells != remainingCells.Count)
            {
                return false;
            }

            List<List<Vector3Int>> components = GetConnectedComponents(remainingCells);
            if (components.Count > remainingPieceSizes.Count)
            {
                return false;
            }

            for (int i = 0; i < components.Count; i++)
            {
                if (components[i].Count < minPieceSize)
                {
                    return false;
                }
            }

            if (remainingPieceSizes.Count == 1)
            {
                return components.Count == 1 && components[0].Count == remainingPieceSizes[0];
            }

            return true;
        }

        private HashSet<Vector3Int> BuildFrontier(HashSet<Vector3Int> region, HashSet<Vector3Int> availableCells)
        {
            var frontier = new HashSet<Vector3Int>();
            foreach (Vector3Int cell in region)
            {
                foreach (Vector3Int neighbor in GetAdjacentCells(cell))
                {
                    if (availableCells.Contains(neighbor) && !region.Contains(neighbor))
                    {
                        frontier.Add(neighbor);
                    }
                }
            }

            return frontier;
        }

        private int GetReachableCellCount(HashSet<Vector3Int> region, HashSet<Vector3Int> availableCells)
        {
            if (region == null || region.Count == 0 || availableCells == null || availableCells.Count == 0)
            {
                return 0;
            }

            Vector3Int start = GetFirstCell(region);
            var visited = new HashSet<Vector3Int>();
            var queue = new Queue<Vector3Int>();
            queue.Enqueue(start);
            visited.Add(start);

            while (queue.Count > 0)
            {
                Vector3Int current = queue.Dequeue();
                foreach (Vector3Int neighbor in GetAdjacentCells(current))
                {
                    if (!availableCells.Contains(neighbor) || !visited.Add(neighbor))
                    {
                        continue;
                    }

                    queue.Enqueue(neighbor);
                }
            }

            return visited.Count;
        }

        private int CountNeighbors(Vector3Int cell, IEnumerable<Vector3Int> cells)
        {
            HashSet<Vector3Int> lookup = cells as HashSet<Vector3Int> ?? new HashSet<Vector3Int>(cells);
            int count = 0;
            foreach (Vector3Int neighbor in GetAdjacentCells(cell))
            {
                if (lookup.Contains(neighbor))
                {
                    count++;
                }
            }

            return count;
        }

        private int CountAvailableNeighbors(Vector3Int cell, HashSet<Vector3Int> availableCells)
        {
            int count = 0;
            foreach (Vector3Int neighbor in GetAdjacentCells(cell))
            {
                if (availableCells.Contains(neighbor))
                {
                    count++;
                }
            }

            return count;
        }

        private bool IsConnected(HashSet<Vector3Int> cells)
        {
            if (cells == null || cells.Count == 0)
            {
                return false;
            }

            return GetReachableCellCount(cells, cells) == cells.Count;
        }

        private List<List<Vector3Int>> GetConnectedComponents(HashSet<Vector3Int> cells)
        {
            var components = new List<List<Vector3Int>>();
            if (cells == null || cells.Count == 0)
            {
                return components;
            }

            var remaining = new HashSet<Vector3Int>(cells);
            while (remaining.Count > 0)
            {
                Vector3Int start = GetFirstCell(remaining);
                var component = new List<Vector3Int>();
                var queue = new Queue<Vector3Int>();
                queue.Enqueue(start);
                remaining.Remove(start);

                while (queue.Count > 0)
                {
                    Vector3Int current = queue.Dequeue();
                    component.Add(current);

                    foreach (Vector3Int neighbor in GetAdjacentCells(current))
                    {
                        if (!remaining.Remove(neighbor))
                        {
                            continue;
                        }

                        queue.Enqueue(neighbor);
                    }
                }

                components.Add(SortCells(component));
            }

            return components;
        }

        private List<Vector3Int> SortCells(IEnumerable<Vector3Int> cells)
        {
            return cells
                .OrderBy(cell => cell.x)
                .ThenBy(cell => cell.y)
                .ThenBy(cell => cell.z)
                .ToList();
        }

        private Vector3Int GetFirstCell(IEnumerable<Vector3Int> cells)
        {
            return cells
                .OrderBy(cell => cell.x)
                .ThenBy(cell => cell.y)
                .ThenBy(cell => cell.z)
                .First();
        }

        private IEnumerable<Vector3Int> GetAdjacentCells(Vector3Int cell)
        {
            yield return new Vector3Int(cell.x + 1, cell.y, cell.z);
            yield return new Vector3Int(cell.x - 1, cell.y, cell.z);
            yield return new Vector3Int(cell.x, cell.y + 1, cell.z);
            yield return new Vector3Int(cell.x, cell.y - 1, cell.z);
            yield return new Vector3Int(cell.x, cell.y, cell.z + 1);
            yield return new Vector3Int(cell.x, cell.y, cell.z - 1);
        }

        private Vector3Int GetCellwiseMinimum(IEnumerable<Vector3Int> cells)
        {
            int minX = int.MaxValue;
            int minY = int.MaxValue;
            int minZ = int.MaxValue;

            foreach (Vector3Int cell in cells)
            {
                if (cell.x < minX) minX = cell.x;
                if (cell.y < minY) minY = cell.y;
                if (cell.z < minZ) minZ = cell.z;
            }

            return new Vector3Int(minX, minY, minZ);
        }

        private void EvaluatePartitionCandidate(List<List<Vector3Int>> partitions, PartitionSearchState state)
        {
            state.ExploredSolutions++;

            int score = ScorePartitionCandidate(partitions);
            if (score == int.MinValue)
            {
                return;
            }

            if (state.BestCandidate != null && state.BestCandidate.Score >= score)
            {
                return;
            }

            List<List<Vector3Int>> snapshot = partitions
                .Select(partition => SortCells(partition))
                .ToList();
            state.BestCandidate = new PartitionCandidate(snapshot, score);
        }

        private int ScorePartitionCandidate(List<List<Vector3Int>> partitions)
        {
            if (partitions == null || partitions.Count == 0)
            {
                return int.MinValue;
            }

            List<int> pieceSizes = partitions.Select(partition => partition.Count).OrderByDescending(size => size).ToList();
            int score = EvaluatePieceSizeCandidate(pieceSizes);
            if (score == int.MinValue)
            {
                return int.MinValue;
            }

            bool containsTwoCellPiece = false;
            bool hasComplexLargePiece = false;
            bool hasIrregularPiece = false;
            var canonicalCounts = new Dictionary<string, int>();

            for (int i = 0; i < partitions.Count; i++)
            {
                PieceShapeProfile profile = AnalyzePieceShape(partitions[i]);
                containsTwoCellPiece |= profile.BlockCount == 2;
                hasComplexLargePiece |= profile.IsComplexLargePiece;
                hasIrregularPiece |= !profile.IsStraightLine && !profile.IsSolidPrism;

                if (!canonicalCounts.TryAdd(profile.CanonicalKey, 1))
                {
                    canonicalCounts[profile.CanonicalKey]++;
                }

                score += ScorePieceShape(profile);
            }

            if (!hasIrregularPiece)
            {
                return int.MinValue;
            }

            if (containsTwoCellPiece && !hasComplexLargePiece)
            {
                return int.MinValue;
            }

            foreach (KeyValuePair<string, int> entry in canonicalCounts)
            {
                if (entry.Value > 1)
                {
                    score -= (entry.Value - 1) * 6;
                }
            }

            return score;
        }

        private PieceShapeProfile AnalyzePieceShape(IEnumerable<Vector3Int> cells)
        {
            Vector3Int[] localBlocks = RotationUtil.NormalizeToOrigin(cells.ToArray());
            int maxX = 0;
            int maxY = 0;
            int maxZ = 0;
            for (int i = 0; i < localBlocks.Length; i++)
            {
                maxX = Mathf.Max(maxX, localBlocks[i].x);
                maxY = Mathf.Max(maxY, localBlocks[i].y);
                maxZ = Mathf.Max(maxZ, localBlocks[i].z);
            }

            int sizeX = maxX + 1;
            int sizeY = maxY + 1;
            int sizeZ = maxZ + 1;
            int varyingAxes = 0;
            if (sizeX > 1) varyingAxes++;
            if (sizeY > 1) varyingAxes++;
            if (sizeZ > 1) varyingAxes++;

            HashSet<Vector3Int> cellSet = new HashSet<Vector3Int>(localBlocks);
            int branchNodeCount = 0;
            for (int i = 0; i < localBlocks.Length; i++)
            {
                if (CountNeighbors(localBlocks[i], cellSet) >= 3)
                {
                    branchNodeCount++;
                }
            }

            string canonicalKey = string.Join(";", RotationUtil.GetCanonicalForm(localBlocks).Select(block => $"{block.x},{block.y},{block.z}"));
            return new PieceShapeProfile(
                localBlocks.Length,
                sizeX,
                sizeY,
                sizeZ,
                varyingAxes <= 1,
                sizeX * sizeY * sizeZ == localBlocks.Length,
                sizeY > 1,
                branchNodeCount,
                canonicalKey);
        }

        private int ScorePieceShape(PieceShapeProfile profile)
        {
            int score = 0;
            if (profile.BlockCount == 2)
            {
                score += 3;
            }

            if (!profile.IsStraightLine)
            {
                score += 4;
            }
            else
            {
                score -= 8;
            }

            if (!profile.IsSolidPrism)
            {
                score += 6;
            }
            else if (profile.BlockCount >= 4)
            {
                score -= 10;
            }

            if (profile.HasVerticalVariation)
            {
                score += 4;
            }

            score += profile.BranchNodeCount * 2;

            if (profile.BlockCount >= 5)
            {
                score += 4;
            }

            if (profile.IsComplexLargePiece)
            {
                score += 12;
            }

            return score;
        }

        private void BuildGeneratedPieces(
            List<List<Vector3Int>> partitions,
            out List<PieceDefinition> pieces,
            out List<SolutionPlacement> solutionPlacements)
        {
            pieces = new List<PieceDefinition>(partitions.Count);
            solutionPlacements = new List<SolutionPlacement>(partitions.Count);

            for (int i = 0; i < partitions.Count; i++)
            {
                List<Vector3Int> cells = SortCells(partitions[i]);
                Vector3Int origin = GetCellwiseMinimum(cells);
                Vector3Int[] localBlocks = cells
                    .Select(cell => cell - origin)
                    .OrderBy(cell => cell.x)
                    .ThenBy(cell => cell.y)
                    .ThenBy(cell => cell.z)
                    .ToArray();

                pieces.Add(new PieceDefinition(
                    $"generated_{i + 1}",
                    $"Generated-{i + 1}",
                    localBlocks,
                    GetGeneratedPieceColor(i),
                    0));

                solutionPlacements.Add(new SolutionPlacement(i, 0, origin));
            }
        }

        private Color GetGeneratedPieceColor(int index)
        {
            if (allPieces != null && allPieces.Length > 0)
            {
                return allPieces[index % allPieces.Length].DefaultColor;
            }

            return Color.white;
        }

        /// <summary>
        /// Selects pieces based on difficulty configuration.
        /// </summary>
        private List<PieceDefinition> SelectPiecesForDifficulty(LevelDifficultyConfig config, int targetBlocks)
        {
            var selected = new List<PieceDefinition>();
            var availablePieces = new List<PieceDefinition>(allPieces);
            int currentBlocks = 0;

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
            List<TargetArea> candidates = BuildAutomaticTargetAreas(difficulty);
            for (int i = 0; i < candidates.Count; i++)
            {
                if (candidates[i].FootprintSize == footprintSize)
                {
                    return candidates[i];
                }
            }

            return candidates.Count > 0 ? candidates[0] : null;
        }

        /// <summary>
        /// Creates varied target areas for harder difficulties.
        /// </summary>
        private TargetArea CreateVariedTargetArea(int footprintSize)
        {
            return CalculateTargetArea(footprintSize, DifficultyLevel.Hard);
        }

        private TargetArea CreateRectangularArea(int footprintSize)
        {
            return CalculateTargetArea(footprintSize, DifficultyLevel.Easy);
        }

        private TargetArea CreateLShapedArea(int footprintSize)
        {
            return CalculateTargetArea(footprintSize, DifficultyLevel.Hard);
        }

        private TargetArea CreateTShapedArea(int footprintSize)
        {
            return CalculateTargetArea(footprintSize, DifficultyLevel.Hard);
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

            var board = new bool[targetArea.Width + 4, TargetArea.RequiredHeight, targetArea.Depth + 4];
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
                    for (int y = 0; y < TargetArea.RequiredHeight; y++)
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
                    worldPos.y < 0 || worldPos.y >= TargetArea.RequiredHeight ||
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
                TargetArea.RequiredHeight,
                targetArea.Depth
            );
        }

        /// <summary>
        /// Determines difficulty level based on level number.
        /// </summary>
        private DifficultyLevel GetDifficultyForLevel(int levelNumber)
        {
            if (levelNumber <= 4)
            {
                return DifficultyLevel.Easy;
            }

            return DifficultyLevel.Hard;
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

            Debug.Log($"[RoundFlow][F{Time.frameCount}] LevelGenerator.SpawnPieces: spawned {pieces.Count} pieces, currentPieces.Count={currentPieces.Count}");
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
            Debug.Log($"[RoundFlow][F{Time.frameCount}] LevelGenerator.ClearCurrentPieces: count={currentPieces.Count}");
            for (int i = 0; i < currentPieces.Count; i++)
            {
                GameObject piece = currentPieces[i];
                Debug.Log($"[RoundFlow][F{Time.frameCount}] LevelGenerator.ClearCurrentPieces: destroying piece[{i}] name={piece?.name ?? "null"}, id={piece?.GetInstanceID() ?? 0}, isNull={piece == null}");
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
                int j = UnityEngine.Random.Range(0, i + 1);
                T temp = list[i];
                list[i] = list[j];
                list[j] = temp;
            }
        }
    }
}
