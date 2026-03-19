using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Ubongo.Domain;
using Ubongo.Systems;
using Ubongo.Tests.PlayMode.Shared;

namespace Ubongo.Tests.PlayMode.Presentation.Systems
{
    public class RoundManagerPlayModeTests
    {
        [UnitySetUp]
        public IEnumerator UnitySetUp()
        {
            yield return PlayModeSceneHelper.CleanupRuntimeSingletonObjects();
        }

        [UnityTearDown]
        public IEnumerator UnityTearDown()
        {
            yield return PlayModeSceneHelper.CleanupRuntimeSingletonObjects();
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
            yield return PlayModeSceneHelper.WaitForRoundInProgress(roundManager, 1);
            PlayModeSceneHelper.AssertRoundRules(roundManager, difficultySystem, DifficultyLevel.Easy);

            roundManager.StartNewGame(DifficultyLevel.Hard);
            yield return PlayModeSceneHelper.WaitForRoundInProgress(roundManager, 1);
            PlayModeSceneHelper.AssertRoundRules(roundManager, difficultySystem, DifficultyLevel.Hard);

            yield return PlayModeSceneHelper.DestroyAndWait(roundObject, difficultyObject, gemObject);
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

            yield return PlayModeSceneHelper.DestroyAndWait(roundObject, difficultyObject, gemObject);
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
            yield return PlayModeSceneHelper.WaitForRoundInProgress(roundManager, 1);
            Assert.AreEqual(1, roundStartedCount);

            // Round 1 완료 → TransitionToNextRound 코루틴 시작 (state=Transitioning)
            roundManager.CompleteRound();
            Assert.AreEqual(RoundState.Transitioning, roundManager.CurrentState);

            // transition delay 중에 수동으로 StartNextRound 호출
            roundManager.StartNextRound();
            yield return PlayModeSceneHelper.WaitForRoundInProgress(roundManager, 2);
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

            yield return PlayModeSceneHelper.DestroyAndWait(roundObject, difficultyObject, gemObject);
        }

    }
}
