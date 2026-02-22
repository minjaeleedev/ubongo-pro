using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Ubongo.Domain.Board;

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
                    Object.DestroyImmediate(spawnedObjects[i]);
                }
            }
            spawnedObjects.Clear();
        }

        [Test]
        public void InitializeGrid_WithZeroDimensions_ClampsToMinimumSize()
        {
            GameBoard board = CreateBoard();

            board.InitializeGrid(new Vector3Int(0, 2, 0));

            Assert.AreEqual(1, board.Width);
            Assert.AreEqual(1, board.Depth);

            BoardState state = GetBoardState(board);
            Assert.NotNull(state);
            Assert.AreEqual(1, state.Width);
            Assert.AreEqual(2, state.Height);
            Assert.AreEqual(1, state.Depth);
        }

        [Test]
        public void RemovePieceFallback_RebuildsRemainingEntriesByPieceId()
        {
            GameBoard board = CreateBoard();
            board.InitializeGrid(new Vector3Int(4, 2, 2));

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

            BoardState state = GetBoardState(board);
            Assert.NotNull(state);
            state.Clear();

            board.RemovePiece(pieceA);

            bool removed = state.Remove(pieceB.GetInstanceID().ToString(), out IReadOnlyList<Vector3Int> removedCells);
            Assert.IsTrue(removed);
            Assert.NotNull(removedCells);
            Assert.AreEqual(1, removedCells.Count);
            Assert.AreEqual(new Vector3Int(3, 0, 1), removedCells[0]);
        }

        private GameBoard CreateBoard()
        {
            GameObject boardObject = new GameObject("GameBoard_Test");
            spawnedObjects.Add(boardObject);

            GameBoard board = boardObject.AddComponent<GameBoard>();
            if (GetBoardState(board) == null)
            {
                InvokeNonPublic(board, "Awake");
            }

            return board;
        }

        private PuzzlePiece CreatePiece(string name, List<Vector3Int> blocks)
        {
            GameObject pieceObject = new GameObject(name);
            spawnedObjects.Add(pieceObject);

            PuzzlePiece piece = pieceObject.AddComponent<PuzzlePiece>();
            piece.SetBlockPositions(blocks);
            return piece;
        }

        private static BoardState GetBoardState(GameBoard board)
        {
            FieldInfo field = typeof(GameBoard).GetField("boardState", BindingFlags.NonPublic | BindingFlags.Instance);
            return field?.GetValue(board) as BoardState;
        }

        private static void InvokeNonPublic(object target, string methodName)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method, $"Expected method '{methodName}' was not found.");
            method.Invoke(target, null);
        }
    }
}
