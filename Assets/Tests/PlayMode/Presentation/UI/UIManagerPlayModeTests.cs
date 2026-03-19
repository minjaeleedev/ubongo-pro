using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Ubongo.Domain;
using Ubongo.Systems;
using Ubongo.Tests.PlayMode.Shared;

namespace Ubongo.Tests.PlayMode.Presentation.UI
{
    public class UIManagerPlayModeTests
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
        public IEnumerator UIManager_SetDifficulty_UpdatesCurrentDifficulty()
        {
            GameObject uiObject = new GameObject("UIManager_Test");
            UIManager uiManager = uiObject.AddComponent<UIManager>();
            uiManager.SetDifficulty(DifficultyLevel.Hard);
            Assert.AreEqual(DifficultyLevel.Hard, uiManager.CurrentDifficulty);
            yield return PlayModeSceneHelper.DestroyAndWait(uiObject);
        }
    }
}
