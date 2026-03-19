using NUnit.Framework;
using UnityEngine;

namespace Ubongo.Tests.EditMode
{
    public class ProjectSanityTests
    {
        [Test]
        public void RequiredLayers_AreConfigured()
        {
            Assert.GreaterOrEqual(LayerMask.NameToLayer("Board"), 0);
            Assert.GreaterOrEqual(LayerMask.NameToLayer("Piece"), 0);
        }
    }
}
