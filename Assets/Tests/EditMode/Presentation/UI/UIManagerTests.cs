using NUnit.Framework;
using UnityEngine;
using Ubongo.Domain;
using Ubongo.Systems;
using Ubongo.Tests.EditMode.Shared;

namespace Ubongo.Tests.EditMode.Presentation.UI
{
    public class UIManagerTests
    {
        [SetUp]
        public void SetUp()
        {
            SceneTestHelper.CleanupRuntimeSingletonObjects();
            PlayerPrefs.DeleteKey("TotalGems");
        }

        [TearDown]
        public void TearDown()
        {
            PlayerPrefs.DeleteKey("TotalGems");
            SceneTestHelper.CleanupRuntimeSingletonObjects();
        }

        [Test]
        public void SetDifficulty_UpdatesCurrentDifficulty()
        {
            GameObject uiObject = new GameObject("UIManager_Test");
            UIManager uiManager = uiObject.AddComponent<UIManager>();

            uiManager.SetDifficulty(DifficultyLevel.Hard);
            Assert.AreEqual(DifficultyLevel.Hard, uiManager.CurrentDifficulty);

            UnityEngine.Object.DestroyImmediate(uiObject);
        }

        [Test]
        public void SetDifficulty_WhenInvalidValue_FallsBackToEasy()
        {
            GameObject uiObject = new GameObject("UIManager_Test");
            UIManager uiManager = uiObject.AddComponent<UIManager>();

            uiManager.SetDifficulty((DifficultyLevel)0);

            Assert.AreEqual(DifficultyLevel.Easy, uiManager.CurrentDifficulty);

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
    }
}
