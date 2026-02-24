using NUnit.Framework;
using UnityEngine;
using Ubongo.Application.Formatting;
using Ubongo.Domain;
using Ubongo.Systems;

namespace Ubongo.Tests.EditMode
{
    public class DifficultyDisplayFormatterTests
    {
        [Test]
        public void Format_WithDefaultConfig_ReturnsExpectedText()
        {
            DifficultyConfig config = DifficultyConfig.CreateDefault(DifficultyLevel.Medium);

            string text = DifficultyDisplayFormatter.Format(config);

            Assert.AreEqual("Medium (4 pieces)", text);
        }

        [Test]
        public void Format_WithCustomConfig_ReturnsDisplayNameAndPieceCount()
        {
            DifficultyConfig config = new DifficultyConfig(
                level: DifficultyLevel.Expert,
                displayName: "Master",
                displayColor: Color.magenta,
                pieceCount: 7,
                boardSize: new Vector2Int(5, 5),
                timeLimit: 30f,
                solutionCount: 1,
                scoreMultiplier: 3f);

            string text = DifficultyDisplayFormatter.Format(config);

            Assert.AreEqual("Master (7 pieces)", text);
        }
    }
}
