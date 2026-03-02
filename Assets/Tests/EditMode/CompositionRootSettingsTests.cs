using NUnit.Framework;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.TestTools;
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

        [Test]
        public void GameManager_TryGetExistingInstance_WhenMissing_ReturnsFalse()
        {
            bool found = GameManager.TryGetExistingInstance(out GameManager manager);

            Assert.IsFalse(found);
            Assert.IsNull(manager);
        }

        [Test]
        public void RoundManager_TryFailCurrentRound_WhenNotInProgress_ReturnsFalse()
        {
            GameObject roundObject = new GameObject("RoundManager_Test");
            RoundManager roundManager = roundObject.AddComponent<RoundManager>();

            bool failed = roundManager.TryFailCurrentRound();

            Assert.IsFalse(failed);
            UnityEngine.Object.DestroyImmediate(roundObject);
        }

        [Test]
        public void GameCompositionRoot_Awake_WithoutUIManager_FailsFast()
        {
            GameObject managerObject = new GameObject("GameManager_Test");
            managerObject.AddComponent<GameManager>();
            GameObject roundObject = new GameObject("RoundManager_Test");
            roundObject.AddComponent<RoundManager>();
            GameObject gemObject = new GameObject("GemSystem_Test");
            gemObject.AddComponent<GemSystem>();
            GameObject difficultyObject = new GameObject("DifficultySystem_Test");
            difficultyObject.AddComponent<DifficultySystem>();
            GameObject tiebreakerObject = new GameObject("TiebreakerManager_Test");
            tiebreakerObject.AddComponent<TiebreakerManager>();
            GameObject inputObject = new GameObject("InputManager_Test");
            inputObject.AddComponent<InputManager>();
            GameObject levelGeneratorObject = new GameObject("LevelGenerator_Test");
            levelGeneratorObject.AddComponent<LevelGenerator>();

            LogAssert.Expect(LogType.Error, "[GameCompositionRoot] Expected exactly one UIManager in scene, but found 0.");
            LogAssert.Expect(LogType.Exception, new Regex(@"\[GameCompositionRoot\] Runtime graph validation failed"));

            GameObject rootObject = new GameObject("GameCompositionRoot_Test");
            rootObject.AddComponent<GameCompositionRoot>();

            UnityEngine.Object.DestroyImmediate(rootObject);
            UnityEngine.Object.DestroyImmediate(levelGeneratorObject);
            UnityEngine.Object.DestroyImmediate(inputObject);
            UnityEngine.Object.DestroyImmediate(tiebreakerObject);
            UnityEngine.Object.DestroyImmediate(difficultyObject);
            UnityEngine.Object.DestroyImmediate(gemObject);
            UnityEngine.Object.DestroyImmediate(roundObject);
            UnityEngine.Object.DestroyImmediate(managerObject);
        }

        [Test]
        public void GameCompositionRoot_Awake_WithoutGameBoard_FailsFast()
        {
            GameObject managerObject = new GameObject("GameManager_Test");
            managerObject.AddComponent<GameManager>();
            GameObject roundObject = new GameObject("RoundManager_Test");
            roundObject.AddComponent<RoundManager>();
            GameObject gemObject = new GameObject("GemSystem_Test");
            gemObject.AddComponent<GemSystem>();
            GameObject difficultyObject = new GameObject("DifficultySystem_Test");
            difficultyObject.AddComponent<DifficultySystem>();
            GameObject tiebreakerObject = new GameObject("TiebreakerManager_Test");
            tiebreakerObject.AddComponent<TiebreakerManager>();
            GameObject inputObject = new GameObject("InputManager_Test");
            inputObject.AddComponent<InputManager>();
            GameObject levelGeneratorObject = new GameObject("LevelGenerator_Test");
            levelGeneratorObject.AddComponent<LevelGenerator>();
            GameObject uiObject = new GameObject("UIManager_Test");
            uiObject.AddComponent<UIManager>();

            LogAssert.Expect(LogType.Error, "[GameCompositionRoot] Expected exactly one GameBoard in scene, but found 0.");
            LogAssert.Expect(LogType.Exception, new Regex(@"\[GameCompositionRoot\] Runtime graph validation failed"));

            GameObject rootObject = new GameObject("GameCompositionRoot_Test");
            rootObject.AddComponent<GameCompositionRoot>();

            UnityEngine.Object.DestroyImmediate(rootObject);
            UnityEngine.Object.DestroyImmediate(uiObject);
            UnityEngine.Object.DestroyImmediate(levelGeneratorObject);
            UnityEngine.Object.DestroyImmediate(inputObject);
            UnityEngine.Object.DestroyImmediate(tiebreakerObject);
            UnityEngine.Object.DestroyImmediate(difficultyObject);
            UnityEngine.Object.DestroyImmediate(gemObject);
            UnityEngine.Object.DestroyImmediate(roundObject);
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
