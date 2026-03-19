using NUnit.Framework;
using UnityEngine;
using Ubongo.Systems;

namespace Ubongo.Tests.EditMode.Presentation.Systems
{
    public class RoundManagerTests
    {
        [Test]
        public void RoundManager_TryFailCurrentRound_WhenNotInProgress_ReturnsFalse()
        {
            GameObject roundObject = new GameObject("RoundManager_Test");
            RoundManager roundManager = roundObject.AddComponent<RoundManager>();

            bool failed = roundManager.TryFailCurrentRound();

            Assert.IsFalse(failed);
            UnityEngine.Object.DestroyImmediate(roundObject);
        }
    }
}
