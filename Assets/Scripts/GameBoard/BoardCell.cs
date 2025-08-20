using UnityEngine;

namespace Ubongo
{
    public class BoardCell : MonoBehaviour
    {
        private int x, y, z;
        private GameBoard board;
        private bool isOccupied = false;
        private bool isTarget = false;
        private PuzzlePiece occupyingPiece;
        private Renderer cellRenderer;
        private Material originalMaterial;
        
        [Header("Visual Feedback")]
        [SerializeField] private Color targetColor = new Color(0.2f, 0.8f, 0.2f, 0.5f);
        [SerializeField] private Color occupiedColor = new Color(0.5f, 0.5f, 0.5f, 0.8f);
        [SerializeField] private Color highlightValidColor = new Color(0.2f, 1f, 0.2f, 0.6f);
        [SerializeField] private Color highlightInvalidColor = new Color(1f, 0.2f, 0.2f, 0.6f);
        
        public int X => x;
        public int Y => y;
        public int Z => z;
        public bool IsOccupied => isOccupied;
        public bool IsTarget => isTarget;
        public PuzzlePiece OccupyingPiece => occupyingPiece;
        
        public void Initialize(int gridX, int gridY, int gridZ, GameBoard gameBoard)
        {
            x = gridX;
            y = gridY;
            z = gridZ;
            board = gameBoard;
            
            cellRenderer = GetComponent<Renderer>();
            if (cellRenderer != null)
            {
                originalMaterial = cellRenderer.material;
            }
        }
        
        public void SetAsTarget(bool target)
        {
            isTarget = target;
            UpdateVisual();
        }
        
        public void SetOccupied(bool occupied, PuzzlePiece piece)
        {
            isOccupied = occupied;
            occupyingPiece = piece;
            UpdateVisual();
        }
        
        public void SetHighlight(bool valid)
        {
            if (cellRenderer != null)
            {
                Color highlightColor = valid ? highlightValidColor : highlightInvalidColor;
                cellRenderer.material.color = highlightColor;
            }
        }
        
        private void UpdateVisual()
        {
            if (cellRenderer == null) return;
            
            if (isOccupied)
            {
                cellRenderer.material.color = occupiedColor;
                cellRenderer.enabled = false;
            }
            else if (isTarget)
            {
                cellRenderer.material.color = targetColor;
                cellRenderer.enabled = true;
            }
            else
            {
                cellRenderer.material.color = Color.white * 0.3f;
                cellRenderer.enabled = true;
            }
        }
        
        private void OnTriggerEnter(Collider other)
        {
            PuzzlePiece piece = other.GetComponentInParent<PuzzlePiece>();
            if (piece != null && piece.IsDragging)
            {
                Vector3Int gridPos = board.WorldToGrid(piece.transform.position);
                board.HighlightValidPlacement(gridPos, piece);
            }
        }
        
        private void OnTriggerExit(Collider other)
        {
            PuzzlePiece piece = other.GetComponentInParent<PuzzlePiece>();
            if (piece != null && piece.IsDragging)
            {
                board.ClearHighlights();
            }
        }
    }
}