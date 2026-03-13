using System.Collections;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Ubongo.Application.Bootstrap;
using Ubongo.Domain;
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
            GameObject gemObject = new GameObject("GemSystem_Test");
            GemSystem gemSystem = gemObject.AddComponent<GemSystem>();
            GameObject roundObject = new GameObject("RoundManager_Test");
            RoundManager roundManager = roundObject.AddComponent<RoundManager>();
            roundManager.ConfigureRuntimeDependencies(difficultySystem, gemSystem);
            yield return null;

            roundManager.StartNewGame(DifficultyLevel.Easy);
            yield return WaitForRoundInProgress(roundManager, 1);
            AssertRoundRules(roundManager, difficultySystem, DifficultyLevel.Easy);

            roundManager.StartNewGame(DifficultyLevel.Hard);
            yield return WaitForRoundInProgress(roundManager, 1);
            AssertRoundRules(roundManager, difficultySystem, DifficultyLevel.Hard);

            yield return DestroyAndWait(roundObject, difficultyObject, gemObject);
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

        [UnityTest]
        public IEnumerator GameManager_SetGameMode_MultiplayerRequest_FallsBackToClassic()
        {
            GameManager manager = CreateConfiguredGameManager(out GameObject managerObject, out GameObject[] dependencies);
            yield return null;

            manager.SetGameMode(GameMode.Multiplayer);

            Assert.AreEqual(GameMode.Classic, manager.CurrentMode);
            yield return DestroyAndWait(CombineObjects(managerObject, dependencies));
        }

        [UnityTest]
        public IEnumerator GameManager_StartGame_ZenThenClassic_ResetsHintsByModeDefaults()
        {
            GameManager manager = CreateConfiguredGameManager(out GameObject managerObject, out GameObject[] dependencies);
            yield return null;

            manager.SetGameMode(GameMode.Zen);
            manager.StartGame(DifficultyLevel.Easy);
            Assert.IsTrue(manager.EnableHints);

            manager.SetGameMode(GameMode.Classic);
            manager.SetHintsEnabled(true);
            Assert.IsTrue(manager.EnableHints);

            manager.StartGame(DifficultyLevel.Easy);
            Assert.IsFalse(manager.EnableHints);

            yield return DestroyAndWait(CombineObjects(managerObject, dependencies));
        }

        [UnityTest]
        public IEnumerator GameManager_StartGame_WithInvalidDifficulty_FallsBackToEasy()
        {
            GameManager manager = CreateConfiguredGameManager(out GameObject managerObject, out GameObject[] dependencies);
            yield return null;

            manager.StartGame((DifficultyLevel)0);
            yield return WaitForRoundInProgress(manager.RoundManager, 1);

            Assert.AreEqual(DifficultyLevel.Easy, manager.CurrentDifficulty);
            AssertRoundRules(manager.RoundManager, manager.DifficultySystem, DifficultyLevel.Easy);

            yield return DestroyAndWait(CombineObjects(managerObject, dependencies));
        }

        [UnityTest]
        public IEnumerator RoundManager_TryFailCurrentRound_DoesNotTriggerSecondChance()
        {
            GameObject difficultyObject = new GameObject("DifficultySystem_Test");
            DifficultySystem difficultySystem = difficultyObject.AddComponent<DifficultySystem>();
            GameObject gemObject = new GameObject("GemSystem_Test");
            GemSystem gemSystem = gemObject.AddComponent<GemSystem>();
            GameObject roundObject = new GameObject("RoundManager_Test");
            RoundManager roundManager = roundObject.AddComponent<RoundManager>();
            roundManager.ConfigureRuntimeDependencies(difficultySystem, gemSystem);
            roundManager.SetTotalPlayers(2);

            bool secondChanceStarted = false;
            bool roundFailed = false;

            roundManager.OnSecondChanceStarted += () => secondChanceStarted = true;
            roundManager.OnRoundFailed += _ => roundFailed = true;
            roundManager.OnRoundStarted += _ => roundManager.TryFailCurrentRound("forced round-start failure");

            yield return null;
            roundManager.StartNewGame(DifficultyLevel.Easy);

            float elapsed = 0f;
            while (!roundFailed && elapsed < 2f)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            Assert.IsTrue(roundFailed);
            Assert.IsFalse(secondChanceStarted);

            yield return DestroyAndWait(roundObject, difficultyObject, gemObject);
        }

        [UnityTest]
        public IEnumerator GameCompositionRoot_Awake_WithoutGameManager_DoesNotAutoCreateManager()
        {
            yield return CleanupRuntimeSingletonObjects();

            GameCompositionRoot[] rootsBefore = UnityEngine.Object.FindObjectsByType<GameCompositionRoot>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            GameManager[] managersBefore = UnityEngine.Object.FindObjectsByType<GameManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.AreEqual(0, rootsBefore.Length, "Expected no pre-existing GameCompositionRoot before test setup.");
            Assert.AreEqual(0, managersBefore.Length, "Expected no pre-existing GameManager before test setup.");

            GameObject difficultyObject = new GameObject("DifficultySystem_Test");
            DifficultySystem difficultySystem = difficultyObject.AddComponent<DifficultySystem>();
            GameObject gemObject = new GameObject("GemSystem_Test");
            GemSystem gemSystem = gemObject.AddComponent<GemSystem>();
            GameObject roundObject = new GameObject("RoundManager_Test");
            RoundManager roundManager = roundObject.AddComponent<RoundManager>();
            roundManager.ConfigureRuntimeDependencies(difficultySystem, gemSystem);
            GameObject tiebreakerObject = new GameObject("TiebreakerManager_Test");
            tiebreakerObject.AddComponent<TiebreakerManager>();
            GameObject inputObject = new GameObject("InputManager_Test");
            inputObject.AddComponent<InputManager>();
            GameObject levelGeneratorObject = new GameObject("LevelGenerator_Test");
            levelGeneratorObject.AddComponent<LevelGenerator>();
            GameObject uiManagerObject = new GameObject("UIManager_Test");
            uiManagerObject.AddComponent<UIManager>();
            uiManagerObject.SetActive(false);

            LogAssert.Expect(LogType.Error, "[GameCompositionRoot] Expected exactly one GameManager in scene, but found 0.");
            LogAssert.Expect(LogType.Exception, new Regex(@"\[GameCompositionRoot\] Runtime graph validation failed"));
            GameObject rootObject = new GameObject("GameCompositionRoot_Test");
            rootObject.AddComponent<GameCompositionRoot>();
            yield return null;

            GameManager[] managers = UnityEngine.Object.FindObjectsByType<GameManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.AreEqual(0, managers.Length);

            yield return DestroyAndWait(
                rootObject,
                difficultyObject,
                gemObject,
                roundObject,
                tiebreakerObject,
                inputObject,
                levelGeneratorObject,
                uiManagerObject);
        }

        [UnityTest]
        public IEnumerator StartNextRound_CalledDuringTransition_DoesNotDoubleStart()
        {
            GameObject difficultyObject = new GameObject("DifficultySystem_Test");
            DifficultySystem difficultySystem = difficultyObject.AddComponent<DifficultySystem>();
            GameObject gemObject = new GameObject("GemSystem_Test");
            GemSystem gemSystem = gemObject.AddComponent<GemSystem>();
            GameObject roundObject = new GameObject("RoundManager_Test");
            RoundManager roundManager = roundObject.AddComponent<RoundManager>();
            roundManager.ConfigureRuntimeDependencies(difficultySystem, gemSystem);
            yield return null;

            int roundStartedCount = 0;
            roundManager.OnRoundStarted += _ => roundStartedCount++;

            roundManager.StartNewGame(DifficultyLevel.Easy);
            yield return WaitForRoundInProgress(roundManager, 1);
            Assert.AreEqual(1, roundStartedCount);

            // Round 1 완료 → TransitionToNextRound 코루틴 시작 (state=Transitioning)
            roundManager.CompleteRound();
            Assert.AreEqual(RoundState.Transitioning, roundManager.CurrentState);

            // transition delay 중에 수동으로 StartNextRound 호출
            roundManager.StartNextRound();
            yield return WaitForRoundInProgress(roundManager, 2);
            Assert.AreEqual(2, roundStartedCount, "Round 2 should start only once");

            // transition 코루틴의 delay가 지나도 Round 3가 시작되지 않아야 함
            float elapsed = 0f;
            int roundBefore = roundManager.CurrentRound;
            while (elapsed < 3f)
            {
                if (roundManager.CurrentRound != roundBefore)
                {
                    Assert.Fail($"Round unexpectedly advanced to {roundManager.CurrentRound}");
                }
                elapsed += Time.deltaTime;
                yield return null;
            }

            Assert.AreEqual(2, roundManager.CurrentRound);

            yield return DestroyAndWait(roundObject, difficultyObject, gemObject);
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

        private static IEnumerator WaitForDestroyed<T>(int maxFrames = 30) where T : Component
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

        private static GameManager CreateConfiguredGameManager(out GameObject managerObject, out GameObject[] dependencies)
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

        private static GameObject[] CombineObjects(GameObject managerObject, GameObject[] dependencies)
        {
            GameObject[] combined = new GameObject[dependencies.Length + 1];
            combined[0] = managerObject;
            for (int i = 0; i < dependencies.Length; i++)
            {
                combined[i + 1] = dependencies[i];
            }

            return combined;
        }
    }
}
