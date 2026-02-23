using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Ubongo.Systems;

namespace Ubongo.Tests.PlayMode
{
    public class DifficultyFlowPlayModeTests
    {
        [UnitySetUp]
        public IEnumerator UnitySetUp()
        {
            yield return CleanupRuntimeSingletonObjects();
        }

        [UnityTearDown]
        public IEnumerator UnityTearDown()
        {
            yield return CleanupRuntimeSingletonObjects();
        }

        [UnityTest]
        public IEnumerator RoundManager_StartNewGame_WithSelectedDifficulty_UpdatesRoundRules()
        {
            GameObject difficultyObject = new GameObject("DifficultySystem_Test");
            DifficultySystem difficultySystem = difficultyObject.AddComponent<DifficultySystem>();
            GameObject roundObject = new GameObject("RoundManager_Test");
            RoundManager roundManager = roundObject.AddComponent<RoundManager>();
            yield return null;

            roundManager.StartNewGame(DifficultyLevel.Easy);
            yield return WaitForRoundInProgress(roundManager, 1);
            AssertRoundRules(roundManager, difficultySystem, DifficultyLevel.Easy);

            roundManager.StartNewGame(DifficultyLevel.Medium);
            yield return WaitForRoundInProgress(roundManager, 1);
            AssertRoundRules(roundManager, difficultySystem, DifficultyLevel.Medium);

            yield return DestroyAndWait(roundObject, difficultyObject);
        }

        [UnityTest]
        public IEnumerator UIManager_SetDifficulty_UpdatesCurrentDifficulty()
        {
            GameObject uiObject = new GameObject("UIManager_Test");
            UIManager uiManager = uiObject.AddComponent<UIManager>();
            uiManager.SetDifficulty(DifficultyLevel.Hard);
            Assert.AreEqual(DifficultyLevel.Hard, uiManager.CurrentDifficulty);
            yield return DestroyAndWait(uiObject);
        }

        private static IEnumerator WaitForRoundInProgress(RoundManager roundManager, int expectedRound, float timeoutSeconds = 2f)
        {
            float elapsed = 0f;
            while (elapsed < timeoutSeconds)
            {
                if (roundManager != null &&
                    roundManager.CurrentRound == expectedRound &&
                    roundManager.CurrentState == RoundState.InProgress &&
                    roundManager.CurrentTimeLimit > 0f)
                {
                    yield break;
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            Assert.Fail($"Round {expectedRound} did not initialize within {timeoutSeconds:0.00}s.");
        }

        private static void AssertRoundRules(
            RoundManager roundManager,
            DifficultySystem difficultySystem,
            DifficultyLevel expectedDifficulty)
        {
            Assert.AreEqual(1, roundManager.CurrentRound);
            Assert.AreEqual(
                difficultySystem.GetTimeLimit(expectedDifficulty),
                roundManager.CurrentTimeLimit,
                0.01f);
        }

        private static IEnumerator CleanupRuntimeSingletonObjects()
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
            yield return WaitForDestroyed<GameManager>();
            yield return WaitForDestroyed<GameBoard>();
            yield return WaitForDestroyed<InputManager>();
            yield return WaitForDestroyed<LevelGenerator>();
            yield return WaitForDestroyed<RoundManager>();
            yield return WaitForDestroyed<GemSystem>();
            yield return WaitForDestroyed<DifficultySystem>();
            yield return WaitForDestroyed<TiebreakerManager>();
            yield return WaitForDestroyed<UIManager>();
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

                UnityEngine.Object.Destroy(component.gameObject);
            }
        }

        private static IEnumerator WaitForDestroyed<T>(int maxFrames = 5) where T : Component
        {
            for (int frame = 0; frame < maxFrames; frame++)
            {
                T[] remaining = UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                if (remaining.Length == 0)
                {
                    yield break;
                }

                yield return null;
            }
        }

        private static IEnumerator DestroyAndWait(params GameObject[] objects)
        {
            foreach (GameObject obj in objects)
            {
                if (obj == null)
                {
                    continue;
                }

                UnityEngine.Object.Destroy(obj);
            }

            for (int frame = 0; frame < 5; frame++)
            {
                bool allDestroyed = true;
                foreach (GameObject obj in objects)
                {
                    if (obj != null)
                    {
                        allDestroyed = false;
                        break;
                    }
                }

                if (allDestroyed)
                {
                    yield break;
                }

                yield return null;
            }
        }
    }
}
