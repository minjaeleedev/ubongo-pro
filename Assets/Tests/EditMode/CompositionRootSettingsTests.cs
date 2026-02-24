using NUnit.Framework;
using UnityEngine;
using Ubongo.Application.Bootstrap;
using Ubongo.Domain;
using Ubongo.Infrastructure.Settings;
using Ubongo.Systems;

namespace Ubongo.Tests.EditMode
{
    public class CompositionRootSettingsTests
    {
        private const string TestSettingsKey = "Ubongo.Tests.EditMode.SettingsStore.BoolKey";

        [SetUp]
        public void SetUp()
        {
            CleanupRuntimeSingletonObjects();
            PlayerPrefs.DeleteKey(TestSettingsKey);
        }

        [TearDown]
        public void TearDown()
        {
            PlayerPrefs.DeleteKey(TestSettingsKey);
            CleanupRuntimeSingletonObjects();
        }

        [Test]
        public void PlayerPrefsSettingsStore_GetSetBool_Works()
        {
            ISettingsStore store = new PlayerPrefsSettingsStore();

            Assert.IsFalse(store.GetBool(TestSettingsKey, false));

            store.SetBool(TestSettingsKey, true);
            store.Save();
            Assert.IsTrue(store.GetBool(TestSettingsKey, false));

            store.SetBool(TestSettingsKey, false);
            store.Save();
            Assert.IsFalse(store.GetBool(TestSettingsKey, true));
        }

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

        private static void CleanupRuntimeSingletonObjects()
        {
            DestroyAllComponents<GameCompositionRoot>();
            DestroyAllComponents<GameManager>();
            DestroyAllComponents<GameBoard>();
            DestroyAllComponents<InputManager>();
            DestroyAllComponents<LevelGenerator>();
            DestroyAllComponents<RoundManager>();
            DestroyAllComponents<GemSystem>();
            DestroyAllComponents<DifficultySystem>();
            DestroyAllComponents<TiebreakerManager>();
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

        private sealed class InMemorySettingsStore : ISettingsStore
        {
            public bool StoredValue { get; private set; }
            public int SaveCallCount { get; private set; }
            private bool hasStoredValue;

            public bool GetBool(string key, bool defaultValue)
            {
                return hasStoredValue ? StoredValue : defaultValue;
            }

            public void SetBool(string key, bool value)
            {
                StoredValue = value;
                hasStoredValue = true;
            }

            public void Save()
            {
                SaveCallCount++;
            }
        }
    }
}
