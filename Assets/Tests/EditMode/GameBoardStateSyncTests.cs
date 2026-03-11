using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Ubongo.Core;

namespace Ubongo.Tests.EditMode
{
    public class GameBoardStateSyncTests
    {
        private readonly List<GameObject> spawnedObjects = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < spawnedObjects.Count; i++)
            {
                if (spawnedObjects[i] != null)
                {
                    UnityEngine.Object.DestroyImmediate(spawnedObjects[i]);
                }
            }
            spawnedObjects.Clear();
        }

        [Test]
        public void InitializeGrid_WithZeroDimensions_ClampsToMinimumSize()
        {
            GameBoard board = CreateBoard();

            board.InitializeGrid(new Vector3Int(0, TargetArea.RequiredHeight, 0));

            Assert.AreEqual(1, board.Width);
            Assert.AreEqual(1, board.Depth);
            Assert.IsNotNull(board.GetCell(0, 0, 0));
            Assert.IsNull(board.GetCell(0, 1, 0));
            Assert.IsNull(board.GetCell(1, 0, 0));
        }

        [Test]
        public void InitializeGrid_IgnoresSizeY_UsesRequiredHeight()
        {
            GameBoard board = CreateBoard();

            board.InitializeGrid(new Vector3Int(3, TargetArea.RequiredHeight + 5, 2));

            Assert.AreEqual(TargetArea.RequiredHeight, board.Height);
            Assert.IsNull(board.GetCell(0, TargetArea.RequiredHeight - 1, 0));
            Assert.IsNull(board.GetCell(0, TargetArea.RequiredHeight, 0));
        }

        [Test]
        public void RemovePiece_OnlyClearsRequestedPieceCells()
        {
            GameBoard board = CreateBoard();
            board.InitializeGrid(new Vector3Int(4, TargetArea.RequiredHeight, 2));

            PuzzlePiece pieceA = CreatePiece("PieceA", new List<Vector3Int>
            {
                new Vector3Int(0, 0, 0),
                new Vector3Int(1, 0, 0)
            });
            PuzzlePiece pieceB = CreatePiece("PieceB", new List<Vector3Int>
            {
                new Vector3Int(0, 0, 0)
            });

            board.PlacePiece(pieceA, new Vector3Int(0, 0, 0));
            board.PlacePiece(pieceB, new Vector3Int(3, 0, 1));

            Assert.IsTrue(board.IsOccupied(0, 0, 0));
            Assert.IsTrue(board.IsOccupied(1, 0, 0));
            Assert.IsTrue(board.IsOccupied(3, 0, 1));

            board.RemovePiece(pieceA);

            Assert.IsFalse(board.IsOccupied(0, 0, 0));
            Assert.IsFalse(board.IsOccupied(1, 0, 0));
            Assert.IsTrue(board.IsOccupied(3, 0, 1));
            Assert.IsFalse(pieceA.IsPlaced);
            Assert.IsTrue(pieceB.IsPlaced);
        }

        [Test]
        public void Construct_WhenCalledTwice_ThrowsInvalidOperationException()
        {
            GameBoard board = CreateUninitializedBoard();
            board.Construct(BoardRuntimeServices.CreateDefault());

            Assert.Throws<InvalidOperationException>(() => board.Construct(BoardRuntimeServices.CreateDefault()));
        }

        [Test]
        public void InitializeGrid_WithoutConstruct_ThrowsInvalidOperationException()
        {
            GameBoard board = CreateUninitializedBoard();

            Assert.Throws<InvalidOperationException>(() => board.InitializeGrid(new Vector3Int(1, TargetArea.RequiredHeight, 1)));
        }

        [Test]
        public void ValidatePlacement_WithoutConstruct_ThrowsInvalidOperationException()
        {
            GameBoard board = CreateUninitializedBoard();
            PuzzlePiece piece = CreatePiece("Piece", new List<Vector3Int> { Vector3Int.zero });

            Assert.Throws<InvalidOperationException>(() => board.ValidatePlacement(piece, Vector3Int.zero));
        }

        [Test]
        public void Construct_WhenCalledOnce_EnablesBoardInitialization()
        {
            GameBoard board = CreateUninitializedBoard();
            board.Construct(BoardRuntimeServices.CreateDefault());

            board.InitializeGrid(new Vector3Int(1, TargetArea.RequiredHeight, 1));

            Assert.IsTrue(board.IsConstructed);
            Assert.IsNotNull(board.GetCell(0, 0, 0));
        }

        private GameBoard CreateBoard()
        {
            GameBoard board = CreateUninitializedBoard();
            if (!board.IsConstructed)
            {
                board.Construct(BoardRuntimeServices.CreateDefault());
            }

            return board;
        }

        private GameBoard CreateUninitializedBoard()
        {
            GameObject boardObject = new GameObject("GameBoard_Test");
            spawnedObjects.Add(boardObject);
            return boardObject.AddComponent<GameBoard>();
        }

        private PuzzlePiece CreatePiece(string name, List<Vector3Int> blocks)
        {
            GameObject pieceObject = new GameObject(name);
            spawnedObjects.Add(pieceObject);
            PuzzlePiece piece = pieceObject.AddComponent<PuzzlePiece>();
            piece.SetBlockPositions(blocks);
            return piece;
        }
    }
}
