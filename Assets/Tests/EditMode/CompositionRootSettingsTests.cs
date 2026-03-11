using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
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
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
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
            DependencyFixtureBuilder.CreateBaseline().Remove<UIManager>();
            AssertRequiredComponentCardinality(expectedUiManagers: 0, expectedGameBoards: 1);

            LogAssert.Expect(LogType.Error, new Regex(@"\[GameCompositionRoot\] Expected exactly one UIManager in scene, but found \d+\."));
            LogAssert.Expect(LogType.Exception, new Regex(@"\[GameCompositionRoot\] Runtime graph validation failed"));

            GameObject rootObject = new GameObject("GameCompositionRoot_Test");
            rootObject.AddComponent<GameCompositionRoot>();

            UnityEngine.Object.DestroyImmediate(rootObject);
        }

        [Test]
        public void GameCompositionRoot_Awake_WithoutGameBoard_FailsFast()
        {
            DependencyFixtureBuilder.CreateBaseline().Remove<GameBoard>();
            AssertRequiredComponentCardinality(expectedUiManagers: 1, expectedGameBoards: 0);

            LogAssert.Expect(LogType.Error, new Regex(@"\[GameCompositionRoot\] Expected exactly one GameBoard in scene, but found \d+\."));
            LogAssert.Expect(LogType.Exception, new Regex(@"\[GameCompositionRoot\] Runtime graph validation failed"));

            GameObject rootObject = new GameObject("GameCompositionRoot_Test");
            rootObject.AddComponent<GameCompositionRoot>();

            UnityEngine.Object.DestroyImmediate(rootObject);
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
            T[] components = Resources.FindObjectsOfTypeAll<T>();
            foreach (T component in components)
            {
                if (component == null)
                {
                    continue;
                }

                GameObject target = component.gameObject;
                if (target == null)
                {
                    continue;
                }

                if (EditorUtility.IsPersistent(target))
                {
                    continue;
                }

                if (!target.scene.IsValid() || !target.scene.isLoaded)
                {
                    continue;
                }

                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        private static int CountSceneComponents<T>() where T : Component
        {
            return UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
        }

        private static void AssertRequiredComponentCardinality(int expectedUiManagers, int expectedGameBoards)
        {
            Assert.AreEqual(0, CountSceneComponents<GameCompositionRoot>(), "Precondition failed: GameCompositionRoot should not exist before test trigger.");
            Assert.AreEqual(1, CountSceneComponents<GameManager>(), "Precondition failed: expected exactly one GameManager.");
            Assert.AreEqual(1, CountSceneComponents<RoundManager>(), "Precondition failed: expected exactly one RoundManager.");
            Assert.AreEqual(1, CountSceneComponents<GemSystem>(), "Precondition failed: expected exactly one GemSystem.");
            Assert.AreEqual(1, CountSceneComponents<DifficultySystem>(), "Precondition failed: expected exactly one DifficultySystem.");
            Assert.AreEqual(1, CountSceneComponents<TiebreakerManager>(), "Precondition failed: expected exactly one TiebreakerManager.");
            Assert.AreEqual(1, CountSceneComponents<InputManager>(), "Precondition failed: expected exactly one InputManager.");
            Assert.AreEqual(1, CountSceneComponents<LevelGenerator>(), "Precondition failed: expected exactly one LevelGenerator.");
            Assert.AreEqual(expectedUiManagers, CountSceneComponents<UIManager>(), $"Precondition failed: expected {expectedUiManagers} UIManager component(s).");
            Assert.AreEqual(expectedGameBoards, CountSceneComponents<GameBoard>(), $"Precondition failed: expected {expectedGameBoards} GameBoard component(s).");
        }

        private sealed class DependencyFixtureBuilder
        {
            private readonly Dictionary<Type, Component> components = new Dictionary<Type, Component>();

            public static DependencyFixtureBuilder CreateBaseline()
            {
                var builder = new DependencyFixtureBuilder();
                builder.Add<GameManager>("GameManager_Test");
                builder.Add<RoundManager>("RoundManager_Test");
                builder.Add<GemSystem>("GemSystem_Test");
                builder.Add<DifficultySystem>("DifficultySystem_Test");
                builder.Add<TiebreakerManager>("TiebreakerManager_Test");
                builder.Add<InputManager>("InputManager_Test");
                builder.Add<LevelGenerator>("LevelGenerator_Test");
                builder.Add<UIManager>("UIManager_Test");
                builder.Add<GameBoard>("GameBoard_Test");
                return builder;
            }

            public DependencyFixtureBuilder Remove<T>() where T : Component
            {
                Type key = typeof(T);
                if (!components.TryGetValue(key, out Component component))
                {
                    return this;
                }

                if (component != null)
                {
                    UnityEngine.Object.DestroyImmediate(component.gameObject);
                }

                components.Remove(key);
                return this;
            }

            private void Add<T>(string objectName) where T : Component
            {
                GameObject gameObject = new GameObject(objectName);
                components[typeof(T)] = gameObject.AddComponent<T>();
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
