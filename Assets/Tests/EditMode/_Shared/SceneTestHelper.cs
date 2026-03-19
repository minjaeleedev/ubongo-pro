using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Ubongo.Application.Bootstrap;
using Ubongo.Systems;

namespace Ubongo.Tests.EditMode.Shared
{
    public static class SceneTestHelper
    {
        public static void CleanupRuntimeSingletonObjects()
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

        public static void DestroyAllComponents<T>() where T : Component
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

                Object.DestroyImmediate(target);
            }
        }

        public static int CountSceneComponents<T>() where T : Component
        {
            return Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
        }

        public static void AssertRequiredComponentCardinality(int expectedUiManagers, int expectedGameBoards)
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
    }
}
