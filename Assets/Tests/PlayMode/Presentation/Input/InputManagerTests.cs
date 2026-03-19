using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Ubongo;

namespace Ubongo.Tests.PlayMode.Presentation.Input
{
    public class InputManagerTests
    {
        [UnitySetUp]
        public IEnumerator UnitySetUp()
        {
            yield return DestroyAllInputManagers();
        }

        [UnityTearDown]
        public IEnumerator UnityTearDown()
        {
            yield return DestroyAllInputManagers();
        }

        [UnityTest]
        public IEnumerator ProcessDebugAction_EnableDisable_DoesNotInvokeWhenDisabled()
        {
            GameObject inputObject = new GameObject("InputManager_Test");
            InputManager inputManager = inputObject.AddComponent<InputManager>();
            int helpToggleCount = 0;

            try
            {
                yield return null;

                inputManager.OnToggleHelp += () => helpToggleCount++;

                Assert.IsTrue(inputManager.ProcessDebugAction(InputManager.DebugActionType.Help));
                Assert.AreEqual(1, helpToggleCount);

                inputManager.enabled = false;
                Assert.IsFalse(inputManager.ProcessDebugAction(InputManager.DebugActionType.Help));
                Assert.AreEqual(1, helpToggleCount);

                inputManager.enabled = true;
                yield return null;
                Assert.IsTrue(inputManager.ProcessDebugAction(InputManager.DebugActionType.Help));
                Assert.AreEqual(2, helpToggleCount);

                inputManager.enabled = false;
                inputManager.enabled = true;
                yield return null;
                Assert.IsTrue(inputManager.ProcessDebugAction(InputManager.DebugActionType.Help));
                Assert.AreEqual(3, helpToggleCount);
            }
            finally
            {
                UnityEngine.Object.Destroy(inputObject);
            }

            yield return WaitForDestroyed(inputObject);
        }

        private static IEnumerator DestroyAllInputManagers()
        {
            InputManager[] managers = UnityEngine.Object.FindObjectsByType<InputManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (InputManager manager in managers)
            {
                if (manager == null)
                {
                    continue;
                }

                UnityEngine.Object.Destroy(manager.gameObject);
            }

            yield return WaitForNoInputManagers();
        }

        private static IEnumerator WaitForNoInputManagers(int maxFrames = 5)
        {
            for (int frame = 0; frame < maxFrames; frame++)
            {
                InputManager[] remaining = UnityEngine.Object.FindObjectsByType<InputManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                if (remaining.Length == 0)
                {
                    yield break;
                }

                yield return null;
            }
        }

        private static IEnumerator WaitForDestroyed(GameObject target, int maxFrames = 5)
        {
            for (int frame = 0; frame < maxFrames && target != null; frame++)
            {
                yield return null;
            }
        }
    }
}
