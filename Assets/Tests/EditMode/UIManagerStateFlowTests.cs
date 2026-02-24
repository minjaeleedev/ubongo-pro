using NUnit.Framework;
using UnityEngine;
using Ubongo.Domain;
using Ubongo.Systems;

namespace Ubongo.Tests.EditMode
{
    public class UIManagerStateFlowTests
    {
        [SetUp]
        public void SetUp()
        {
            CleanupRuntimeSingletonObjects();
            PlayerPrefs.DeleteKey("TotalGems");
        }

        [TearDown]
        public void TearDown()
        {
            PlayerPrefs.DeleteKey("TotalGems");
            CleanupRuntimeSingletonObjects();
        }

        [Test]
        public void SetDifficulty_UpdatesCurrentDifficulty()
        {
            GameObject uiObject = new GameObject("UIManager_Test");
            UIManager uiManager = uiObject.AddComponent<UIManager>();

            uiManager.SetDifficulty(DifficultyLevel.Expert);
            Assert.AreEqual(DifficultyLevel.Expert, uiManager.CurrentDifficulty);

            UnityEngine.Object.DestroyImmediate(uiObject);
        }

        [Test]
        public void NextRound_AndResetRounds_UpdatePublicRoundState()
        {
            GameObject uiObject = new GameObject("UIManager_Test");
            UIManager uiManager = uiObject.AddComponent<UIManager>();

            Assert.AreEqual(1, uiManager.CurrentRound);
            Assert.IsFalse(uiManager.IsLastRound());

            for (int i = 0; i < 8; i++)
            {
                uiManager.NextRound();
            }

            Assert.AreEqual(9, uiManager.CurrentRound);
            Assert.IsTrue(uiManager.IsLastRound());

            uiManager.ResetRounds();
            Assert.AreEqual(1, uiManager.CurrentRound);
            Assert.IsFalse(uiManager.IsLastRound());

            UnityEngine.Object.DestroyImmediate(uiObject);
        }

        [Test]
        public void AddGems_AndUseGems_UpdateCurrentAndTotalCounters()
        {
            GameObject uiObject = new GameObject("UIManager_Test");
            UIManager uiManager = uiObject.AddComponent<UIManager>();

            Assert.AreEqual(0, uiManager.CurrentGems);
            Assert.AreEqual(0, uiManager.TotalGems);

            uiManager.AddGems(5);
            Assert.AreEqual(5, uiManager.CurrentGems);
            Assert.AreEqual(5, uiManager.TotalGems);

            uiManager.UseGems(2);
            Assert.AreEqual(3, uiManager.CurrentGems);
            Assert.AreEqual(3, uiManager.TotalGems);

            uiManager.UseGems(10);
            Assert.AreEqual(3, uiManager.CurrentGems);
            Assert.AreEqual(3, uiManager.TotalGems);

            UnityEngine.Object.DestroyImmediate(uiObject);
        }

        private static void CleanupRuntimeSingletonObjects()
        {
            DestroyAllComponents<GameManager>();
            DestroyAllComponents<GameBoard>();
            DestroyAllComponents<InputManager>();
            DestroyAllComponents<LevelGenerator>();
            DestroyAllComponents<RoundManager>();
            DestroyAllComponents<GemSystem>();
            DestroyAllComponents<DifficultySystem>();
            DestroyAllComponents<TiebreakerManager>();
            DestroyAllComponents<UIManager>();
        }

        private static void DestroyAllComponents<T>() where T : Component
        {
            T[] components = UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (T component in components)
            {
                if (component == null)
                {
                    continue;
                }

                UnityEngine.Object.DestroyImmediate(component.gameObject);
            }
        }
    }
}
