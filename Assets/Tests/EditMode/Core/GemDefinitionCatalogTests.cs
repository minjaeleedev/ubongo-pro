using NUnit.Framework;
using UnityEngine;
using Ubongo.Domain;
using Ubongo.Systems;

namespace Ubongo.Tests.EditMode.Core
{
    public class GemDefinitionCatalogTests
    {
        [TestCase(GemType.Ruby, 4)]
        [TestCase(GemType.Sapphire, 3)]
        [TestCase(GemType.Emerald, 2)]
        [TestCase(GemType.Amber, 1)]
        public void GetPointValue_KnownType_ReturnsConfiguredValue(GemType type, int expectedPointValue)
        {
            int pointValue = GemDefinitionCatalog.GetPointValue(type);

            Assert.AreEqual(expectedPointValue, pointValue);
        }

        [Test]
        public void GemConstructor_UsesCatalogForPointAndColor()
        {
            GemDefinition definition = GemDefinitionCatalog.Get(GemType.Sapphire);

            var gem = new Gem(GemType.Sapphire);

            Assert.AreEqual(definition.PointValue, gem.PointValue);
            Assert.AreEqual(definition.Color, gem.Color);
        }

        [Test]
        public void GetIconStyle_KnownType_ReturnsConfiguredStyle()
        {
            GemIconStyle iconStyle = GemDefinitionCatalog.GetIconStyle(GemType.Emerald);

            Assert.AreEqual(Color.white, iconStyle.HighlightColor);
            Assert.IsFalse(string.IsNullOrWhiteSpace(iconStyle.Description));
        }

        [Test]
        public void Get_UnknownType_ReturnsFallbackDefinition()
        {
            GemType unknownType = (GemType)999;

            GemDefinition definition = GemDefinitionCatalog.Get(unknownType);

            Assert.AreEqual(unknownType, definition.Type);
            Assert.AreEqual(0, definition.PointValue);
            Assert.AreEqual(Color.white, definition.Color);
            Assert.AreEqual("Unknown gem", definition.IconStyle.Description);
        }
    }
}
