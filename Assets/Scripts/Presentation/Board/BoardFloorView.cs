using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Ubongo
{
    public sealed class BoardFloorView : IDisposable
    {
        private const string CellVisualName = "Visual";
        private const string CellGridOverlayName = "GridOverlay";

        private readonly Transform ownerTransform;
        private readonly int boardLayerIndex;
        private readonly GameObject cellPrefab;
        private readonly Material defaultMaterial;
        private readonly bool showCellGrid;
        private readonly Color gridLineColor;
        private readonly float gridLineWidth;
        private readonly float gridLineYOffset;

        private GameObject boardContainer;
        private Material gridLineMaterial;
        private FloorTileView[,] floorTiles;
        private float cellSize;
        private float cellSpacing;
        private float boardFootprintSize;
        private int width;
        private int depth;

        public BoardFloorView(
            Transform ownerTransform,
            int boardLayerIndex,
            GameObject cellPrefab,
            Material defaultMaterial,
            bool showCellGrid,
            Color gridLineColor,
            float gridLineWidth,
            float gridLineYOffset)
        {
            this.ownerTransform = ownerTransform ?? throw new ArgumentNullException(nameof(ownerTransform));
            this.boardLayerIndex = boardLayerIndex;
            this.cellPrefab = cellPrefab;
            this.defaultMaterial = defaultMaterial;
            this.showCellGrid = showCellGrid;
            this.gridLineColor = gridLineColor;
            this.gridLineWidth = gridLineWidth;
            this.gridLineYOffset = gridLineYOffset;
        }

        public void Rebuild(int width, int depth, float cellSize, float cellSpacing, float boardFootprintSize)
        {
            this.width = Mathf.Max(1, width);
            this.depth = Mathf.Max(1, depth);
            this.cellSize = cellSize;
            this.cellSpacing = cellSpacing;
            this.boardFootprintSize = boardFootprintSize;

            Debug.Log($"[RoundFlow] BoardFloorView.Rebuild: size=({this.width}x{this.depth}), hasExistingContainer={boardContainer != null}");
            DestroyBoardContainer();
            CreateBoardContainer();
            CreateFloorTiles();
            AddBoardCollider();
        }

        public FloorTileView GetTile(int x, int z)
        {
            if (floorTiles == null || x < 0 || x >= width || z < 0 || z >= depth)
            {
                return null;
            }

            return floorTiles[x, z];
        }

        public void ApplyVisualState(int x, int z, FloorTileVisualState state)
        {
            FloorTileView tile = GetTile(x, z);
            if (tile == null)
            {
                return;
            }

            tile.Apply(state);
        }

        public void RefreshVisualLayout()
        {
            if (floorTiles == null)
            {
                return;
            }

            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < depth; z++)
                {
                    FloorTileView tile = floorTiles[x, z];
                    if (tile == null)
                    {
                        continue;
                    }

                    EnsureTileVisualScale(tile.gameObject);
                    EnsureTileGridOverlay(tile.gameObject);
                }
            }

            AddBoardCollider();
        }

        public void Dispose()
        {
            Debug.Log($"[RoundFlow] BoardFloorView.Dispose: hasContainer={boardContainer != null}, containerId={boardContainer?.GetInstanceID() ?? 0}");
            NullifyGridOverlayMaterials();
            DestroyBoardContainer();
            if (gridLineMaterial != null)
            {
                UnityObjectUtility.SafeDestroy(gridLineMaterial);
                gridLineMaterial = null;
            }
        }

        private void NullifyGridOverlayMaterials()
        {
            if (floorTiles == null)
            {
                return;
            }

            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < depth; z++)
                {
                    FloorTileView tile = floorTiles[x, z];
                    if (tile == null)
                    {
                        continue;
                    }

                    Transform overlay = tile.transform.Find(CellGridOverlayName);
                    if (overlay == null)
                    {
                        continue;
                    }

                    LineRenderer lineRenderer = overlay.GetComponent<LineRenderer>();
                    if (lineRenderer != null)
                    {
                        lineRenderer.sharedMaterial = null;
                    }
                }
            }
        }

        private void CreateBoardContainer()
        {
            boardContainer = new GameObject("BoardContainer");
            boardContainer.transform.SetParent(ownerTransform, false);
            ApplyBoardLayer(boardContainer);
            Debug.Log($"[RoundFlow] BoardFloorView.CreateBoardContainer: id={boardContainer.GetInstanceID()}, parent={ownerTransform.name}(childCount={ownerTransform.childCount})");
        }

        private void DestroyBoardContainer()
        {
            if (boardContainer == null)
            {
                Debug.Log("[RoundFlow] BoardFloorView.DestroyBoardContainer: no container to destroy");
                return;
            }

            Debug.Log($"[RoundFlow] BoardFloorView.DestroyBoardContainer: id={boardContainer.GetInstanceID()}, active={boardContainer.activeSelf}, childCount={boardContainer.transform.childCount}");
            boardContainer.transform.SetParent(null, false);
            boardContainer.SetActive(false);
            UnityObjectUtility.SafeDestroy(boardContainer);
            boardContainer = null;
            floorTiles = null;
        }

        private void CreateFloorTiles()
        {
            floorTiles = new FloorTileView[width, depth];
            Debug.Log($"[RoundFlow] BoardFloorView.CreateFloorTiles: creating {width}x{depth}={width * depth} tiles in container id={boardContainer.GetInstanceID()}");
            float totalCellSize = cellSize + cellSpacing;
            Vector3 startPos = new Vector3(
                -(width - 1) * totalCellSize * 0.5f,
                0f,
                -(depth - 1) * totalCellSize * 0.5f
            );

            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < depth; z++)
                {
                    Vector3 position = startPos + new Vector3(x * totalCellSize, 0f, z * totalCellSize);
                    floorTiles[x, z] = CreateTile(x, z, position);
                }
            }
        }

        private FloorTileView CreateTile(int x, int z, Vector3 position)
        {
            GameObject tileObject = cellPrefab != null
                ? UnityEngine.Object.Instantiate(cellPrefab, boardContainer.transform)
                : CreateDefaultTile();

            tileObject.transform.localPosition = position;
            tileObject.name = $"Cell_{x}_0_{z}";
            ApplyBoardLayer(tileObject);
            EnsureTileVisualScale(tileObject);
            EnsureTileGridOverlay(tileObject);

            FloorTileView tile = tileObject.GetComponent<FloorTileView>();
            if (tile == null)
            {
                tile = tileObject.AddComponent<FloorTileView>();
            }

            tile.Initialize(x, z);
            return tile;
        }

        private GameObject CreateDefaultTile()
        {
            GameObject tile = new GameObject("CellRoot");
            ApplyBoardLayer(tile);

            BoxCollider collider = tile.AddComponent<BoxCollider>();
            collider.size = new Vector3(boardFootprintSize, 0.2f, boardFootprintSize);
            collider.center = new Vector3(0f, 0.1f, 0f);
            collider.isTrigger = true;

            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Quad);
            visual.name = CellVisualName;
            visual.transform.SetParent(tile.transform, false);
            visual.transform.localPosition = new Vector3(0f, 0.001f, 0f);
            visual.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            visual.transform.localScale = new Vector3(boardFootprintSize, boardFootprintSize, 1f);
            ApplyBoardLayer(visual);

            Collider visualCollider = visual.GetComponent<Collider>();
            if (visualCollider != null)
            {
                UnityObjectUtility.SafeDestroy(visualCollider);
            }

            Renderer renderer = visual.GetComponent<Renderer>();
            if (defaultMaterial != null)
            {
                renderer.sharedMaterial = defaultMaterial;
            }

            return tile;
        }

        private void EnsureTileVisualScale(GameObject tileObject)
        {
            if (tileObject == null)
            {
                return;
            }

            Transform visualTransform = tileObject.transform.Find(CellVisualName);
            if (visualTransform == null)
            {
                return;
            }

            Vector3 scale = visualTransform.localScale;
            if (Mathf.Abs(scale.z - 1f) <= 0.001f)
            {
                visualTransform.localScale = new Vector3(boardFootprintSize, boardFootprintSize, scale.z);
            }
            else
            {
                visualTransform.localScale = new Vector3(boardFootprintSize, scale.y, boardFootprintSize);
            }
        }

        private void EnsureTileGridOverlay(GameObject tileObject)
        {
            Transform overlayTransform = tileObject.transform.Find(CellGridOverlayName);

            if (!showCellGrid)
            {
                if (overlayTransform != null)
                {
                    UnityObjectUtility.SafeDestroy(overlayTransform.gameObject);
                }
                return;
            }

            GameObject overlayObject;
            if (overlayTransform == null)
            {
                overlayObject = new GameObject(CellGridOverlayName);
                overlayObject.transform.SetParent(tileObject.transform, false);
            }
            else
            {
                overlayObject = overlayTransform.gameObject;
            }

            overlayObject.transform.localPosition = new Vector3(0f, gridLineYOffset, 0f);
            overlayObject.transform.localRotation = Quaternion.identity;
            ApplyBoardLayer(overlayObject);

            LineRenderer lineRenderer = overlayObject.GetComponent<LineRenderer>();
            if (lineRenderer == null)
            {
                lineRenderer = overlayObject.AddComponent<LineRenderer>();
            }

            float halfSize = Mathf.Max(0.05f, boardFootprintSize * 0.5f);
            lineRenderer.useWorldSpace = false;
            lineRenderer.loop = true;
            lineRenderer.positionCount = 4;
            lineRenderer.SetPosition(0, new Vector3(-halfSize, 0f, -halfSize));
            lineRenderer.SetPosition(1, new Vector3(-halfSize, 0f, halfSize));
            lineRenderer.SetPosition(2, new Vector3(halfSize, 0f, halfSize));
            lineRenderer.SetPosition(3, new Vector3(halfSize, 0f, -halfSize));
            lineRenderer.widthMultiplier = gridLineWidth;
            lineRenderer.startColor = gridLineColor;
            lineRenderer.endColor = gridLineColor;
            lineRenderer.shadowCastingMode = ShadowCastingMode.Off;
            lineRenderer.receiveShadows = false;
            lineRenderer.sharedMaterial = GetGridLineMaterial();
        }

        private Material GetGridLineMaterial()
        {
            if (gridLineMaterial != null)
            {
                return gridLineMaterial;
            }

            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null)
            {
                shader = Shader.Find("Unlit/Color");
            }

            if (shader == null)
            {
                return null;
            }

            gridLineMaterial = new Material(shader)
            {
                name = "RuntimeBoardGridLineMaterial"
            };
            return gridLineMaterial;
        }

        private void AddBoardCollider()
        {
            if (boardContainer == null)
            {
                return;
            }

            BoxCollider boardCollider = boardContainer.GetComponent<BoxCollider>();
            if (boardCollider == null)
            {
                boardCollider = boardContainer.AddComponent<BoxCollider>();
            }

            float totalCellSize = cellSize + cellSpacing;
            boardCollider.size = new Vector3(width * totalCellSize, 0.5f, depth * totalCellSize);
            boardCollider.center = Vector3.zero;
        }

        private void ApplyBoardLayer(GameObject target)
        {
            if (target == null || boardLayerIndex < 0)
            {
                return;
            }

            target.layer = boardLayerIndex;
            foreach (Transform child in target.transform)
            {
                ApplyBoardLayer(child.gameObject);
            }
        }
    }
}
