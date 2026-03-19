using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Ubongo.Domain;
using Ubongo.Systems;
using Ubongo.Tests.PlayMode.Shared;

namespace Ubongo.Tests.PlayMode.Presentation.Managers
{
    public class GameManagerPlayModeTests
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
        public IEnumerator GameManager_SetGameMode_MultiplayerRequest_FallsBackToClassic()
        {
            GameManager manager = PlayModeSceneHelper.CreateConfiguredGameManager(out GameObject managerObject, out GameObject[] dependencies);
            yield return null;

            manager.SetGameMode(GameMode.Multiplayer);

            Assert.AreEqual(GameMode.Classic, manager.CurrentMode);
            yield return PlayModeSceneHelper.DestroyAndWait(PlayModeSceneHelper.CombineObjects(managerObject, dependencies));
        }

        [UnityTest]
        public IEnumerator GameManager_StartGame_ZenThenClassic_ResetsHintsByModeDefaults()
        {
            GameManager manager = PlayModeSceneHelper.CreateConfiguredGameManager(out GameObject managerObject, out GameObject[] dependencies);
            yield return null;

            manager.SetGameMode(GameMode.Zen);
            manager.StartGame(DifficultyLevel.Easy);
            Assert.IsTrue(manager.EnableHints);

            manager.SetGameMode(GameMode.Classic);
            manager.SetHintsEnabled(true);
            Assert.IsTrue(manager.EnableHints);

            manager.StartGame(DifficultyLevel.Easy);
            Assert.IsFalse(manager.EnableHints);

            yield return PlayModeSceneHelper.DestroyAndWait(PlayModeSceneHelper.CombineObjects(managerObject, dependencies));
        }

        [UnityTest]
        public IEnumerator GameManager_StartGame_WithInvalidDifficulty_FallsBackToEasy()
        {
            GameManager manager = PlayModeSceneHelper.CreateConfiguredGameManager(out GameObject managerObject, out GameObject[] dependencies);
            yield return null;

            manager.StartGame((DifficultyLevel)0);
            yield return PlayModeSceneHelper.WaitForRoundInProgress(manager.RoundManager, 1);

            Assert.AreEqual(DifficultyLevel.Easy, manager.CurrentDifficulty);
            PlayModeSceneHelper.AssertRoundRules(manager.RoundManager, manager.DifficultySystem, DifficultyLevel.Easy);

            yield return PlayModeSceneHelper.DestroyAndWait(PlayModeSceneHelper.CombineObjects(managerObject, dependencies));
        }
    }
}
