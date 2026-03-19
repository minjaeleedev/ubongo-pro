using NUnit.Framework;
using UnityEngine;
using Ubongo.Core;
using Ubongo.Domain;
using Ubongo.Domain.Board;
using System.Collections.Generic;
using System.Linq;

namespace Ubongo.Tests.EditMode.Presentation.Managers
{
    public class LevelGeneratorTests
    {
        [SetUp]
        public void SetUp()
        {
            DestroyAllLevelGenerators();
        }

        [TearDown]
        public void TearDown()
        {
            DestroyAllLevelGenerators();
        }

        [Test]
        public void SpawnFromLevelData_WithValidPayload_UpdatesCurrentLevelData()
        {
            GameObject generatorObject = new GameObject("LevelGenerator_Test");
            LevelGenerator generator = generatorObject.AddComponent<LevelGenerator>();
            LevelData levelData = CreateManualLevelData(DifficultyLevel.Easy, 1);

            generator.SpawnFromLevelData(levelData);

            Assert.AreSame(levelData, generator.CurrentLevelData);
            Assert.AreEqual(DifficultyLevel.Easy, generator.CurrentLevelData.Difficulty);
            Assert.IsNotNull(generator.CurrentLevelData.Pieces);
            Assert.Greater(generator.CurrentLevelData.Pieces.Count, 0);
            Assert.AreEqual(TargetArea.RequiredHeight, generator.CurrentLevelData.BoardSize.y);

            UnityEngine.Object.DestroyImmediate(generatorObject);
        }

        [Test]
        public void SpawnFromLevelData_WithNullPayload_DoesNotMutateCurrentSpawn()
        {
            GameObject generatorObject = new GameObject("LevelGenerator_Test");
            LevelGenerator generator = generatorObject.AddComponent<LevelGenerator>();
            LevelData levelData = CreateManualLevelData(DifficultyLevel.Hard, 2);
            generator.SpawnFromLevelData(levelData);
            int pieceCountBefore = CountPuzzlePiecesInScene();
            bool hadBoundsBefore = generator.TryGetSpawnedPiecesBounds(out Bounds boundsBefore);

            generator.SpawnFromLevelData(null);

            Assert.AreSame(levelData, generator.CurrentLevelData);
            Assert.AreEqual(pieceCountBefore, CountPuzzlePiecesInScene());
            Assert.AreEqual(hadBoundsBefore, generator.TryGetSpawnedPiecesBounds(out Bounds boundsAfter));
            if (hadBoundsBefore)
            {
                Assert.AreEqual(boundsBefore.center, boundsAfter.center);
                Assert.AreEqual(boundsBefore.size, boundsAfter.size);
            }

            UnityEngine.Object.DestroyImmediate(generatorObject);
        }

        [Test]
        public void SpawnFromLevelData_WithEmptyPayload_DoesNotMutateCurrentSpawn()
        {
            GameObject generatorObject = new GameObject("LevelGenerator_Test");
            LevelGenerator generator = generatorObject.AddComponent<LevelGenerator>();
            LevelData levelData = CreateManualLevelData(DifficultyLevel.Hard, 2);
            generator.SpawnFromLevelData(levelData);
            int pieceCountBefore = CountPuzzlePiecesInScene();

            LevelData emptyPayload = new LevelData
            {
                LevelNumber = 99,
                Difficulty = DifficultyLevel.Hard,
                TimeLimit = 30f,
                Pieces = new List<PieceDefinition>(),
                BoardSize = new Vector3Int(2, TargetArea.RequiredHeight, 1),
                TargetArea = TargetArea.CreateRectangular(2, 1),
                SolutionPlacements = new List<SolutionPlacement>()
            };

            generator.SpawnFromLevelData(emptyPayload);

            Assert.AreSame(levelData, generator.CurrentLevelData);
            Assert.AreEqual(pieceCountBefore, CountPuzzlePiecesInScene());
            Assert.IsTrue(generator.TryGetSpawnedPiecesBounds(out _));

            UnityEngine.Object.DestroyImmediate(generatorObject);
        }

        [Test]
        public void ClearSpawnedPieces_WithExistingSpawn_RemovesOnlySpawnedObjects()
        {
            int pieceCountBaseline = CountPuzzlePiecesInScene();
            GameObject generatorObject = new GameObject("LevelGenerator_Test");
            LevelGenerator generator = generatorObject.AddComponent<LevelGenerator>();
            LevelData levelData = CreateManualLevelData(DifficultyLevel.Easy, 1);

            generator.SpawnFromLevelData(levelData);
            Assert.Greater(CountPuzzlePiecesInScene(), pieceCountBaseline);
            Assert.IsTrue(generator.TryGetSpawnedPiecesBounds(out _));

            generator.ClearSpawnedPieces();

            Assert.AreEqual(pieceCountBaseline, CountPuzzlePiecesInScene());
            Assert.IsFalse(generator.TryGetSpawnedPiecesBounds(out _));
            Assert.AreSame(levelData, generator.CurrentLevelData);

            UnityEngine.Object.DestroyImmediate(generatorObject);
        }

        [Test]
        public void CurrentLevelData_DefaultState_IsNull()
        {
            GameObject generatorObject = new GameObject("LevelGenerator_Test");
            LevelGenerator generator = generatorObject.AddComponent<LevelGenerator>();

            Assert.IsNull(generator.CurrentLevelData);
            UnityEngine.Object.DestroyImmediate(generatorObject);
        }

        [Test]
        public void TryCreateLevelData_Easy_UsesConfiguredFootprintAndPieceCount()
        {
            GameObject generatorObject = new GameObject("LevelGenerator_Test");
            LevelGenerator generator = generatorObject.AddComponent<LevelGenerator>();

            bool generated = generator.TryCreateLevelData(1, DifficultyLevel.Easy, out LevelData levelData);

            Assert.IsTrue(generated, "Expected level generation to succeed for Easy difficulty.");
            Assert.IsNotNull(levelData);
            Assert.AreEqual(TargetArea.RequiredHeight, levelData.BoardSize.y);
            Assert.That(levelData.TargetArea.FootprintSize, Is.InRange(6, 7));
            Assert.AreEqual(3, levelData.Pieces.Count);
            Assert.AreEqual(levelData.TargetArea.TotalCells, levelData.Pieces.Sum(p => p.BlockCount));
            Assert.IsFalse(IsSingleRowOrColumn(levelData.TargetArea));
            Assert.Greater(levelData.Pieces.Select(piece => piece.BlockCount).Distinct().Count(), 1);
            AssertSolutionExactlyFillsTarget(levelData);

            UnityEngine.Object.DestroyImmediate(generatorObject);
        }

        [Test]
        public void TryCreateLevelData_Hard_UsesEightFootprintCellsAndFourPieces()
        {
            GameObject generatorObject = new GameObject("LevelGenerator_Test");
            LevelGenerator generator = generatorObject.AddComponent<LevelGenerator>();

            bool generated = generator.TryCreateLevelData(1, DifficultyLevel.Hard, out LevelData levelData);

            Assert.IsTrue(generated, "Expected level generation to succeed for Hard difficulty.");
            Assert.IsNotNull(levelData);
            Assert.AreEqual(TargetArea.RequiredHeight, levelData.BoardSize.y);
            Assert.AreEqual(8, levelData.TargetArea.FootprintSize);
            Assert.AreEqual(4, levelData.Pieces.Count);

            int totalBlocks = levelData.Pieces.Sum(piece => piece.BlockCount);
            Assert.AreEqual(16, totalBlocks);
            Assert.AreEqual(0, totalBlocks % TargetArea.RequiredHeight);
            Assert.AreEqual(levelData.TargetArea.TotalCells, totalBlocks);
            AssertSolutionExactlyFillsTarget(levelData);

            UnityEngine.Object.DestroyImmediate(generatorObject);
        }

        [Test]
        public void TryCreateLevelData_WithCustomFootprintAndThreePieces_FillsTwoLayersWithoutGaps()
        {
            GameObject generatorObject = new GameObject("LevelGenerator_Test");
            LevelGenerator generator = generatorObject.AddComponent<LevelGenerator>();
            TargetArea targetArea = CreateTargetAreaFromRows(
                "xx.",
                "xxx",
                "xx.");

            bool generated = generator.TryCreateLevelData(1, DifficultyLevel.Easy, targetArea, 3, out LevelData levelData);

            Assert.IsTrue(generated, "Expected custom target generation to succeed.");
            Assert.IsNotNull(levelData);
            Assert.AreEqual(3, levelData.Pieces.Count);
            Assert.AreEqual(targetArea.TotalCells, levelData.Pieces.Sum(piece => piece.BlockCount));
            Assert.AreEqual(targetArea.Width, levelData.BoardSize.x);
            Assert.AreEqual(TargetArea.RequiredHeight, levelData.BoardSize.y);
            Assert.AreEqual(targetArea.Depth, levelData.BoardSize.z);
            CollectionAssert.AreEquivalent(targetArea.GetAllCells(), levelData.TargetArea.GetAllCells());
            AssertSolutionExactlyFillsTarget(levelData);

            UnityEngine.Object.DestroyImmediate(generatorObject);
        }

        [Test]
        public void TryCreateLevelData_WhenTwoCellPieceExists_IncludesComplexLargePiece()
        {
            GameObject generatorObject = new GameObject("LevelGenerator_Test");
            LevelGenerator generator = generatorObject.AddComponent<LevelGenerator>();
            TargetArea targetArea = CreateTargetAreaFromRows(
                "x..",
                "xx.",
                "xxx");

            Assert.IsTrue(generator.TryCreateLevelData(1, DifficultyLevel.Easy, targetArea, 3, out LevelData levelData));
            Assert.IsTrue(levelData.Pieces.Any(piece => piece.BlockCount == 2));
            Assert.IsTrue(levelData.Pieces.Any(piece => piece.BlockCount >= 5 && IsComplexPiece(piece.Blocks)));
            UnityEngine.Object.DestroyImmediate(generatorObject);
        }

        private static LevelData CreateManualLevelData(DifficultyLevel difficulty, int levelNumber)
        {
            return new LevelData
            {
                LevelNumber = levelNumber,
                Difficulty = difficulty,
                TimeLimit = LevelDifficultyConfig.GetConfig(difficulty).TimeLimit,
                Pieces = new List<PieceDefinition> { PieceCatalog.Tower },
                BoardSize = new Vector3Int(2, TargetArea.RequiredHeight, 1),
                TargetArea = TargetArea.CreateRectangular(2, 1),
                SolutionPlacements = new List<SolutionPlacement>()
            };
        }

        private static int CountPuzzlePiecesInScene()
        {
            PuzzlePiece[] pieces = UnityEngine.Object.FindObjectsByType<PuzzlePiece>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            return pieces.Length;
        }

        private static TargetArea CreateTargetAreaFromRows(params string[] rows)
        {
            Assert.IsNotNull(rows);
            Assert.Greater(rows.Length, 0);

            int width = rows[0].Length;
            bool[,] mask = new bool[width, rows.Length];

            for (int z = 0; z < rows.Length; z++)
            {
                Assert.AreEqual(width, rows[z].Length);
                for (int x = 0; x < width; x++)
                {
                    mask[x, z] = rows[z][x] == 'x';
                }
            }

            return TargetArea.CreateFromMask(mask);
        }

        private static bool IsSingleRowOrColumn(TargetArea targetArea)
        {
            List<Vector2Int> columns = targetArea.GetColumnPositions().ToList();
            return columns.All(column => column.x == columns[0].x) ||
                   columns.All(column => column.y == columns[0].y);
        }

        private static bool IsComplexPiece(IEnumerable<Vector3Int> blocks)
        {
            Vector3Int[] normalized = RotationUtil.NormalizeToOrigin(blocks.ToArray());
            int maxX = normalized.Max(block => block.x) + 1;
            int maxY = normalized.Max(block => block.y) + 1;
            int maxZ = normalized.Max(block => block.z) + 1;
            int varyingAxes = 0;
            if (maxX > 1) varyingAxes++;
            if (maxY > 1) varyingAxes++;
            if (maxZ > 1) varyingAxes++;

            bool isStraightLine = varyingAxes <= 1;
            bool isSolidPrism = maxX * maxY * maxZ == normalized.Length;
            return normalized.Length >= 5 && !isStraightLine && !isSolidPrism;
        }

        private static void AssertSolutionExactlyFillsTarget(LevelData levelData)
        {
            Assert.IsNotNull(levelData);
            Assert.IsNotNull(levelData.TargetArea);
            Assert.IsNotNull(levelData.Pieces);
            Assert.IsNotNull(levelData.SolutionPlacements);
            Assert.AreEqual(levelData.Pieces.Count, levelData.SolutionPlacements.Count);

            BoardState board = new BoardState(levelData.BoardSize.x, levelData.BoardSize.y, levelData.BoardSize.z);
            PuzzleValidator validator = new PuzzleValidator();

            for (int i = 0; i < levelData.SolutionPlacements.Count; i++)
            {
                SolutionPlacement placement = levelData.SolutionPlacements[i];
                PieceDefinition piece = levelData.Pieces[placement.PieceIndex];
                Vector3Int[] rotatedBlocks = RotationUtil.RotatePiece(piece.Blocks, placement.RotationIndex);
                List<Vector3Int> worldCells = rotatedBlocks
                    .Select(block => placement.Position + block)
                    .ToList();

                ValidationResult placementResult = validator.ValidatePlacement(
                    worldCells,
                    board.CreateOccupancySnapshot(),
                    levelData.TargetArea);

                Assert.IsTrue(placementResult.IsValid, $"Expected piece {placement.PieceIndex} placement to be valid.");
                Assert.IsTrue(board.TryPlace($"piece_{i}", worldCells), $"Expected piece {placement.PieceIndex} to place without collisions.");
            }

            BoardWinConditionService winConditionService = new BoardWinConditionService();
            ValidationResult result = winConditionService.ValidateSolution(board, levelData.TargetArea);
            Assert.IsTrue(result.IsSolved, "Expected generated solution to fill the full target area.");
        }

        private static void DestroyAllLevelGenerators()
        {
            LevelGenerator[] generators = UnityEngine.Object.FindObjectsByType<LevelGenerator>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (LevelGenerator generator in generators)
            {
                if (generator == null)
                {
                    continue;
                }

                UnityEngine.Object.DestroyImmediate(generator.gameObject);
            }
        }
    }
}
