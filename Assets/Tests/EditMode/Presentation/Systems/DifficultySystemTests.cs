using NUnit.Framework;
using UnityEngine;
using Ubongo.Domain;
using Ubongo.Systems;

namespace Ubongo.Tests.EditMode.Presentation.Systems
{
    public class DifficultySystemTests
    {
        [SetUp]
        public void SetUp()
        {
            DestroyAllDifficultySystems();
        }

        [TearDown]
        public void TearDown()
        {
            DestroyAllDifficultySystems();
        }

        [Test]
        public void SetDifficulty_WhenInvalidValue_FallsBackToEasy()
        {
            GameObject difficultyObject = new GameObject("DifficultySystem_Test");
            DifficultySystem difficultySystem = difficultyObject.AddComponent<DifficultySystem>();

            difficultySystem.SetDifficulty((DifficultyLevel)0);

            Assert.AreEqual(DifficultyLevel.Easy, difficultySystem.CurrentDifficulty);

            UnityEngine.Object.DestroyImmediate(difficultyObject);
        }

        [Test]
        public void FromInt_WhenValueIsOutsideDefinedRange_FallsBackToEasy()
        {
            Assert.AreEqual(DifficultyLevel.Easy, DifficultySystem.FromInt(0));
            Assert.AreEqual(DifficultyLevel.Easy, DifficultySystem.FromInt(99));
        }

        [Test]
        public void GetDifficultyConfig_Hard_UsesFourPieces()
        {
            GameObject difficultyObject = new GameObject("DifficultySystem_Test");
            DifficultySystem difficultySystem = difficultyObject.AddComponent<DifficultySystem>();

            DifficultyConfig config = difficultySystem.GetDifficultyConfig(DifficultyLevel.Hard);

            Assert.AreEqual(DifficultyLevel.Hard, config.Level);
            Assert.AreEqual(4, config.PieceCount);

            UnityEngine.Object.DestroyImmediate(difficultyObject);
        }

        private static void DestroyAllDifficultySystems()
        {
            DifficultySystem[] systems = UnityEngine.Object.FindObjectsByType<DifficultySystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (DifficultySystem difficultySystem in systems)
            {
                if (difficultySystem == null)
                {
                    continue;
                }

                UnityEngine.Object.DestroyImmediate(difficultySystem.gameObject);
            }
        }
    }
}
