using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Ubongo.Core;
using Ubongo.Domain.Board;

namespace Ubongo.Tests.EditMode
{
    public class BoardWinConditionServiceTests
    {
        [Test]
        public void CalculateFillState_MatchesExpectedProgress()
        {
            BoardState board = new BoardState(1, TargetArea.RequiredHeight, 1);
            TargetArea target = TargetArea.CreateRectangular(1, 1);
            BoardWinConditionService service = new BoardWinConditionService();

            board.TryPlace(
                "piece_complete",
                new List<Vector3Int>
                {
                    new Vector3Int(0, 0, 0),
                    new Vector3Int(0, 1, 0)
                });

            FillState fill = service.CalculateFillState(board, target);

            Assert.AreEqual(1, fill.Layer0FilledCount);
            Assert.AreEqual(1, fill.Layer1FilledCount);
            Assert.AreEqual(target.TotalCells, fill.TotalTargetCells);
            Assert.IsTrue(fill.IsComplete);
        }

        [Test]
        public void ValidateSolution_WhenComplete_ReturnsSolved()
        {
            BoardState board = new BoardState(1, TargetArea.RequiredHeight, 1);
            TargetArea target = TargetArea.CreateRectangular(1, 1);
            BoardWinConditionService service = new BoardWinConditionService();

            board.TryPlace(
                "piece_complete",
                new List<Vector3Int>
                {
                    new Vector3Int(0, 0, 0),
                    new Vector3Int(0, 1, 0)
                });

            ValidationResult result = service.ValidateSolution(board, target);

            Assert.IsTrue(result.IsSolved);
            Assert.AreEqual(0, result.ErrorCount);
        }
    }
}
