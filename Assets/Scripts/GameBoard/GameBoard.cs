using UnityEngine;
using System.Collections.Generic;

namespace Ubongo
{
    public class GameBoard : MonoBehaviour
    {
        [Header("Board Settings")]
        [SerializeField] private int width = 4;
        [SerializeField] private int height = 3;
        [SerializeField] private int depth = 2;
        [SerializeField] private float cellSize = 1f;
        [SerializeField] private float cellSpacing = 0.1f;
        
        [Header("Visual Settings")]
        [SerializeField] private GameObject cellPrefab;
        [SerializeField] private Material validPlacementMaterial;
        [SerializeField] private Material invalidPlacementMaterial;
        [SerializeField] private Material defaultMaterial;
        
        private BoardCell[,,] grid;
        private List<BoardCell> targetCells = new List<BoardCell>();
        private GameObject boardContainer;
        
        public int Width => width;
        public int Height => height;
        public int Depth => depth;
        public float CellSize => cellSize;
        
        private void Start()
        {
            InitializeBoard();
        }
        
        private void InitializeBoard()
        {
            CreateBoardContainer();
            CreateGrid();
            SetupTargetArea();
        }
        
        private void CreateBoardContainer()
        {
            if (boardContainer != null)
                Destroy(boardContainer);
                
            boardContainer = new GameObject("BoardContainer");
            boardContainer.transform.parent = transform;
            boardContainer.transform.localPosition = Vector3.zero;
        }
        
        private void CreateGrid()
        {
            grid = new BoardCell[width, height, depth];
            float totalCellSize = cellSize + cellSpacing;
            
            Vector3 startPos = new Vector3(
                -(width - 1) * totalCellSize * 0.5f,
                0,
                -(depth - 1) * totalCellSize * 0.5f
            );
            
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
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
        }
        
        private void CreateCell(int x, int y, int z, Vector3 position)
        {
            GameObject cellObject = cellPrefab != null ? 
                Instantiate(cellPrefab, boardContainer.transform) : 
                CreateDefaultCell();
                
            cellObject.transform.localPosition = position;
            cellObject.name = $"Cell_{x}_{y}_{z}";
            
            BoardCell cell = cellObject.AddComponent<BoardCell>();
            cell.Initialize(x, y, z, this);
            grid[x, y, z] = cell;
        }
        
        private GameObject CreateDefaultCell()
        {
            GameObject cell = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cell.transform.localScale = Vector3.one * cellSize * 0.9f;
            
            Renderer renderer = cell.GetComponent<Renderer>();
            if (defaultMaterial != null)
                renderer.material = defaultMaterial;
            else
            {
                renderer.material.color = new Color(0.8f, 0.8f, 0.8f, 0.3f);
            }
            
            Collider collider = cell.GetComponent<Collider>();
            collider.isTrigger = true;
            
            return cell;
        }
        
        private void SetupTargetArea()
        {
            targetCells.Clear();
            
            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < depth; z++)
                {
                    BoardCell cell = grid[x, 0, z];
                    if (cell != null)
                    {
                        cell.SetAsTarget(true);
                        targetCells.Add(cell);
                    }
                }
            }
        }
        
        public BoardCell GetCell(int x, int y, int z)
        {
            if (x < 0 || x >= width || y < 0 || y >= height || z < 0 || z >= depth)
                return null;
                
            return grid[x, y, z];
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
        
        public bool CanPlacePiece(PuzzlePiece piece, Vector3Int gridPosition)
        {
            List<Vector3Int> pieceBlocks = piece.GetBlockPositions();
            
            foreach (Vector3Int block in pieceBlocks)
            {
                Vector3Int checkPos = gridPosition + block;
                
                if (checkPos.x < 0 || checkPos.x >= width ||
                    checkPos.y < 0 || checkPos.y >= height ||
                    checkPos.z < 0 || checkPos.z >= depth)
                {
                    return false;
                }
                
                BoardCell cell = grid[checkPos.x, checkPos.y, checkPos.z];
                if (cell == null || cell.IsOccupied)
                {
                    return false;
                }
            }
            
            return true;
        }
        
        public void PlacePiece(PuzzlePiece piece, Vector3Int gridPosition)
        {
            List<Vector3Int> pieceBlocks = piece.GetBlockPositions();
            
            foreach (Vector3Int block in pieceBlocks)
            {
                Vector3Int cellPos = gridPosition + block;
                BoardCell cell = grid[cellPos.x, cellPos.y, cellPos.z];
                if (cell != null)
                {
                    cell.SetOccupied(true, piece);
                }
            }
            
            piece.SetPlaced(true);
            CheckWinCondition();
        }
        
        public void RemovePiece(PuzzlePiece piece)
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    for (int z = 0; z < depth; z++)
                    {
                        BoardCell cell = grid[x, y, z];
                        if (cell != null && cell.OccupyingPiece == piece)
                        {
                            cell.SetOccupied(false, null);
                        }
                    }
                }
            }
            
            piece.SetPlaced(false);
        }
        
        private void CheckWinCondition()
        {
            bool allTargetsFilled = true;
            
            foreach (BoardCell targetCell in targetCells)
            {
                if (!targetCell.IsOccupied)
                {
                    allTargetsFilled = false;
                    break;
                }
            }
            
            if (allTargetsFilled)
            {
                Debug.Log("Puzzle Solved!");
                GameManager.Instance.CompleteLevel();
            }
        }
        
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
        
        public void ClearHighlights()
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
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
    }
}