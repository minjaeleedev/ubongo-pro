using NUnit.Framework;
using UnityEngine;

namespace Ubongo.Tests.EditMode.Presentation.Board
{
    public class FloorTileViewTests
    {
        private GameObject tileRoot;
        private FloorTileView tile;
        private Renderer cellRenderer;
        private GameObject gridOverlay;
        private BoxCollider tileCollider;

        [SetUp]
        public void SetUp()
        {
            tileRoot = new GameObject("CellRoot");

            tileCollider = tileRoot.AddComponent<BoxCollider>();
            tileCollider.isTrigger = true;

            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Quad);
            visual.name = "Visual";
            visual.transform.SetParent(tileRoot.transform, false);
            cellRenderer = visual.GetComponent<Renderer>();

            gridOverlay = new GameObject("GridOverlay");
            gridOverlay.transform.SetParent(tileRoot.transform, false);

            tile = tileRoot.AddComponent<FloorTileView>();
            tile.Initialize(0, 0);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(tileRoot);
        }

        [Test]
        public void Apply_NonTargetNonOccupied_DisablesRenderer()
        {
            var state = new FloorTileVisualState(false, false, FloorTileHighlightMode.None);

            tile.Apply(state);

            Assert.IsFalse(cellRenderer.enabled);
        }

        [Test]
        public void Apply_TargetNonOccupied_EnablesRenderer()
        {
            var state = new FloorTileVisualState(true, false, FloorTileHighlightMode.None);

            tile.Apply(state);

            Assert.IsTrue(cellRenderer.enabled);
        }

        [Test]
        public void Apply_NonTargetThenTarget_ReEnablesRenderer()
        {
            tile.Apply(new FloorTileVisualState(false, false, FloorTileHighlightMode.None));
            Assert.IsFalse(cellRenderer.enabled);

            tile.Apply(new FloorTileVisualState(true, false, FloorTileHighlightMode.None));
            Assert.IsTrue(cellRenderer.enabled);
        }

        [Test]
        public void Apply_NonTarget_DisablesGridOverlay()
        {
            var state = new FloorTileVisualState(false, false, FloorTileHighlightMode.None);

            tile.Apply(state);

            Assert.IsFalse(gridOverlay.activeSelf);
        }

        [Test]
        public void Apply_Target_EnablesGridOverlay()
        {
            // First disable, then re-enable via target state
            tile.Apply(new FloorTileVisualState(false, false, FloorTileHighlightMode.None));
            tile.Apply(new FloorTileVisualState(true, false, FloorTileHighlightMode.None));

            Assert.IsTrue(gridOverlay.activeSelf);
        }

        [Test]
        public void Apply_NonTarget_DisablesCollider()
        {
            var state = new FloorTileVisualState(false, false, FloorTileHighlightMode.None);

            tile.Apply(state);

            Assert.IsFalse(tileCollider.enabled);
        }

        [Test]
        public void Apply_OccupiedAndTarget_RaisesGridOverlayAboveBaseY()
        {
            float baseY = gridOverlay.transform.localPosition.y;

            tile.Apply(new FloorTileVisualState(true, true, FloorTileHighlightMode.None));

            Assert.Greater(gridOverlay.transform.localPosition.y, baseY,
                "Grid overlay should be raised above base Y when occupied and target");
        }

        [Test]
        public void Apply_OccupiedAndTarget_ThenUnoccupied_RestoresGridOverlayBaseY()
        {
            float baseY = gridOverlay.transform.localPosition.y;

            tile.Apply(new FloorTileVisualState(true, true, FloorTileHighlightMode.None));
            tile.Apply(new FloorTileVisualState(true, false, FloorTileHighlightMode.None));

            Assert.That(gridOverlay.transform.localPosition.y, Is.EqualTo(baseY).Within(0.001f),
                "Grid overlay should return to base Y when no longer occupied");
        }

        [Test]
        public void Apply_OccupiedButNotTarget_DoesNotRaiseGridOverlay()
        {
            float baseY = gridOverlay.transform.localPosition.y;

            tile.Apply(new FloorTileVisualState(false, true, FloorTileHighlightMode.None));

            Assert.That(gridOverlay.transform.localPosition.y, Is.EqualTo(baseY).Within(0.001f),
                "Grid overlay should not be raised when occupied but not target");
        }

        [Test]
        public void FloorTileView_UsesVisualChildRenderer_WhenAvailable()
        {
            GameObject cellRoot = GameObject.CreatePrimitive(PrimitiveType.Cube);
            GameObject visualChild = GameObject.CreatePrimitive(PrimitiveType.Quad);
            visualChild.name = "Visual";
            visualChild.transform.SetParent(cellRoot.transform, false);

            FloorTileView cell = cellRoot.AddComponent<FloorTileView>();
            cell.Initialize(0, 0);

            Assert.AreEqual(visualChild.GetComponent<Renderer>(), cell.VisualRenderer);

            Object.DestroyImmediate(cellRoot);
        }

        [Test]
        public void FloorTileView_FallsBackToRootRenderer_WhenVisualChildMissing()
        {
            GameObject cellRoot = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Renderer rootRenderer = cellRoot.GetComponent<Renderer>();

            FloorTileView cell = cellRoot.AddComponent<FloorTileView>();
            cell.Initialize(0, 0);

            Assert.AreEqual(rootRenderer, cell.VisualRenderer);

            Object.DestroyImmediate(cellRoot);
        }
    }
}
