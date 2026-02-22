using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.Rendering;
using Ubongo.Core;

namespace Ubongo
{
    public class GameBoard : MonoBehaviour
    {
        private const int UbongoHeight = 2;
        private const string BoardLayerName = "Board";
        private const string CellVisualName = "Visual";
        private const string CellGridOverlayName = "GridOverlay";

        [Header("Board Settings")]
        [SerializeField] private int width = 4;
        [SerializeField] private int depth = 2;
        [SerializeField] private float cellSize = 1f;
        [SerializeField] private float cellSpacing = 0.1f;

        [Header("Visual Settings")]
        [SerializeField] private GameObject cellPrefab;
        [SerializeField, Range(0.75f, 1f)] private float boardFootprintRatio = 0.92f;
        [SerializeField] private Material validPlacementMaterial;
        [SerializeField] private Material invalidPlacementMaterial;
        [SerializeField] private Material defaultMaterial;
        [SerializeField] private Material targetAreaMaterial;
        [SerializeField] private bool showCellGrid = true;
        [SerializeField] private Color gridLineColor = new Color(0.9f, 0.94f, 1f, 0.65f);
        [SerializeField, Min(0.002f)] private float gridLineWidth = 0.018f;
        [SerializeField, Min(0f)] private float gridLineYOffset = 0.02f;

        private BoardCell[,,] grid;
        private TargetArea targetArea;
        private bool[,,] occupancyGrid;
        private PuzzleValidator validator;
        private GameObject boardContainer;
        private int boardLayerIndex = -1;
        private Material gridLineMaterial;

        public int Width => width;
        public int Height => UbongoHeight;
        public int Depth => depth;
        public float CellSize => cellSize;
        public float GridStep => cellSize + cellSpacing;
        public float BoardFootprintRatio => boardFootprintRatio;
        public float BoardFootprintSize => cellSize * boardFootprintRatio;
        public TargetArea CurrentTargetArea => targetArea;

        public event Action<FillState> OnFillStateChanged;
        public event Action OnPuzzleSolved;

        private void Awake()
        {
            validator = new PuzzleValidator();
            boardLayerIndex = LayerMask.NameToLayer(BoardLayerName);
            if (boardLayerIndex < 0)
            {
                Debug.LogWarning($"[{nameof(GameBoard)}] Layer '{BoardLayerName}' not found. Using existing object layers.");
            }
            ApplyBoardLayer(gameObject);
        }

        private void Start()
        {
            InitializeBoard();
        }

        private void InitializeBoard()
        {
            CreateBoardContainer();
            CreateGrid();
            SetupDefaultTargetArea();
        }

        private void CreateBoardContainer()
        {
            if (boardContainer != null)
            {
                Destroy(boardContainer);
            }

            boardContainer = new GameObject("BoardContainer");
            boardContainer.transform.parent = transform;
            boardContainer.transform.localPosition = Vector3.zero;
            ApplyBoardLayer(boardContainer);
        }

        private void CreateGrid()
        {
            grid = new BoardCell[width, UbongoHeight, depth];
            occupancyGrid = new bool[width, UbongoHeight, depth];
            float totalCellSize = cellSize + cellSpacing;

            Vector3 startPos = new Vector3(
                -(width - 1) * totalCellSize * 0.5f,
                0,
                -(depth - 1) * totalCellSize * 0.5f
            );

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < UbongoHeight; y++)
                {
                    for (int z = 0; z < depth; z++)
                    {
                        Vector3 position = startPos + new Vector3(
                            x * totalCellSize,
                            0,
                            z * totalCellSize
                        );

                        CreateCell(x, y, z, position);

                        // 2층 셀은 시각적으로 숨김
                        if (y > 0 && grid[x, y, z] != null)
                        {
                            Renderer r = grid[x, y, z].VisualRenderer;
                            if (r != null) r.enabled = false;
                            LineRenderer line = grid[x, y, z].GetComponentInChildren<LineRenderer>();
                            if (line != null) line.enabled = false;
                            Collider c = grid[x, y, z].GetComponent<Collider>();
                            if (c != null) c.enabled = false;
                        }
                    }
                }
            }

            AddBoardCollider();
        }

        private void AddBoardCollider()
        {
            BoxCollider boardCollider = boardContainer.GetComponent<BoxCollider>();
            if (boardCollider == null)
            {
                boardCollider = boardContainer.AddComponent<BoxCollider>();
            }

            float totalCellSize = cellSize + cellSpacing;
            boardCollider.size = new Vector3(
                width * totalCellSize,
                0.4f,
                depth * totalCellSize
            );
            boardCollider.center = new Vector3(0, -0.2f, 0);
        }

        private void CreateCell(int x, int y, int z, Vector3 position)
        {
            GameObject cellObject = cellPrefab != null
                ? Instantiate(cellPrefab, boardContainer.transform)
                : CreateDefaultCell();

            cellObject.transform.localPosition = position;
            cellObject.name = $"Cell_{x}_{y}_{z}";
            ApplyBoardLayer(cellObject);
            EnsureCellVisualScale(cellObject);
            EnsureCellGridOverlay(cellObject);

            BoardCell cell = cellObject.GetComponent<BoardCell>();
            if (cell == null)
            {
                cell = cellObject.AddComponent<BoardCell>();
            }
            cell.Initialize(x, y, z, this);
            grid[x, y, z] = cell;
        }

        private GameObject CreateDefaultCell()
        {
            GameObject cell = new GameObject("CellRoot");
            ApplyBoardLayer(cell);
            float visualSize = BoardFootprintSize;

            // Root object keeps interaction logic only.
            BoxCollider collider = cell.AddComponent<BoxCollider>();
            collider.size = new Vector3(visualSize, 0.2f, visualSize);
            collider.center = new Vector3(0f, 0.1f, 0f);
            collider.isTrigger = true;

            // Visual object is intentionally separated from colliders.
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Quad);
            visual.name = CellVisualName;
            visual.transform.SetParent(cell.transform, false);
            visual.transform.localPosition = new Vector3(0f, 0.001f, 0f);
            visual.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            visual.transform.localScale = new Vector3(visualSize, visualSize, 1f);
            ApplyBoardLayer(visual);

            Collider visualCollider = visual.GetComponent<Collider>();
            if (visualCollider != null)
            {
                Destroy(visualCollider);
            }

            Renderer renderer = visual.GetComponent<Renderer>();
            if (defaultMaterial != null)
            {
                renderer.material = defaultMaterial;
            }
            else
            {
                renderer.material.color = new Color(0.8f, 0.8f, 0.8f, 0.3f);
            }

            return cell;
        }

        private void EnsureCellVisualScale(GameObject cellObject)
        {
            if (cellObject == null)
            {
                return;
            }

            Transform visualTransform = cellObject.transform.Find(CellVisualName);
            if (visualTransform == null)
            {
                return;
            }

            float visualSize = BoardFootprintSize;
            Vector3 scale = visualTransform.localScale;

            // Keep the existing axis that the source prefab uses for "thickness".
            // We only align the footprint axes to match piece block footprint.
            if (Mathf.Abs(scale.z - 1f) <= 0.001f)
            {
                visualTransform.localScale = new Vector3(visualSize, visualSize, scale.z);
            }
            else
            {
                visualTransform.localScale = new Vector3(visualSize, scale.y, visualSize);
            }
        }

        private void EnsureCellGridOverlay(GameObject cellObject)
        {
            Transform overlayTransform = cellObject.transform.Find(CellGridOverlayName);

            if (!showCellGrid)
            {
                if (overlayTransform != null)
                {
                    Destroy(overlayTransform.gameObject);
                }
                return;
            }

            GameObject overlayObject;
            if (overlayTransform == null)
            {
                overlayObject = new GameObject(CellGridOverlayName);
                overlayObject.transform.SetParent(cellObject.transform, false);
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

            float halfSize = Mathf.Max(0.05f, BoardFootprintSize * 0.5f);
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
            lineRenderer.material = GetGridLineMaterial();
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

        private void SetupDefaultTargetArea()
        {
            targetArea = new TargetArea(width, depth);
            UpdateTargetAreaVisuals();
        }

        /// <summary>
        /// Sets a custom target area for the puzzle.
        /// </summary>
        public void SetTargetArea(TargetArea area)
        {
            targetArea = area ?? new TargetArea(width, depth);
            UpdateTargetAreaVisuals();
        }

        /// <summary>
        /// Reinitializes the board with new dimensions.
        /// </summary>
        public void InitializeGrid(Vector3Int size)
        {
            width = size.x;
            depth = size.z;

            ClearBoard();
            CreateBoardContainer();
            CreateGrid();
            SetupDefaultTargetArea();
        }

        private void UpdateTargetAreaVisuals()
        {
            if (grid == null || targetArea == null)
            {
                return;
            }

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < UbongoHeight; y++)
                {
                    for (int z = 0; z < depth; z++)
                    {
                        BoardCell cell = grid[x, y, z];
                        if (cell != null)
                        {
                            bool isTarget = targetArea.Contains(x, z);
                            cell.SetAsTarget(isTarget);
                        }
                    }
                }
            }
        }

        public BoardCell GetCell(int x, int y, int z)
        {
            if (x < 0 || x >= width || y < 0 || y >= UbongoHeight || z < 0 || z >= depth)
            {
                return null;
            }

            return grid[x, y, z];
        }

        public bool IsOccupied(int x, int y, int z)
        {
            if (x < 0 || x >= width || y < 0 || y >= UbongoHeight || z < 0 || z >= depth)
            {
                return false;
            }

            return occupancyGrid[x, y, z];
        }

        public bool IsWithinBounds(Vector3Int position)
        {
            return position.x >= 0 && position.x < width &&
                   position.y >= 0 && position.y < UbongoHeight &&
                   position.z >= 0 && position.z < depth;
        }

        public Vector3Int WorldToGrid(Vector3 worldPosition)
        {
            Vector3 localPos = transform.InverseTransformPoint(worldPosition);
            float totalCellSize = cellSize + cellSpacing;

            Vector3 startPos = new Vector3(
                -(width - 1) * totalCellSize * 0.5f,
                0,
                -(depth - 1) * totalCellSize * 0.5f
            );

            Vector3 relativePos = localPos - startPos;

            int x = Mathf.RoundToInt(relativePos.x / totalCellSize);
            int y = 0;  // 아이소메트릭 뷰에서는 항상 layer 0 기준
            int z = Mathf.RoundToInt(relativePos.z / totalCellSize);

            return new Vector3Int(x, y, z);
        }

        public Vector3 GetBoardCenterWorld()
        {
            return transform.position;
        }

        public Bounds GetWorldBounds()
        {
            float totalCellSize = cellSize + cellSpacing;
            Vector3 size = new Vector3(
                width * totalCellSize,
                1f,
                depth * totalCellSize
            );

            return new Bounds(transform.TransformPoint(Vector3.zero), size);
        }

        public Vector3 GridToWorld(int x, int y, int z)
        {
            float totalCellSize = cellSize + cellSpacing;

            Vector3 startPos = new Vector3(
                -(width - 1) * totalCellSize * 0.5f,
                0,
                -(depth - 1) * totalCellSize * 0.5f
            );

            Vector3 localPos = startPos + new Vector3(
                x * totalCellSize,
                0,
                z * totalCellSize
            );

            return transform.TransformPoint(localPos);
        }

        /// <summary>
        /// Checks if a piece can be placed at the given grid position.
        /// </summary>
        public bool CanPlacePiece(PuzzlePiece piece, Vector3Int gridPosition)
        {
            List<Vector3Int> pieceBlocks = piece.GetBlockPositions();

            foreach (Vector3Int block in pieceBlocks)
            {
                Vector3Int checkPos = gridPosition + block;

                // Check grid bounds
                if (checkPos.x < 0 || checkPos.x >= width ||
                    checkPos.y < 0 || checkPos.y >= UbongoHeight ||
                    checkPos.z < 0 || checkPos.z >= depth)
                {
                    return false;
                }

                // Check target area
                if (targetArea != null && !targetArea.Contains(checkPos.x, checkPos.z))
                {
                    return false;
                }

                // Check occupation
                if (occupancyGrid[checkPos.x, checkPos.y, checkPos.z])
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Validates placement and returns the specific validity status.
        /// </summary>
        public PlacementValidity ValidatePlacement(PuzzlePiece piece, Vector3Int gridPosition)
        {
            List<Vector3Int> pieceBlocks = piece.GetBlockPositions();

            foreach (Vector3Int block in pieceBlocks)
            {
                Vector3Int checkPos = gridPosition + block;

                // Check bounds
                if (checkPos.x < 0 || checkPos.x >= width ||
                    checkPos.z < 0 || checkPos.z >= depth)
                {
                    return PlacementValidity.OutOfBounds;
                }

                // Check height constraint (Ubongo 3D: exactly 2 layers)
                if (checkPos.y < 0 || checkPos.y >= UbongoHeight)
                {
                    return PlacementValidity.HeightExceeded;
                }

                // Check target area
                if (targetArea != null && !targetArea.Contains(checkPos.x, checkPos.z))
                {
                    return PlacementValidity.OutsideTarget;
                }

                // Check occupation
                if (occupancyGrid[checkPos.x, checkPos.y, checkPos.z])
                {
                    return PlacementValidity.Collision;
                }
            }

            return PlacementValidity.Valid;
        }

        /// <summary>
        /// Places a piece on the board at the specified grid position.
        /// </summary>
        public void PlacePiece(PuzzlePiece piece, Vector3Int gridPosition)
        {
            List<Vector3Int> pieceBlocks = piece.GetBlockPositions();

            foreach (Vector3Int block in pieceBlocks)
            {
                Vector3Int cellPos = gridPosition + block;
                occupancyGrid[cellPos.x, cellPos.y, cellPos.z] = true;

                BoardCell cell = grid[cellPos.x, cellPos.y, cellPos.z];
                if (cell != null)
                {
                    cell.SetOccupied(true, piece);
                }
            }

            piece.SetPlaced(true);
            NotifyFillStateChanged();
            CheckWinCondition();
        }

        /// <summary>
        /// Removes a piece from the board.
        /// </summary>
        public void RemovePiece(PuzzlePiece piece)
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < UbongoHeight; y++)
                {
                    for (int z = 0; z < depth; z++)
                    {
                        BoardCell cell = grid[x, y, z];
                        if (cell != null && cell.OccupyingPiece == piece)
                        {
                            cell.SetOccupied(false, null);
                            occupancyGrid[x, y, z] = false;
                        }
                    }
                }
            }

            piece.SetPlaced(false);
            NotifyFillStateChanged();
        }

        /// <summary>
        /// Checks the win condition: all target area cells must be filled exactly 2 layers high.
        /// </summary>
        private void CheckWinCondition()
        {
            if (targetArea == null)
            {
                return;
            }

            ValidationResult result = validator.ValidateSolution(occupancyGrid, targetArea);

            if (result.IsSolved)
            {
                OnPuzzleSolved?.Invoke();
                GameManager.Instance.CompleteLevel();
            }
        }

        /// <summary>
        /// Gets the current fill state of the target area.
        /// </summary>
        public FillState GetFillState()
        {
            if (validator == null || targetArea == null)
            {
                return new FillState(0, 0, 0);
            }

            return validator.CalculateFillState(occupancyGrid, targetArea);
        }

        private void NotifyFillStateChanged()
        {
            FillState state = GetFillState();
            OnFillStateChanged?.Invoke(state);
        }

        /// <summary>
        /// Highlights cells where a piece would be placed.
        /// </summary>
        public void HighlightValidPlacement(Vector3Int gridPosition, PuzzlePiece piece)
        {
            ClearHighlights();
            if (piece == null)
            {
                return;
            }

            bool canPlace = CanPlacePiece(piece, gridPosition);
            List<Vector3Int> pieceBlocks = piece.GetBlockPositions();

            foreach (Vector2Int footprint in GetFootprintCells(pieceBlocks, gridPosition))
            {
                BoardCell cell = GetCell(footprint.x, 0, footprint.y);

                if (cell != null)
                {
                    cell.SetHighlight(true, canPlace);
                }
            }
        }

        /// <summary>
        /// Clears all cell highlights.
        /// </summary>
        public void ClearHighlights()
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < UbongoHeight; y++)
                {
                    for (int z = 0; z < depth; z++)
                    {
                        BoardCell cell = grid[x, y, z];
                        if (cell != null)
                        {
                            cell.SetHighlight(false, true);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Clears all pieces and resets the board.
        /// </summary>
        public void ClearBoard()
        {
            if (grid != null)
            {
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < UbongoHeight; y++)
                    {
                        for (int z = 0; z < depth; z++)
                        {
                            if (grid[x, y, z] != null)
                            {
                                grid[x, y, z].SetOccupied(false, null);
                            }
                            if (occupancyGrid != null)
                            {
                                occupancyGrid[x, y, z] = false;
                            }
                        }
                    }
                }
            }

            NotifyFillStateChanged();
        }

        /// <summary>
        /// Finds the lowest valid Y position for a piece at the given XZ coordinates.
        /// </summary>
        public Vector3Int FindLowestValidPosition(PuzzlePiece piece, int gridX, int gridZ)
        {
            for (int y = 0; y < UbongoHeight; y++)
            {
                Vector3Int testPos = new Vector3Int(gridX, y, gridZ);
                if (CanPlacePiece(piece, testPos))
                {
                    return testPos;
                }
            }

            return new Vector3Int(-1, -1, -1); // Invalid
        }

        public static HashSet<Vector2Int> GetFootprintCells(IEnumerable<Vector3Int> pieceBlocks, Vector3Int gridPosition)
        {
            HashSet<Vector2Int> footprint = new HashSet<Vector2Int>();
            if (pieceBlocks == null)
            {
                return footprint;
            }

            foreach (Vector3Int block in pieceBlocks)
            {
                Vector3Int cellPos = gridPosition + block;
                footprint.Add(new Vector2Int(cellPos.x, cellPos.z));
            }

            return footprint;
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

        private void OnValidate()
        {
            boardFootprintRatio = Mathf.Clamp(boardFootprintRatio, 0.75f, 1f);
            gridLineWidth = Mathf.Max(0.002f, gridLineWidth);
            gridLineYOffset = Mathf.Max(0f, gridLineYOffset);

            if (!Application.isPlaying || grid == null)
            {
                return;
            }

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < UbongoHeight; y++)
                {
                    for (int z = 0; z < depth; z++)
                    {
                        BoardCell cell = grid[x, y, z];
                        if (cell == null)
                        {
                            continue;
                        }

                        EnsureCellVisualScale(cell.gameObject);
                        EnsureCellGridOverlay(cell.gameObject);
                    }
                }
            }
        }

        private void OnDestroy()
        {
            if (gridLineMaterial != null)
            {
                Destroy(gridLineMaterial);
                gridLineMaterial = null;
            }
        }
    }
}
