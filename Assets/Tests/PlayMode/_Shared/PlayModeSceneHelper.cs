using System.Collections;
using NUnit.Framework;
using UnityEngine;
using Ubongo.Application.Bootstrap;
using Ubongo.Domain;
using Ubongo.Systems;

namespace Ubongo.Tests.PlayMode.Shared
{
    public static class PlayModeSceneHelper
    {
        public static IEnumerator CleanupRuntimeSingletonObjects()
        {
            DestroyAllComponents<GameManager>();
            DestroyAllComponents<GameCompositionRoot>();
            DestroyAllComponents<GameBoard>();
            DestroyAllComponents<InputManager>();
            DestroyAllComponents<LevelGenerator>();
            DestroyAllComponents<RoundManager>();
            DestroyAllComponents<GemSystem>();
            DestroyAllComponents<DifficultySystem>();
            DestroyAllComponents<TiebreakerManager>();
            DestroyAllComponents<UIManager>();
            yield return WaitForDestroyed<GameManager>();
            yield return WaitForDestroyed<GameCompositionRoot>();
            yield return WaitForDestroyed<GameBoard>();
            yield return WaitForDestroyed<InputManager>();
            yield return WaitForDestroyed<LevelGenerator>();
            yield return WaitForDestroyed<RoundManager>();
            yield return WaitForDestroyed<GemSystem>();
            yield return WaitForDestroyed<DifficultySystem>();
            yield return WaitForDestroyed<TiebreakerManager>();
            yield return WaitForDestroyed<UIManager>();
        }

        public static void DestroyAllComponents<T>() where T : Component
        {
            T[] components = Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (T component in components)
            {
                if (component == null)
                {
                    continue;
                }

                Object.Destroy(component.gameObject);
            }
        }

        public static IEnumerator WaitForDestroyed<T>(int maxFrames = 30) where T : Component
        {
            for (int frame = 0; frame < maxFrames; frame++)
            {
                T[] remaining = Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                if (remaining.Length == 0)
                {
                    yield break;
                }

                yield return null;
            }
        }

        public static IEnumerator DestroyAndWait(params GameObject[] objects)
        {
            foreach (GameObject obj in objects)
            {
                if (obj == null)
                {
                    continue;
                }

                Object.Destroy(obj);
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

        public static GameManager CreateConfiguredGameManager(out GameObject managerObject, out GameObject[] dependencies)
        {
            GameObject gemObject = new GameObject("GemSystem_Test");
            GemSystem gemSystem = gemObject.AddComponent<GemSystem>();
            GameObject difficultyObject = new GameObject("DifficultySystem_Test");
            DifficultySystem difficultySystem = difficultyObject.AddComponent<DifficultySystem>();
            GameObject roundObject = new GameObject("RoundManager_Test");
            RoundManager roundManager = roundObject.AddComponent<RoundManager>();
            roundManager.ConfigureRuntimeDependencies(difficultySystem, gemSystem);
            GameObject tiebreakerObject = new GameObject("TiebreakerManager_Test");
            TiebreakerManager tiebreakerManager = tiebreakerObject.AddComponent<TiebreakerManager>();
            GameObject inputObject = new GameObject("InputManager_Test");
            InputManager inputManager = inputObject.AddComponent<InputManager>();
            GameObject levelGeneratorObject = new GameObject("LevelGenerator_Test");
            LevelGenerator levelGenerator = levelGeneratorObject.AddComponent<LevelGenerator>();
            GameObject boardObject = new GameObject("GameBoard_Test");
            GameBoard board = boardObject.AddComponent<GameBoard>();
            board.Construct(BoardRuntimeServices.CreateDefault());

            managerObject = new GameObject("GameManager_Test");
            GameManager manager = managerObject.AddComponent<GameManager>();
            manager.ConfigureRuntimeDependencies(
                gemSystem,
                roundManager,
                difficultySystem,
                tiebreakerManager,
                levelGenerator,
                board,
                inputManager);

            dependencies = new[]
            {
                gemObject,
                difficultyObject,
                roundObject,
                tiebreakerObject,
                inputObject,
                levelGeneratorObject,
                boardObject
            };

            return manager;
        }

        public static GameObject[] CombineObjects(GameObject managerObject, GameObject[] dependencies)
        {
            GameObject[] combined = new GameObject[dependencies.Length + 1];
            combined[0] = managerObject;
            for (int i = 0; i < dependencies.Length; i++)
            {
                combined[i + 1] = dependencies[i];
            }

            return combined;
        }

        public static IEnumerator WaitForRoundInProgress(RoundManager roundManager, int expectedRound, float timeoutSeconds = 2f)
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

        public static void AssertRoundRules(
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
    }
}
