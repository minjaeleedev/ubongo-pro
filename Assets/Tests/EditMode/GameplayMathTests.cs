using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Ubongo.Application.Bootstrap;

namespace Ubongo.Tests.EditMode
{
    public class GameplayMathTests
    {
        [Test]
        public void GetFootprintCells_RemovesHeightAndDuplicates()
        {
            List<Vector3Int> blocks = new List<Vector3Int>
            {
                new Vector3Int(0, 0, 0),
                new Vector3Int(0, 1, 0),
                new Vector3Int(1, 0, 0),
                new Vector3Int(1, 1, 0),
                new Vector3Int(0, 0, 1)
            };

            HashSet<Vector2Int> footprint = GameBoard.GetFootprintCells(blocks, new Vector3Int(2, 0, 3));

            CollectionAssert.AreEquivalent(
                new[]
                {
                    new Vector2Int(2, 3),
                    new Vector2Int(3, 3),
                    new Vector2Int(2, 4)
                },
                footprint
            );
        }

        [Test]
        public void CalculateOrthographicSizeFromBounds_IncreasesWithPadding()
        {
            Bounds bounds = new Bounds(Vector3.zero, new Vector3(10f, 2f, 4f));
            Quaternion rotation = Quaternion.Euler(35.264f, 45f, 0f);

            float withoutPadding = GameManager.CalculateOrthographicSizeFromBounds(bounds, rotation, 16f / 9f, 0f);
            float withPadding = GameManager.CalculateOrthographicSizeFromBounds(bounds, rotation, 16f / 9f, 1f);

            Assert.Greater(withPadding, withoutPadding);
        }

        [Test]
        public void CalculateOrthographicSizeFromBounds_NarrowAspectNeedsLargerSize()
        {
            Bounds bounds = new Bounds(Vector3.zero, new Vector3(8f, 2f, 6f));
            Quaternion rotation = Quaternion.Euler(35.264f, 45f, 0f);

            float wideSize = GameManager.CalculateOrthographicSizeFromBounds(bounds, rotation, 16f / 9f, 0f);
            float narrowSize = GameManager.CalculateOrthographicSizeFromBounds(bounds, rotation, 9f / 16f, 0f);

            Assert.Greater(narrowSize, wideSize);
        }

        [Test]
        public void RequiredLayers_AreConfigured()
        {
            Assert.GreaterOrEqual(LayerMask.NameToLayer("Board"), 0);
            Assert.GreaterOrEqual(LayerMask.NameToLayer("Piece"), 0);
        }

        [Test]
        public void BoardCell_UsesVisualChildRenderer_WhenAvailable()
        {
            GameObject boardObject = new GameObject("Board");
            GameBoard board = boardObject.AddComponent<GameBoard>();
            GameBoardFactory.EnsureConstructed(board);

            GameObject cellRoot = GameObject.CreatePrimitive(PrimitiveType.Cube);
            GameObject visualChild = GameObject.CreatePrimitive(PrimitiveType.Quad);
            visualChild.name = "Visual";
            visualChild.transform.SetParent(cellRoot.transform, false);

            BoardCell cell = cellRoot.AddComponent<BoardCell>();
            cell.Initialize(0, 0, 0, board);

            Assert.AreEqual(visualChild.GetComponent<Renderer>(), cell.VisualRenderer);

            Object.DestroyImmediate(cellRoot);
            Object.DestroyImmediate(boardObject);
        }

        [Test]
        public void BoardCell_FallsBackToRootRenderer_WhenVisualChildMissing()
        {
            GameObject boardObject = new GameObject("Board");
            GameBoard board = boardObject.AddComponent<GameBoard>();
            GameBoardFactory.EnsureConstructed(board);

            GameObject cellRoot = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Renderer rootRenderer = cellRoot.GetComponent<Renderer>();

            BoardCell cell = cellRoot.AddComponent<BoardCell>();
            cell.Initialize(0, 0, 0, board);

            Assert.AreEqual(rootRenderer, cell.VisualRenderer);

            Object.DestroyImmediate(cellRoot);
            Object.DestroyImmediate(boardObject);
        }

        [Test]
        public void PuzzlePiece_HeightProfile_AppliesExpectedLocalYOffset()
        {
            GameObject pieceObject = new GameObject("Piece");
            PuzzlePiece piece = pieceObject.AddComponent<PuzzlePiece>();

            piece.SetBlockPositions(new List<Vector3Int>
            {
                new Vector3Int(0, 0, 0),
                new Vector3Int(0, 1, 0)
            });

            float[] localY = pieceObject.transform
                .Cast<Transform>()
                .Where(child => child.name == "Cube")
                .Select(child => child.localPosition.y)
                .OrderBy(y => y)
                .ToArray();

            Assert.AreEqual(2, localY.Length);
            Assert.AreEqual(0.4f, localY[0], 0.001f);
            Assert.AreEqual(1.2f, localY[1], 0.001f);

            Object.DestroyImmediate(pieceObject);
        }

        [Test]
        public void PuzzlePiece_GetBlockPositions_NormalizesRotatedOffsets()
        {
            GameObject pieceObject = new GameObject("Piece");
            PuzzlePiece piece = pieceObject.AddComponent<PuzzlePiece>();
            piece.SetBlockPositions(new List<Vector3Int>
            {
                new Vector3Int(0, 0, 0),
                new Vector3Int(1, 0, 0)
            });

            pieceObject.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            List<Vector3Int> blocks = piece.GetBlockPositions();

            Assert.AreEqual(2, blocks.Count);
            Assert.IsTrue(blocks.All(b => b.x >= 0 && b.y >= 0 && b.z >= 0));
            CollectionAssert.AreEquivalent(
                new[]
                {
                    new Vector3Int(0, 0, 0),
                    new Vector3Int(1, 0, 0)
                },
                blocks
            );

            Object.DestroyImmediate(pieceObject);
        }
    }
}
