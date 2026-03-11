using UnityEngine;

namespace Ubongo
{
    public class FloorTileView : MonoBehaviour
    {
        private const string VisualChildName = "Visual";
        private static readonly int BaseColorPropertyId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorPropertyId = Shader.PropertyToID("_Color");

        private int x;
        private int z;
        private Renderer cellRenderer;
        private MaterialPropertyBlock colorPropertyBlock;
        private int colorPropertyId = -1;
        
        [Header("Visual Feedback")]
        [SerializeField] private Color baseColor = new Color(0.34f, 0.4f, 0.5f, 0.5f);
        [SerializeField] private Color targetColor = new Color(0.45f, 0.54f, 0.66f, 0.65f);
        [SerializeField] private Color occupiedColor = new Color(0.5f, 0.5f, 0.5f, 0.8f);
        [SerializeField] private Color highlightValidColor = new Color(0.2f, 1f, 0.2f, 0.6f);
        [SerializeField] private Color highlightInvalidColor = new Color(1f, 0.2f, 0.2f, 0.6f);
        
        public int X => x;
        public int Z => z;
        public Renderer VisualRenderer => cellRenderer;

        public void Initialize(int gridX, int gridZ)
        {
            x = gridX;
            z = gridZ;
            cellRenderer = ResolveRenderer();
            Apply(new FloorTileVisualState(false, false, FloorTileHighlightMode.None));
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
        
        public void Apply(FloorTileVisualState state)
        {
            if (cellRenderer == null)
            {
                return;
            }

            if (state.HighlightMode != FloorTileHighlightMode.None)
            {
                ApplyColor(state.HighlightMode == FloorTileHighlightMode.Valid ? highlightValidColor : highlightInvalidColor);
                cellRenderer.enabled = true;
                return;
            }

            if (state.IsOccupied)
            {
                ApplyColor(occupiedColor);
                cellRenderer.enabled = false;
            }
            else if (state.IsTarget)
            {
                ApplyColor(targetColor);
                cellRenderer.enabled = true;
            }
            else
            {
                ApplyColor(baseColor);
                cellRenderer.enabled = true;
            }
        }

        private void ApplyColor(Color color)
        {
            if (cellRenderer == null)
            {
                return;
            }

            EnsureColorPropertyState();
            cellRenderer.GetPropertyBlock(colorPropertyBlock);
            colorPropertyBlock.SetColor(colorPropertyId, color);
            cellRenderer.SetPropertyBlock(colorPropertyBlock);
        }

        private void EnsureColorPropertyState()
        {
            if (colorPropertyBlock == null)
            {
                colorPropertyBlock = new MaterialPropertyBlock();
            }

            if (colorPropertyId != -1)
            {
                return;
            }

            Material material = cellRenderer != null ? cellRenderer.sharedMaterial : null;
            colorPropertyId = ResolveColorPropertyId(material);
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
                Debug.LogWarning($"[{nameof(FloorTileView)}] targetColor is too close to highlightValidColor (distance: {distance:0.00}).");
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
