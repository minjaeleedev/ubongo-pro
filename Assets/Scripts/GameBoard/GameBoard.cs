using UnityEngine;
using System;
using System.Collections.Generic;
using Ubongo.Core;

namespace Ubongo
{
    public class GameBoard : MonoBehaviour
    {
        private const int UbongoHeight = 2;

        [Header("Board Settings")]
        [SerializeField] private int width = 4;
        [SerializeField] private int depth = 2;
        [SerializeField] private float cellSize = 1f;
        [SerializeField] private float cellSpacing = 0.1f;

        [Header("Visual Settings")]
        [SerializeField] private GameObject cellPrefab;
        [SerializeField] private Material validPlacementMaterial;
        [SerializeField] private Material invalidPlacementMaterial;
        [SerializeField] private Material defaultMaterial;
        [SerializeField] private Material targetAreaMaterial;

        private BoardCell[,,] grid;
        private TargetArea targetArea;
        private bool[,,] occupancyGrid;
        private PuzzleValidator validator;
        private GameObject boardContainer;

        public int Width => width;
        public int Height => UbongoHeight;
        public int Depth => depth;
        public float CellSize => cellSize;
        public TargetArea CurrentTargetArea => targetArea;

        public event Action<FillState> OnFillStateChanged;
        public event Action OnPuzzleSolved;

        private void Awake()
        {
            validator = new PuzzleValidator();
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
                            y * totalCellSize,
                            z * totalCellSize
                        );

                        CreateCell(x, y, z, position);
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
                0.1f,
                depth * totalCellSize
            );
            boardCollider.center = new Vector3(0, -0.05f, 0);
        }

        private void CreateCell(int x, int y, int z, Vector3 position)
        {
            GameObject cellObject = cellPrefab != null
                ? Instantiate(cellPrefab, boardContainer.transform)
                : CreateDefaultCell();

            cellObject.transform.localPosition = position;
            cellObject.name = $"Cell_{x}_{y}_{z}";

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
            GameObject cell = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cell.transform.localScale = Vector3.one * cellSize * 0.9f;

            Renderer renderer = cell.GetComponent<Renderer>();
            if (defaultMaterial != null)
            {
                renderer.material = defaultMaterial;
            }
            else
            {
                renderer.material.color = new Color(0.8f, 0.8f, 0.8f, 0.3f);
            }

            Collider collider = cell.GetComponent<Collider>();
            collider.isTrigger = true;

            return cell;
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
            int y = Mathf.RoundToInt(relativePos.y / totalCellSize);
            int z = Mathf.RoundToInt(relativePos.z / totalCellSize);

            return new Vector3Int(x, y, z);
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
                y * totalCellSize,
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
            bool canPlace = CanPlacePiece(piece, gridPosition);
            List<Vector3Int> pieceBlocks = piece.GetBlockPositions();

            foreach (Vector3Int block in pieceBlocks)
            {
                Vector3Int cellPos = gridPosition + block;
                BoardCell cell = GetCell(cellPos.x, cellPos.y, cellPos.z);

                if (cell != null)
                {
                    cell.SetHighlight(canPlace);
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
                            cell.SetHighlight(false);
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
    }
}
