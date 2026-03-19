using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using Ubongo.Application.Bootstrap;
using Ubongo.Tests.EditMode.Shared;

namespace Ubongo.Tests.EditMode.Bootstrap
{
    public class GameCompositionRootTests
    {
        [SetUp]
        public void SetUp()
        {
            SceneTestHelper.CleanupRuntimeSingletonObjects();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SceneTestHelper.CleanupRuntimeSingletonObjects();
        }

        [TearDown]
        public void TearDown()
        {
            SceneTestHelper.CleanupRuntimeSingletonObjects();
        }

        [Test]
        public void GameCompositionRoot_Awake_WithoutUIManager_FailsFast()
        {
            DependencyFixtureBuilder.CreateBaseline().Remove<UIManager>();
            SceneTestHelper.AssertRequiredComponentCardinality(expectedUiManagers: 0, expectedGameBoards: 1);

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
            SceneTestHelper.AssertRequiredComponentCardinality(expectedUiManagers: 1, expectedGameBoards: 0);

            LogAssert.Expect(LogType.Error, new Regex(@"\[GameCompositionRoot\] Expected exactly one GameBoard in scene, but found \d+\."));
            LogAssert.Expect(LogType.Exception, new Regex(@"\[GameCompositionRoot\] Runtime graph validation failed"));

            GameObject rootObject = new GameObject("GameCompositionRoot_Test");
            rootObject.AddComponent<GameCompositionRoot>();

            UnityEngine.Object.DestroyImmediate(rootObject);
        }
    }
}
