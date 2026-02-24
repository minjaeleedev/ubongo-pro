using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Ubongo.Core;
using Ubongo.Domain.Board;

namespace Ubongo.Tests.EditMode
{
    public class BoardStateTests
    {
        [Test]
        public void TryPlaceAndRemove_UpdatesOccupancyAndIndex()
        {
            BoardState board = new BoardState(4, TargetArea.RequiredHeight, 2);
            List<Vector3Int> cells = new List<Vector3Int>
            {
                new Vector3Int(1, 0, 0),
                new Vector3Int(1, 1, 0)
            };

            bool placed = board.TryPlace("piece_a", cells);
            Assert.IsTrue(placed);
            Assert.IsTrue(board.IsOccupied(cells[0]));
            Assert.IsTrue(board.IsOccupied(cells[1]));

            bool removed = board.Remove("piece_a", out IReadOnlyList<Vector3Int> removedCells);
            Assert.IsTrue(removed);
            Assert.AreEqual(2, removedCells.Count);
            Assert.IsFalse(board.IsOccupied(cells[0]));
            Assert.IsFalse(board.IsOccupied(cells[1]));

            bool removedAgain = board.Remove("piece_a", out _);
            Assert.IsFalse(removedAgain);
        }

        [Test]
        public void CopyOccupancyTo_CopiesCurrentOccupancy()
        {
            BoardState board = new BoardState(2, TargetArea.RequiredHeight, 1);
            board.TryPlace("piece_a", new List<Vector3Int>
            {
                new Vector3Int(0, 0, 0),
                new Vector3Int(1, 1, 0)
            });

            bool[,,] buffer = new bool[2, TargetArea.RequiredHeight, 1];
            board.CopyOccupancyTo(buffer);

            Assert.IsTrue(buffer[0, 0, 0]);
            Assert.IsTrue(buffer[1, 1, 0]);
            Assert.IsFalse(buffer[1, 0, 0]);
            Assert.IsFalse(buffer[0, 1, 0]);
        }

        [Test]
        public void CopyOccupancyTo_DimensionMismatch_ThrowsArgumentException()
        {
            BoardState board = new BoardState(2, TargetArea.RequiredHeight, 1);

            Assert.Throws<ArgumentException>(() => board.CopyOccupancyTo(new bool[1, TargetArea.RequiredHeight, 1]));
            Assert.Throws<ArgumentException>(() => board.CopyOccupancyTo(new bool[2, 1, 1]));
            Assert.Throws<ArgumentException>(() => board.CopyOccupancyTo(new bool[2, TargetArea.RequiredHeight, 2]));
        }

        [Test]
        public void Resize_WithNonPositiveValues_ClampsToMinimumDimensionOne()
        {
            BoardState board = new BoardState(2, TargetArea.RequiredHeight, 2);

            board.Resize(0, -1, 0);

            Assert.AreEqual(1, board.Width);
            Assert.AreEqual(1, board.Height);
            Assert.AreEqual(1, board.Depth);
        }
    }
}
