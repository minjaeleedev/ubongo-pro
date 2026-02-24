using NUnit.Framework;
using UnityEngine;
using Ubongo.Core;
using Ubongo.Domain;
using System.Collections.Generic;

namespace Ubongo.Tests.EditMode
{
    public class LevelGeneratorContractTests
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

            UnityEngine.Object.DestroyImmediate(generatorObject);
        }

        [Test]
        public void SpawnFromLevelData_WithNullPayload_DoesNotMutateCurrentSpawn()
        {
            GameObject generatorObject = new GameObject("LevelGenerator_Test");
            LevelGenerator generator = generatorObject.AddComponent<LevelGenerator>();
            LevelData levelData = CreateManualLevelData(DifficultyLevel.Medium, 2);
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
            LevelData levelData = CreateManualLevelData(DifficultyLevel.Medium, 2);
            generator.SpawnFromLevelData(levelData);
            int pieceCountBefore = CountPuzzlePiecesInScene();

            LevelData emptyPayload = new LevelData
            {
                LevelNumber = 99,
                Difficulty = DifficultyLevel.Hard,
                TimeLimit = 30f,
                Pieces = new List<PieceDefinition>(),
                BoardSize = new Vector3Int(2, 2, 1),
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

        private static LevelData CreateManualLevelData(DifficultyLevel difficulty, int levelNumber)
        {
            return new LevelData
            {
                LevelNumber = levelNumber,
                Difficulty = difficulty,
                TimeLimit = LevelDifficultyConfig.GetConfig(difficulty).TimeLimit,
                Pieces = new List<PieceDefinition> { PieceCatalog.Tower },
                BoardSize = new Vector3Int(2, 2, 1),
                TargetArea = TargetArea.CreateRectangular(2, 1),
                SolutionPlacements = new List<SolutionPlacement>()
            };
        }

        private static int CountPuzzlePiecesInScene()
        {
            PuzzlePiece[] pieces = UnityEngine.Object.FindObjectsByType<PuzzlePiece>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            return pieces.Length;
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
