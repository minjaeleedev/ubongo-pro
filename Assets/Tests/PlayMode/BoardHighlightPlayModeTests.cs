using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Ubongo.Core;

namespace Ubongo.Tests.PlayMode
{
    public class BoardHighlightPlayModeTests
    {
        private static readonly int BaseColorPropertyId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorPropertyId = Shader.PropertyToID("_Color");

        [UnityTest]
        public IEnumerator ClearHighlights_RestoresTargetColor()
        {
            GameBoard board = CreateInitializedBoard();
            GameObject boardObject = board.gameObject;
            yield return null;

            board.SetTargetArea(TargetArea.CreateRectangular(board.Width, board.Depth));
            FloorTileView targetCell = board.GetCell(0, 0, 0);
            Assert.IsNotNull(targetCell);

            Renderer renderer = targetCell.VisualRenderer;
            Assert.IsNotNull(renderer);
            Color baseColor = GetRendererColor(renderer);

            GameObject pieceObject = new GameObject("Piece");
            PuzzlePiece piece = pieceObject.AddComponent<PuzzlePiece>();
            piece.SetBlockPositions(new List<Vector3Int> { Vector3Int.zero });
            yield return null;

            board.HighlightValidPlacement(Vector3Int.zero, piece);
            Assert.IsFalse(AreColorsClose(baseColor, GetRendererColor(renderer)));

            board.ClearHighlights();
            yield return null;
            Assert.IsTrue(AreColorsClose(baseColor, GetRendererColor(renderer)));

            Object.DestroyImmediate(pieceObject);
            Object.DestroyImmediate(boardObject);
        }

        [UnityTest]
        public IEnumerator HighlightValidPlacement_ProjectsToGroundLayerFootprint()
        {
            GameBoard board = CreateInitializedBoard();
            GameObject boardObject = board.gameObject;
            yield return null;

            board.SetTargetArea(TargetArea.CreateRectangular(board.Width, board.Depth));

            GameObject pieceObject = new GameObject("Piece");
            PuzzlePiece piece = pieceObject.AddComponent<PuzzlePiece>();
            piece.SetBlockPositions(new List<Vector3Int>
            {
                new Vector3Int(0, 0, 0),
                new Vector3Int(0, 1, 0)
            });
            yield return null;

            FloorTileView floorCell = board.GetCell(0, 0, 0);
            FloorTileView upperCell = board.GetCell(0, 1, 0);
            Assert.IsNotNull(floorCell);
            Assert.IsNull(upperCell);

            Renderer floorRenderer = floorCell.VisualRenderer;
            Assert.IsNotNull(floorRenderer);

            Color floorBaseColor = GetRendererColor(floorRenderer);
            board.HighlightValidPlacement(Vector3Int.zero, piece);
            yield return null;

            Assert.IsFalse(AreColorsClose(floorBaseColor, GetRendererColor(floorRenderer)));

            Object.DestroyImmediate(pieceObject);
            Object.DestroyImmediate(boardObject);
        }

        [UnityTest]
        public IEnumerator TryGetBoardHit_FindsBoard_WhenPieceColliderIsInFront()
        {
            yield return DestroyInputManagerIfExists();

            GameObject cameraObject = new GameObject("Main Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            cameraObject.tag = "MainCamera";
            camera.transform.position = new Vector3(0f, 5f, -6f);
            camera.transform.rotation = Quaternion.Euler(35f, 0f, 0f);

            GameObject inputObject = new GameObject("InputManager");
            InputManager inputManager = inputObject.AddComponent<InputManager>();
            yield return null;

            Vector2 pointerPosition = camera.pixelRect.center;
            inputManager.SetPointerPosition(pointerPosition);
            Ray pointerRay = camera.ScreenPointToRay(pointerPosition);

            GameBoard board = CreateInitializedBoard();
            GameObject boardObject = board.gameObject;
            boardObject.transform.position = pointerRay.origin + (pointerRay.direction * 6f);
            yield return null;

            GameObject blocker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            blocker.transform.position = pointerRay.origin + (pointerRay.direction * 3f);
            blocker.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);

            inputManager.SetPointerPosition(pointerPosition);
            bool hitBoard = inputManager.TryGetBoardHit(out RaycastHit hit);
            Assert.IsTrue(hitBoard);
            Assert.IsNotNull(hit.collider);
            Assert.IsNotNull(hit.collider.GetComponentInParent<GameBoard>());

            Object.DestroyImmediate(inputObject);
            Object.DestroyImmediate(blocker);
            Object.DestroyImmediate(boardObject);
            Object.DestroyImmediate(cameraObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator PlacePiece_PublicApi_PlacesPieceOnValidCell()
        {
            GameBoard board = CreateInitializedBoard();
            GameObject boardObject = board.gameObject;
            yield return null;

            board.SetTargetArea(TargetArea.CreateRectangular(board.Width, board.Depth));

            GameObject pieceObject = new GameObject("Piece");
            PuzzlePiece piece = pieceObject.AddComponent<PuzzlePiece>();
            piece.SetBlockPositions(new List<Vector3Int> { Vector3Int.zero });
            yield return null;

            Assert.IsTrue(board.CanPlacePiece(piece, Vector3Int.zero));
            board.PlacePiece(piece, Vector3Int.zero);
            yield return null;

            Assert.IsTrue(board.IsOccupied(0, 0, 0));
            Assert.IsTrue(piece.IsPlaced);

            Object.DestroyImmediate(pieceObject);
            Object.DestroyImmediate(boardObject);
        }

        [UnityTest]
        public IEnumerator PlacePiece_PublicApi_WithRotation_FillsExpectedCells()
        {
            GameBoard board = CreateInitializedBoard();
            GameObject boardObject = board.gameObject;
            yield return null;

            board.SetTargetArea(TargetArea.CreateRectangular(board.Width, board.Depth));

            GameObject pieceObject = new GameObject("Piece");
            PuzzlePiece piece = pieceObject.AddComponent<PuzzlePiece>();
            piece.SetBlockPositions(new List<Vector3Int>
            {
                new Vector3Int(0, 0, 0),
                new Vector3Int(1, 0, 0)
            });
            yield return null;

            piece.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            Assert.IsTrue(board.CanPlacePiece(piece, Vector3Int.zero));
            board.PlacePiece(piece, Vector3Int.zero);
            yield return null;

            Assert.IsTrue(board.IsOccupied(0, 0, 0));
            Assert.IsTrue(board.IsOccupied(1, 0, 0));

            Object.DestroyImmediate(pieceObject);
            Object.DestroyImmediate(boardObject);
        }

        [UnityTest]
        public IEnumerator PlacePiece_PublicApi_OutOfBounds_DoesNotPlaceAndKeepsRotation()
        {
            GameBoard board = CreateInitializedBoard();
            GameObject boardObject = board.gameObject;
            yield return null;

            board.SetTargetArea(TargetArea.CreateRectangular(board.Width, board.Depth));

            GameObject pieceObject = new GameObject("Piece");
            PuzzlePiece piece = pieceObject.AddComponent<PuzzlePiece>();
            piece.SetBlockPositions(new List<Vector3Int> { Vector3Int.zero });
            yield return null;

            Quaternion expectedRotation = Quaternion.Euler(0f, 90f, 0f);
            piece.transform.rotation = expectedRotation;
            Vector3Int invalidGridPosition = new Vector3Int(-4, 0, -4);
            Assert.AreEqual(PlacementValidity.OutOfBounds, board.ValidatePlacement(piece, invalidGridPosition));

            board.PlacePiece(piece, invalidGridPosition);
            yield return null;

            Assert.Less(Quaternion.Angle(piece.transform.rotation, expectedRotation), 0.1f);
            Assert.IsFalse(piece.IsPlaced);
            Assert.IsFalse(board.IsOccupied(0, 0, 0));

            Object.DestroyImmediate(pieceObject);
            Object.DestroyImmediate(boardObject);
        }

        [UnityTest]
        public IEnumerator PieceVisualSpacing_MatchesBoardGridStep()
        {
            GameBoard board = CreateInitializedBoard();
            GameObject boardObject = board.gameObject;
            yield return null;

            GameObject pieceObject = new GameObject("Piece");
            PuzzlePiece piece = pieceObject.AddComponent<PuzzlePiece>();
            piece.SetBlockPositions(new List<Vector3Int>
            {
                new Vector3Int(0, 0, 0),
                new Vector3Int(1, 0, 0)
            });
            yield return null;

            float[] xPositions = pieceObject.transform
                .Cast<Transform>()
                .Where(child => child.name == "Cube")
                .Select(child => child.localPosition.x)
                .OrderBy(x => x)
                .ToArray();

            Assert.AreEqual(2, xPositions.Length);
            Assert.AreEqual(board.GridStep, xPositions[1] - xPositions[0], 0.001f);

            Object.DestroyImmediate(pieceObject);
            Object.DestroyImmediate(boardObject);
        }

        [UnityTest]
        public IEnumerator FloorTiles_ShowExplicitGridOverlay_MatchingBoardFootprint()
        {
            GameBoard board = CreateInitializedBoard();
            GameObject boardObject = board.gameObject;
            yield return null;

            FloorTileView floorCell = board.GetCell(0, 0, 0);
            Assert.IsNotNull(floorCell);

            Transform floorOverlay = floorCell.transform.Find("GridOverlay");
            Assert.IsNotNull(floorOverlay);

            LineRenderer floorLine = floorOverlay.GetComponent<LineRenderer>();
            Assert.IsNotNull(floorLine);
            Assert.IsTrue(floorLine.enabled);
            Assert.AreEqual(4, floorLine.positionCount);

            float overlayWidth = Vector3.Distance(floorLine.GetPosition(0), floorLine.GetPosition(3));
            Assert.AreEqual(board.BoardFootprintSize, overlayWidth, 0.001f);

            Assert.IsNull(board.GetCell(0, 1, 0));

            Object.DestroyImmediate(boardObject);
        }

        [UnityTest]
        public IEnumerator PieceBlockWidth_MatchesBoardFootprintSize()
        {
            GameBoard board = CreateInitializedBoard();
            GameObject boardObject = board.gameObject;
            yield return null;

            GameObject pieceObject = new GameObject("Piece");
            PuzzlePiece piece = pieceObject.AddComponent<PuzzlePiece>();
            piece.SetBlockPositions(new List<Vector3Int> { Vector3Int.zero });
            yield return null;

            Transform cube = pieceObject.transform.Cast<Transform>().FirstOrDefault(child => child.name == "Cube");
            Assert.IsNotNull(cube);
            Assert.AreEqual(board.BoardFootprintSize, cube.localScale.x, 0.001f);
            Assert.AreEqual(board.BoardFootprintSize, cube.localScale.z, 0.001f);

            Object.DestroyImmediate(pieceObject);
            Object.DestroyImmediate(boardObject);
        }

        [UnityTest]
        public IEnumerator TargetColor_IsSeparatedFromValidHighlightColor()
        {
            GameBoard board = CreateInitializedBoard();
            GameObject boardObject = board.gameObject;
            yield return null;

            FloorTileView floorCell = board.GetCell(0, 0, 0);
            Assert.IsNotNull(floorCell);
            Renderer renderer = floorCell.VisualRenderer;
            Assert.IsNotNull(renderer);

            Color targetColor = GetRendererColor(renderer);
            GameObject pieceObject = new GameObject("Piece");
            PuzzlePiece piece = pieceObject.AddComponent<PuzzlePiece>();
            piece.SetBlockPositions(new List<Vector3Int> { Vector3Int.zero });
            yield return null;

            board.HighlightValidPlacement(Vector3Int.zero, piece);
            Color validColor = GetRendererColor(renderer);

            Assert.Greater(ColorDistance(targetColor, validColor), 0.35f);

            Object.DestroyImmediate(pieceObject);
            Object.DestroyImmediate(boardObject);
        }

        [UnityTest]
        public IEnumerator InitializeGrid_DefaultCellVisual_KeepsFootprintScale()
        {
            GameBoard board = CreateInitializedBoard();
            GameObject boardObject = board.gameObject;
            yield return null;

            board.InitializeGrid(new Vector3Int(6, TargetArea.RequiredHeight, 3));
            FloorTileView floorCell = board.GetCell(0, 0, 0);
            Assert.IsNotNull(floorCell);
            Transform visualTransform = floorCell.transform.Find("Visual");
            Assert.IsNotNull(visualTransform);
            Assert.AreEqual(board.BoardFootprintSize, visualTransform.localScale.x, 0.001f);
            Assert.AreEqual(board.BoardFootprintSize, visualTransform.localScale.y, 0.001f);

            Object.DestroyImmediate(boardObject);
        }

        private static IEnumerator DestroyInputManagerIfExists()
        {
            if (InputManager.Instance != null)
            {
                Object.DestroyImmediate(InputManager.Instance.gameObject);
            }
            yield return null;
        }

        private static GameBoard CreateInitializedBoard()
        {
            GameObject boardObject = new GameObject("Board");
            GameBoard board = boardObject.AddComponent<GameBoard>();
            board.Construct(BoardRuntimeServices.CreateDefault());
            board.InitializeGrid(new Vector3Int(4, TargetArea.RequiredHeight, 2));
            return board;
        }

        private static bool AreColorsClose(Color a, Color b, float tolerance = 0.01f)
        {
            return Mathf.Abs(a.r - b.r) <= tolerance &&
                   Mathf.Abs(a.g - b.g) <= tolerance &&
                   Mathf.Abs(a.b - b.b) <= tolerance &&
                   Mathf.Abs(a.a - b.a) <= tolerance;
        }

        private static Color GetRendererColor(Renderer renderer)
        {
            Assert.IsNotNull(renderer);

            Material shared = renderer.sharedMaterial;
            int colorPropertyId = ResolveColorPropertyId(shared);

            if (renderer.HasPropertyBlock())
            {
                MaterialPropertyBlock block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block);
                return block.GetColor(colorPropertyId);
            }

            if (shared != null && shared.HasProperty(colorPropertyId))
            {
                return shared.GetColor(colorPropertyId);
            }

            return Color.white;
        }

        private static int ResolveColorPropertyId(Material material)
        {
            if (material != null)
            {
                if (material.HasProperty(BaseColorPropertyId))
                {
                    return BaseColorPropertyId;
                }

                if (material.HasProperty(ColorPropertyId))
                {
                    return ColorPropertyId;
                }
            }

            return ColorPropertyId;
        }

        private static float ColorDistance(Color a, Color b)
        {
            float dr = a.r - b.r;
            float dg = a.g - b.g;
            float db = a.b - b.b;
            return Mathf.Sqrt((dr * dr) + (dg * dg) + (db * db));
        }
    }
}
