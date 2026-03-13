using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Ubongo.Core;

namespace Ubongo.Tests.EditMode
{
    public class RoundTransitionCleanupTests
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
        public void InitializeGrid_CalledTwice_OldBoardContainerIsInactive()
        {
            GameBoard board = CreateBoard();
            board.InitializeGrid(new Vector3Int(3, TargetArea.RequiredHeight, 3));

            Transform oldContainer = board.transform.Find("BoardContainer");
            Assert.IsNotNull(oldContainer, "First BoardContainer should exist");

            board.InitializeGrid(new Vector3Int(2, TargetArea.RequiredHeight, 2));

            Assert.IsTrue(oldContainer == null || !oldContainer.gameObject.activeSelf,
                "Old BoardContainer should be inactive or destroyed after re-initialization");
        }

        [Test]
        public void InitializeGrid_CalledTwice_OnlyOneChildRemains()
        {
            GameBoard board = CreateBoard();
            board.InitializeGrid(new Vector3Int(3, TargetArea.RequiredHeight, 3));
            Assert.AreEqual(1, board.transform.childCount, "First init should have 1 child");

            board.InitializeGrid(new Vector3Int(2, TargetArea.RequiredHeight, 2));
            Assert.AreEqual(1, board.transform.childCount,
                "After re-init, old container should be unparented; only new container remains");
        }

        [Test]
        public void InitializeGrid_CalledTwice_NewFloorTilesCreated()
        {
            GameBoard board = CreateBoard();
            board.InitializeGrid(new Vector3Int(3, TargetArea.RequiredHeight, 3));

            board.InitializeGrid(new Vector3Int(2, TargetArea.RequiredHeight, 2));

            Assert.AreEqual(2, board.Width);
            Assert.AreEqual(2, board.Depth);
            Assert.IsNotNull(board.GetCell(0, 0, 0));
            Assert.IsNotNull(board.GetCell(1, 0, 1));
            Assert.IsNull(board.GetCell(2, 0, 0));
        }

        [Test]
        public void InitializeGrid_CalledTwice_ClearsOccupancy()
        {
            GameBoard board = CreateBoard();
            board.InitializeGrid(new Vector3Int(4, TargetArea.RequiredHeight, 2));

            PuzzlePiece piece = CreatePiece("Piece", new List<Vector3Int>
            {
                new Vector3Int(0, 0, 0),
                new Vector3Int(1, 0, 0)
            });
            board.PlacePiece(piece, new Vector3Int(0, 0, 0));
            Assert.IsTrue(board.IsOccupied(0, 0, 0));

            board.InitializeGrid(new Vector3Int(4, TargetArea.RequiredHeight, 2));

            Assert.IsFalse(board.IsOccupied(0, 0, 0));
            Assert.IsFalse(board.IsOccupied(1, 0, 0));
        }

        [Test]
        public void InitializeGrid_CalledTwice_OldHighlightsCleared()
        {
            GameBoard board = CreateBoard();
            board.InitializeGrid(new Vector3Int(4, TargetArea.RequiredHeight, 2));

            PuzzlePiece piece = CreatePiece("Piece", new List<Vector3Int>
            {
                new Vector3Int(0, 0, 0)
            });
            board.HighlightValidPlacement(new Vector3Int(0, 0, 0), piece);

            board.InitializeGrid(new Vector3Int(4, TargetArea.RequiredHeight, 2));

            FloorTileView cell = board.GetCell(0, 0, 0);
            Assert.IsNotNull(cell);
            // After re-initialization, the cell should be in default target state
            // (not highlighted). The renderer should be enabled because it's a target cell.
            Assert.IsTrue(cell.VisualRenderer.enabled);
        }

        [Test]
        public void InitializeGrid_DefaultTilePath_CellsAreChildrenOfBoardContainer()
        {
            GameBoard board = CreateBoard();
            board.InitializeGrid(new Vector3Int(3, TargetArea.RequiredHeight, 3));

            Transform container = board.transform.Find("BoardContainer");
            Assert.IsNotNull(container);

            int cellCount = 0;
            foreach (Transform child in container)
            {
                if (child.name.StartsWith("Cell_"))
                {
                    cellCount++;
                }
            }

            Assert.AreEqual(9, cellCount,
                "All 9 cells should be children of BoardContainer, not scene root");
        }

        [Test]
        public void InitializeGrid_CalledTwice_NoCellsRemainAtSceneRoot()
        {
            GameBoard board = CreateBoard();
            board.InitializeGrid(new Vector3Int(3, TargetArea.RequiredHeight, 3));
            board.InitializeGrid(new Vector3Int(2, TargetArea.RequiredHeight, 2));

            GameObject[] allObjects = Object.FindObjectsByType<GameObject>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            var orphanCells = new List<GameObject>();
            foreach (GameObject obj in allObjects)
            {
                if (obj.name.StartsWith("Cell_") && obj.transform.parent == null)
                {
                    orphanCells.Add(obj);
                }
            }

            Assert.AreEqual(0, orphanCells.Count,
                $"Found {orphanCells.Count} orphan Cell(s) at scene root: " +
                string.Join(", ", orphanCells.ConvertAll(o => o.name)));

            foreach (GameObject orphan in orphanCells)
            {
                Object.DestroyImmediate(orphan);
            }
        }

        private GameBoard CreateBoard()
        {
            GameObject boardObject = new GameObject("GameBoard_Test");
            spawnedObjects.Add(boardObject);
            GameBoard board = boardObject.AddComponent<GameBoard>();
            board.Construct(BoardRuntimeServices.CreateDefault());
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
    }
}
