using NUnit.Framework;
using UnityEngine;
using Ubongo.Domain;
using Ubongo.Tests.EditMode.Shared;

namespace Ubongo.Tests.EditMode.Presentation.Managers
{
    public class GameManagerTests
    {
        [Test]
        public void GameManager_Initialize_IsIdempotent_AndKeepsFirstInjectedStore()
        {
            GameObject managerObject = new GameObject("GameManager_Test");
            GameManager manager = managerObject.AddComponent<GameManager>();
            InMemorySettingsStore firstStore = new InMemorySettingsStore();
            InMemorySettingsStore secondStore = new InMemorySettingsStore();

            manager.Initialize(firstStore);
            manager.Initialize(secondStore);
            manager.SetShowSolutionOnTimeout(true);

            Assert.IsTrue(firstStore.StoredValue);
            Assert.AreEqual(1, firstStore.SaveCallCount);
            Assert.IsFalse(secondStore.StoredValue);
            Assert.AreEqual(0, secondStore.SaveCallCount);

            UnityEngine.Object.DestroyImmediate(managerObject);
        }

        [Test]
        public void GameManager_SetGameMode_MultiplayerRequest_NormalizesToClassic()
        {
            GameObject managerObject = new GameObject("GameManager_Test");
            GameManager manager = managerObject.AddComponent<GameManager>();

            manager.SetGameMode(GameMode.Multiplayer);

            Assert.AreEqual(GameMode.Classic, manager.CurrentMode);
            UnityEngine.Object.DestroyImmediate(managerObject);
        }

        [Test]
        public void GameManager_SetGameMode_EmitsOnlyWhenEffectiveModeChanges()
        {
            GameObject managerObject = new GameObject("GameManager_Test");
            GameManager manager = managerObject.AddComponent<GameManager>();
            manager.SetGameMode(GameMode.Classic);
            int eventCount = 0;
            GameMode lastMode = GameMode.Classic;

            manager.OnGameModeChanged += mode =>
            {
                eventCount++;
                lastMode = mode;
            };

            manager.SetGameMode(GameMode.Classic);
            manager.SetGameMode(GameMode.Zen);
            manager.SetGameMode(GameMode.Multiplayer);
            manager.SetGameMode(GameMode.Multiplayer);

            Assert.AreEqual(2, eventCount);
            Assert.AreEqual(GameMode.Classic, lastMode);
            Assert.AreEqual(GameMode.Classic, manager.CurrentMode);
            UnityEngine.Object.DestroyImmediate(managerObject);
        }

        [Test]
        public void GameManager_TryGetExistingInstance_WhenMissing_ReturnsFalse()
        {
            bool found = GameManager.TryGetExistingInstance(out GameManager manager);

            Assert.IsFalse(found);
            Assert.IsNull(manager);
        }

        [Test]
        public void CalculateOrthographicSizeFromBounds_IncreasesWithPadding()
        {
            Bounds bounds = new Bounds(Vector3.zero, new Vector3(10f, 2f, 4f));
            Quaternion rotation = Quaternion.Euler(35.264f, 45f, 0f);

            float withoutPadding = GameManager.CalculateOrthographicSizeFromBounds(bounds, rotation, 16f / 9f, 0f);
            float withPadding = GameManager.CalculateOrthographicSizeFromBounds(bounds, rotation, 16f / 9f, 1f);

            Assert.Greater(withPadding, withoutPadding);
        }

        [Test]
        public void CalculateOrthographicSizeFromBounds_NarrowAspectNeedsLargerSize()
        {
            Bounds bounds = new Bounds(Vector3.zero, new Vector3(8f, 2f, 6f));
            Quaternion rotation = Quaternion.Euler(35.264f, 45f, 0f);

            float wideSize = GameManager.CalculateOrthographicSizeFromBounds(bounds, rotation, 16f / 9f, 0f);
            float narrowSize = GameManager.CalculateOrthographicSizeFromBounds(bounds, rotation, 9f / 16f, 0f);

            Assert.Greater(narrowSize, wideSize);
        }
    }
}
