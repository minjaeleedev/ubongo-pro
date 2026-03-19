using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Ubongo.Core;
using Ubongo.Domain.Board;

namespace Ubongo.Tests.EditMode.Domain.Board
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

        [Test]
        public void ValidateSolution_ShapedTargetArea_SolvesWhenOnlyTargetCellsFilled()
        {
            // L-shaped: 3x3 bounding box with 2x2 corner cut = 7 footprint cells
            TargetArea target = TargetArea.CreateLShaped(3, 3, 2, 1);
            BoardState board = new BoardState(3, TargetArea.RequiredHeight, 3);
            BoardWinConditionService service = new BoardWinConditionService();

            // Fill only the 7 target columns × 2 layers
            int pieceIndex = 0;
            foreach (Vector2Int col in target.GetColumnPositions())
            {
                board.TryPlace(
                    $"piece_{pieceIndex++}",
                    new List<Vector3Int>
                    {
                        new Vector3Int(col.x, 0, col.y),
                        new Vector3Int(col.x, 1, col.y)
                    });
            }

            ValidationResult result = service.ValidateSolution(board, target);

            Assert.IsTrue(result.IsSolved);
        }
    }
}
