using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Ubongo.Application.Bootstrap;
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
        public void GameManager_Initialize_IsIdempotent()
        {
            GameObject managerObject = new GameObject("GameManager_Test");
            GameManager manager = managerObject.AddComponent<GameManager>();
            InMemorySettingsStore injectedStore = new InMemorySettingsStore();

            manager.Initialize(injectedStore);
            manager.Initialize(injectedStore);
            manager.SetShowSolutionOnTimeout(true);

            FieldInfo settingsField = typeof(GameManager).GetField("settingsStore", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(settingsField);
            Assert.AreSame(injectedStore, settingsField.GetValue(manager));
            Assert.IsTrue(injectedStore.StoredValue);
            Assert.AreEqual(1, injectedStore.SaveCallCount);
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
            T[] components = Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (T component in components)
            {
                if (component == null)
                {
                    continue;
                }

                Object.DestroyImmediate(component.gameObject);
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
