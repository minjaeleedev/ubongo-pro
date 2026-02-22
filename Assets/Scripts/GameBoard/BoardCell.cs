using UnityEngine;

namespace Ubongo
{
    public class BoardCell : MonoBehaviour
    {
        private const string VisualChildName = "Visual";

        private int x, y, z;
        private bool isOccupied = false;
        private bool isTarget = false;
        private bool isHighlighted = false;
        private bool isHighlightValid = true;
        private PuzzlePiece occupyingPiece;
        private Renderer cellRenderer;
        
        [Header("Visual Feedback")]
        [SerializeField] private Color baseColor = new Color(0.34f, 0.4f, 0.5f, 0.5f);
        [SerializeField] private Color targetColor = new Color(0.45f, 0.54f, 0.66f, 0.65f);
        [SerializeField] private Color occupiedColor = new Color(0.5f, 0.5f, 0.5f, 0.8f);
        [SerializeField] private Color highlightValidColor = new Color(0.2f, 1f, 0.2f, 0.6f);
        [SerializeField] private Color highlightInvalidColor = new Color(1f, 0.2f, 0.2f, 0.6f);
        
        public int X => x;
        public int Y => y;
        public int Z => z;
        public bool IsOccupied => isOccupied;
        public bool IsTarget => isTarget;
        public PuzzlePiece OccupyingPiece => occupyingPiece;
        public Renderer VisualRenderer => cellRenderer;
        
        public void Initialize(int gridX, int gridY, int gridZ, GameBoard _)
        {
            x = gridX;
            y = gridY;
            z = gridZ;
            
            cellRenderer = ResolveRenderer();
            UpdateVisual();
        }

        private Renderer ResolveRenderer()
        {
            Transform visualTransform = transform.Find(VisualChildName);
            if (visualTransform != null)
            {
                Renderer visualChildRenderer = visualTransform.GetComponent<Renderer>();
                if (visualChildRenderer != null)
                {
                    return visualChildRenderer;
                }
            }

            Renderer rootRenderer = GetComponent<Renderer>();
            if (rootRenderer != null)
            {
                return rootRenderer;
            }

            return GetComponentInChildren<Renderer>();
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
            if (occupied)
            {
                isHighlighted = false;
            }
            UpdateVisual();
        }
        
        public void SetHighlight(bool highlighted, bool valid)
        {
            isHighlighted = highlighted;
            isHighlightValid = valid;
            UpdateVisual();
        }
        
        private void UpdateVisual()
        {
            if (cellRenderer == null) return;
            if (y > 0)
            {
                cellRenderer.enabled = false;
                return;
            }

            if (isHighlighted)
            {
                cellRenderer.material.color = isHighlightValid ? highlightValidColor : highlightInvalidColor;
                cellRenderer.enabled = true;
                return;
            }

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
                cellRenderer.material.color = baseColor;
                cellRenderer.enabled = true;
            }
        }
        
        private void OnTriggerEnter(Collider _)
        {
            // Piece preview/highlight is updated centrally from PuzzlePiece.UpdatePlacementPreview.
            // Trigger-based updates can race and show stale green highlights.
        }
        
        private void OnTriggerExit(Collider _)
        {
            // Intentionally no-op. PuzzlePiece handles highlight clearing on release.
        }

        private void OnValidate()
        {
            float distance = ColorDistance(targetColor, highlightValidColor);
            if (distance < 0.35f)
            {
                Debug.LogWarning($"[{nameof(BoardCell)}] targetColor is too close to highlightValidColor (distance: {distance:0.00}).");
            }
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
