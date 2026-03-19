using NUnit.Framework;
using Ubongo.Application.Formatting;

namespace Ubongo.Tests.EditMode.Application.Formatting
{
    public class DifficultyDisplayFormatterTests
    {
        [Test]
        public void Format_WithDefaultValues_ReturnsExpectedText()
        {
            string text = DifficultyDisplayFormatter.Format("Hard", 4);

            Assert.AreEqual("Hard (4 pieces)", text);
        }

        [Test]
        public void Format_WithCustomValues_ReturnsDisplayNameAndPieceCount()
        {
            string text = DifficultyDisplayFormatter.Format("Master", 7);

            Assert.AreEqual("Master (7 pieces)", text);
        }
    }
}
