using System.Collections;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Ubongo.Application.Bootstrap;
using Ubongo.Systems;
using Ubongo.Tests.PlayMode.Shared;

namespace Ubongo.Tests.PlayMode.Bootstrap
{
    public class GameCompositionRootPlayModeTests
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
        public IEnumerator GameCompositionRoot_Awake_WithoutGameManager_DoesNotAutoCreateManager()
        {
            yield return PlayModeSceneHelper.CleanupRuntimeSingletonObjects();

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

            yield return PlayModeSceneHelper.DestroyAndWait(
                rootObject,
                difficultyObject,
                gemObject,
                roundObject,
                tiebreakerObject,
                inputObject,
                levelGeneratorObject,
                uiManagerObject);
        }
    }
}
