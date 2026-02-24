using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Ubongo.Core;
using Ubongo.Domain.Board;

namespace Ubongo.Tests.EditMode
{
    public class BoardPlacementServiceTests
    {
        [Test]
        public void Validate_OutOfBounds_ReturnsOutOfBounds()
        {
            BoardState board = new BoardState(4, TargetArea.RequiredHeight, 2);
            TargetArea target = TargetArea.CreateRectangular(4, 2);
            BoardPlacementService service = new BoardPlacementService();

            PlacementValidity result = service.Validate(
                board,
                target,
                new List<Vector3Int> { new Vector3Int(-1, 0, 0) });

            Assert.AreEqual(PlacementValidity.OutOfBounds, result);
        }

        [Test]
        public void Validate_HeightExceeded_ReturnsHeightExceeded()
        {
            BoardState board = new BoardState(4, TargetArea.RequiredHeight, 2);
            TargetArea target = TargetArea.CreateRectangular(4, 2);
            BoardPlacementService service = new BoardPlacementService();

            PlacementValidity result = service.Validate(
                board,
                target,
                new List<Vector3Int> { new Vector3Int(0, TargetArea.RequiredHeight, 0) });

            Assert.AreEqual(PlacementValidity.HeightExceeded, result);
        }

        [Test]
        public void Validate_OutsideTarget_ReturnsOutsideTarget()
        {
            BoardState board = new BoardState(4, TargetArea.RequiredHeight, 2);
            TargetArea target = TargetArea.CreateRectangular(1, 1);
            BoardPlacementService service = new BoardPlacementService();

            PlacementValidity result = service.Validate(
                board,
                target,
                new List<Vector3Int> { new Vector3Int(1, 0, 0) });

            Assert.AreEqual(PlacementValidity.OutsideTarget, result);
        }

        [Test]
        public void Validate_Collision_ReturnsCollision()
        {
            BoardState board = new BoardState(4, TargetArea.RequiredHeight, 2);
            TargetArea target = TargetArea.CreateRectangular(4, 2);
            BoardPlacementService service = new BoardPlacementService();
            board.TryPlace("piece_a", new List<Vector3Int> { new Vector3Int(0, 0, 0) });

            PlacementValidity result = service.Validate(
                board,
                target,
                new List<Vector3Int> { new Vector3Int(0, 0, 0) });

            Assert.AreEqual(PlacementValidity.Collision, result);
        }

        [Test]
        public void Validate_ValidPlacement_ReturnsValid()
        {
            BoardState board = new BoardState(4, TargetArea.RequiredHeight, 2);
            TargetArea target = TargetArea.CreateRectangular(4, 2);
            BoardPlacementService service = new BoardPlacementService();

            PlacementValidity result = service.Validate(
                board,
                target,
                new List<Vector3Int> { new Vector3Int(1, 0, 1) });

            Assert.AreEqual(PlacementValidity.Valid, result);
        }
    }
}
