using UnityEngine;
using UnityEngine.InputSystem;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Ubongo
{
    public enum PlacementState
    {
        Default,
        Hovering,
        Selected,
        Dragging,
        ValidPlacement,
        InvalidPlacement,
        Placed
    }

    public class PuzzlePiece : MonoBehaviour
    {
        [Header("Piece Configuration")]
        [SerializeField] private List<Vector3Int> blockPositions = new List<Vector3Int>();
        [SerializeField] private Color pieceColor = Color.blue;

        [Header("Drag Settings")]
        [SerializeField] private float dragHeight = 2f;
        [SerializeField] private LayerMask boardLayer;

        [Header("Visual Feedback Colors")]
        [SerializeField] private Color hoverOutlineColor = new Color(1f, 1f, 1f, 0.8f);
        [SerializeField] private Color selectedOutlineColor = new Color(1f, 0.9f, 0.2f, 1f);
        [SerializeField] private Color validPlacementColor = new Color(0.2f, 1f, 0.2f, 0.8f);
        [SerializeField] private Color invalidPlacementColor = new Color(1f, 0.2f, 0.2f, 0.8f);
        [SerializeField] private Color placedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        [SerializeField] private Color layer2TintColor = new Color(0.7f, 0.7f, 0.9f, 1f);

        [Header("Animation Settings")]
        [SerializeField] private float pulseSpeed = 2f;
        [SerializeField] private float pulseIntensity = 0.15f;
        [SerializeField] private float hoverLiftHeight = 0.2f;
        [SerializeField] private float snapAnimationDuration = 0.2f;
        [SerializeField] private float shakeIntensity = 0.1f;
        [SerializeField] private float shakeDuration = 0.3f;

        [Header("Outline Settings")]
        [SerializeField] private float outlineWidth = 0.05f;
        [SerializeField] private Material outlineMaterial;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip pickupSound;
        [SerializeField] private AudioClip rotateSound;
        [SerializeField] private AudioClip validPlaceSound;
        [SerializeField] private AudioClip invalidPlaceSound;
        [SerializeField] private AudioClip releaseSound;

        [Header("Height Visualization")]
        [SerializeField] private GameObject heightIndicatorPrefab;
        [SerializeField] private Color layer1Color = new Color(0.3f, 0.8f, 0.3f, 0.5f);
        [SerializeField] private Color layer2Color = new Color(0.3f, 0.5f, 0.8f, 0.5f);

        private bool isDragging = false;
        private bool isPlaced = false;
        private bool isSelected = false;
        private bool isHovering = false;
        private PlacementState currentState = PlacementState.Default;
        private Vector3 originalPosition;
        private Quaternion originalRotation;
        private GameBoard gameBoard;
        private Camera mainCamera;
        private Vector3 dragOffset;
        private List<GameObject> blockObjects = new List<GameObject>();
        private List<GameObject> outlineObjects = new List<GameObject>();
        private List<GameObject> heightIndicators = new List<GameObject>();
        private Coroutine pulseCoroutine;
        private Coroutine shakeCoroutine;
        private Vector3 lastValidPosition;
        private bool subscribedToInput = false;

        public bool IsDragging => isDragging;
        public bool IsPlaced => isPlaced;
        public bool IsSelected => isSelected;
        public PlacementState CurrentState => currentState;
        public float DragHeight => dragHeight;

        public event Action<PuzzlePiece> OnPieceSelected;
        public event Action<PuzzlePiece> OnPieceDeselected;
        public event Action<PuzzlePiece, bool> OnPlacementAttempt;

        private void Start()
        {
            mainCamera = Camera.main;
            gameBoard = FindFirstObjectByType<GameBoard>();
            originalPosition = transform.position;
            originalRotation = transform.rotation;

            CreateBlockVisuals();
            SetState(PlacementState.Default);
            SubscribeToInputEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeFromInputEvents();
        }

        private void SubscribeToInputEvents()
        {
            if (InputManager.Instance == null) return;
            if (subscribedToInput) return;

            InputManager.Instance.OnPieceHoverEnter += HandleHoverEnter;
            InputManager.Instance.OnPieceHoverExit += HandleHoverExit;
            InputManager.Instance.OnPieceSelectStart += HandleSelectStart;
            InputManager.Instance.OnPieceDrag += HandleDrag;
            InputManager.Instance.OnPieceSelectEnd += HandleSelectEnd;
            InputManager.Instance.OnPieceRotate += HandleRotate;
            subscribedToInput = true;
        }

        private void UnsubscribeFromInputEvents()
        {
            if (InputManager.Instance == null) return;

            InputManager.Instance.OnPieceHoverEnter -= HandleHoverEnter;
            InputManager.Instance.OnPieceHoverExit -= HandleHoverExit;
            InputManager.Instance.OnPieceSelectStart -= HandleSelectStart;
            InputManager.Instance.OnPieceDrag -= HandleDrag;
            InputManager.Instance.OnPieceSelectEnd -= HandleSelectEnd;
            InputManager.Instance.OnPieceRotate -= HandleRotate;
        }

        private void HandleHoverEnter(PuzzlePiece piece)
        {
            if (piece != this) return;
            if (isDragging || isPlaced) return;

            isHovering = true;
            SetState(PlacementState.Hovering);
            StartCoroutine(HoverLift());
        }

        private void HandleHoverExit(PuzzlePiece piece)
        {
            if (piece != this) return;
            if (isDragging || isSelected) return;

            isHovering = false;
            SetState(PlacementState.Default);
        }

        private void HandleSelectStart(PuzzlePiece piece)
        {
            if (piece != this) return;

            if (isPlaced)
            {
                gameBoard.RemovePiece(this);
                PlaySound(releaseSound);
            }

            isDragging = true;
            isSelected = true;
            SetState(PlacementState.Dragging);

            PlaySound(pickupSound);
            OnPieceSelected?.Invoke(this);

            if (InputManager.Instance != null)
            {
                Ray ray = InputManager.Instance.GetPointerRay();
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    dragOffset = transform.position - hit.point;
                    dragOffset.y = 0;
                }
            }
        }

        private void HandleDrag(PuzzlePiece piece, Vector3 newPosition)
        {
            if (piece != this) return;
            if (!isDragging) return;

            transform.position = newPosition;
        }

        private void HandleSelectEnd(PuzzlePiece piece)
        {
            if (piece != this) return;

            isDragging = false;
            ClearHeightIndicators();

            if (InputManager.Instance != null && InputManager.Instance.TryGetBoardHit(out RaycastHit hit))
            {
                GameBoard board = hit.collider.GetComponentInParent<GameBoard>();
                if (board != null)
                {
                    Vector3Int gridPos = board.WorldToGrid(transform.position);

                    if (board.CanPlacePiece(this, gridPos))
                    {
                        StartCoroutine(SnapToPosition(board.GridToWorld(gridPos.x, gridPos.y, gridPos.z)));
                        board.PlacePiece(this, gridPos);
                        PlaySound(validPlaceSound);
                        OnPlacementAttempt?.Invoke(this, true);
                    }
                    else
                    {
                        StartCoroutine(ShakeAndReturn());
                        PlaySound(invalidPlaceSound);
                        OnPlacementAttempt?.Invoke(this, false);
                    }
                }
                else
                {
                    ReturnToOriginalPosition();
                }
            }
            else
            {
                ReturnToOriginalPosition();
            }

            gameBoard.ClearHighlights();
        }

        private void HandleRotate(Vector3 axis, float angle)
        {
            if (!isDragging) return;
            if (InputManager.Instance == null) return;
            if (InputManager.Instance.SelectedPiece != this) return;

            RotateWithAnimation(axis, angle);
        }

        private void Update()
        {
            if (!subscribedToInput && InputManager.Instance != null)
            {
                SubscribeToInputEvents();
            }

            if (isDragging)
            {
                UpdatePlacementPreview();
            }
        }

        private void CreateBlockVisuals()
        {
            ClearBlockObjects();

            if (blockPositions.Count == 0)
            {
                GenerateDefaultShape();
            }

            foreach (Vector3Int blockPos in blockPositions)
            {
                GameObject block = CreateBlock(blockPos);
                blockObjects.Add(block);

                GameObject outline = CreateOutline(block, blockPos);
                if (outline != null)
                {
                    outlineObjects.Add(outline);
                }
            }

            BoxCollider pieceCollider = GetComponent<BoxCollider>();
            if (pieceCollider == null)
            {
                pieceCollider = gameObject.AddComponent<BoxCollider>();
            }
            CalculateBounds(pieceCollider);
        }

        private void ClearBlockObjects()
        {
            foreach (GameObject block in blockObjects)
            {
                if (block != null)
                    Destroy(block);
            }
            blockObjects.Clear();

            foreach (GameObject outline in outlineObjects)
            {
                if (outline != null)
                    Destroy(outline);
            }
            outlineObjects.Clear();

            ClearHeightIndicators();
        }

        private void ClearHeightIndicators()
        {
            foreach (GameObject indicator in heightIndicators)
            {
                if (indicator != null)
                    Destroy(indicator);
            }
            heightIndicators.Clear();
        }

        private GameObject CreateBlock(Vector3Int blockPos)
        {
            GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.transform.parent = transform;
            block.transform.localPosition = blockPos;
            block.transform.localScale = Vector3.one * 0.95f;

            Renderer renderer = block.GetComponent<Renderer>();
            Color blockColor = GetColorForHeight(blockPos.y);
            renderer.material.color = blockColor;

            Collider collider = block.GetComponent<Collider>();
            collider.enabled = true;

            return block;
        }

        private Color GetColorForHeight(int height)
        {
            if (height > 0)
            {
                return pieceColor * layer2TintColor;
            }
            return pieceColor;
        }

        private GameObject CreateOutline(GameObject block, Vector3Int blockPos)
        {
            if (outlineMaterial == null) return null;

            GameObject outline = GameObject.CreatePrimitive(PrimitiveType.Cube);
            outline.name = "Outline";
            outline.transform.parent = block.transform;
            outline.transform.localPosition = Vector3.zero;
            outline.transform.localScale = Vector3.one * (1f + outlineWidth);

            Renderer outlineRenderer = outline.GetComponent<Renderer>();
            outlineRenderer.material = new Material(outlineMaterial);
            outlineRenderer.material.color = Color.clear;

            Collider outlineCollider = outline.GetComponent<Collider>();
            Destroy(outlineCollider);

            outline.SetActive(false);

            return outline;
        }

        private void GenerateDefaultShape()
        {
            int shapeType = UnityEngine.Random.Range(0, 5);
            blockPositions.Clear();

            switch (shapeType)
            {
                case 0:
                    blockPositions.Add(new Vector3Int(0, 0, 0));
                    blockPositions.Add(new Vector3Int(1, 0, 0));
                    blockPositions.Add(new Vector3Int(0, 0, 1));
                    break;

                case 1:
                    blockPositions.Add(new Vector3Int(0, 0, 0));
                    blockPositions.Add(new Vector3Int(1, 0, 0));
                    blockPositions.Add(new Vector3Int(2, 0, 0));
                    break;

                case 2:
                    blockPositions.Add(new Vector3Int(0, 0, 0));
                    blockPositions.Add(new Vector3Int(1, 0, 0));
                    blockPositions.Add(new Vector3Int(0, 1, 0));
                    blockPositions.Add(new Vector3Int(1, 1, 0));
                    break;

                case 3:
                    blockPositions.Add(new Vector3Int(0, 0, 0));
                    blockPositions.Add(new Vector3Int(1, 0, 0));
                    blockPositions.Add(new Vector3Int(1, 0, 1));
                    blockPositions.Add(new Vector3Int(2, 0, 1));
                    break;

                case 4:
                    blockPositions.Add(new Vector3Int(0, 0, 0));
                    blockPositions.Add(new Vector3Int(0, 1, 0));
                    blockPositions.Add(new Vector3Int(0, 0, 1));
                    break;
            }
        }

        private void CalculateBounds(BoxCollider collider)
        {
            if (blockPositions.Count == 0) return;

            Vector3 min = blockPositions[0];
            Vector3 max = blockPositions[0];

            foreach (Vector3Int pos in blockPositions)
            {
                min = Vector3.Min(min, pos);
                max = Vector3.Max(max, pos);
            }

            collider.center = (min + max) * 0.5f;
            collider.size = max - min + Vector3.one;
        }

        private void SetState(PlacementState newState)
        {
            currentState = newState;
            UpdateVisualFeedback();
        }

        private void UpdateVisualFeedback()
        {
            switch (currentState)
            {
                case PlacementState.Default:
                    SetBlocksColor(pieceColor, 1f);
                    SetOutlineVisibility(false);
                    StopPulseAnimation();
                    break;

                case PlacementState.Hovering:
                    SetBlocksColor(pieceColor, 1f);
                    SetOutlineColor(hoverOutlineColor);
                    SetOutlineVisibility(true);
                    break;

                case PlacementState.Selected:
                    SetBlocksColor(pieceColor, 1f);
                    SetOutlineColor(selectedOutlineColor);
                    SetOutlineVisibility(true);
                    StartPulseAnimation();
                    break;

                case PlacementState.Dragging:
                    SetBlocksColor(pieceColor, 0.8f);
                    SetOutlineColor(selectedOutlineColor);
                    SetOutlineVisibility(true);
                    ShowHeightIndicators();
                    break;

                case PlacementState.ValidPlacement:
                    SetBlocksColor(validPlacementColor, 0.9f);
                    SetOutlineColor(validPlacementColor);
                    SetOutlineVisibility(true);
                    break;

                case PlacementState.InvalidPlacement:
                    SetBlocksColor(invalidPlacementColor, 0.9f);
                    SetOutlineColor(invalidPlacementColor);
                    SetOutlineVisibility(true);
                    break;

                case PlacementState.Placed:
                    SetBlocksColor(pieceColor, 1f);
                    SetOutlineVisibility(false);
                    StopPulseAnimation();
                    ClearHeightIndicators();
                    break;
            }
        }

        private void SetBlocksColor(Color color, float alpha)
        {
            for (int i = 0; i < blockObjects.Count; i++)
            {
                if (blockObjects[i] == null) continue;

                Renderer renderer = blockObjects[i].GetComponent<Renderer>();
                if (renderer != null)
                {
                    Color finalColor = color;

                    if (i < blockPositions.Count && blockPositions[i].y > 0)
                    {
                        finalColor *= layer2TintColor;
                    }

                    finalColor.a = alpha;
                    renderer.material.color = finalColor;
                }
            }
        }

        private void SetOutlineColor(Color color)
        {
            foreach (GameObject outline in outlineObjects)
            {
                if (outline == null) continue;

                Renderer renderer = outline.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.color = color;
                }
            }
        }

        private void SetOutlineVisibility(bool visible)
        {
            foreach (GameObject outline in outlineObjects)
            {
                if (outline != null)
                {
                    outline.SetActive(visible);
                }
            }
        }

        private void StartPulseAnimation()
        {
            if (pulseCoroutine != null)
            {
                StopCoroutine(pulseCoroutine);
            }
            pulseCoroutine = StartCoroutine(PulseAnimation());
        }

        private void StopPulseAnimation()
        {
            if (pulseCoroutine != null)
            {
                StopCoroutine(pulseCoroutine);
                pulseCoroutine = null;
            }
        }

        private IEnumerator PulseAnimation()
        {
            while (true)
            {
                float pulse = Mathf.Sin(Time.time * pulseSpeed) * pulseIntensity;

                foreach (GameObject outline in outlineObjects)
                {
                    if (outline == null) continue;

                    Renderer renderer = outline.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        Color color = renderer.material.color;
                        color.a = 0.6f + pulse;
                        renderer.material.color = color;
                    }
                }

                yield return null;
            }
        }

        private void ShowHeightIndicators()
        {
            ClearHeightIndicators();

            foreach (Vector3Int blockPos in blockPositions)
            {
                if (blockPos.y > 0)
                {
                    CreateHeightIndicator(blockPos);
                }
            }
        }

        private void CreateHeightIndicator(Vector3Int blockPos)
        {
            GameObject indicator;

            if (heightIndicatorPrefab != null)
            {
                indicator = Instantiate(heightIndicatorPrefab, transform);
            }
            else
            {
                indicator = GameObject.CreatePrimitive(PrimitiveType.Cube);
                indicator.transform.parent = transform;
                indicator.transform.localScale = new Vector3(0.9f, 0.05f, 0.9f);

                Renderer renderer = indicator.GetComponent<Renderer>();
                renderer.material.color = blockPos.y == 1 ? layer2Color : layer1Color;

                Collider collider = indicator.GetComponent<Collider>();
                Destroy(collider);
            }

            indicator.transform.localPosition = new Vector3(blockPos.x, blockPos.y - 0.5f, blockPos.z);
            heightIndicators.Add(indicator);
        }

        private IEnumerator HoverLift()
        {
            Vector3 startPos = transform.position;
            Vector3 targetPos = startPos + Vector3.up * hoverLiftHeight;
            float elapsed = 0f;
            float duration = 0.15f;

            while (elapsed < duration && isHovering && !isDragging)
            {
                elapsed += Time.deltaTime;
                transform.position = Vector3.Lerp(startPos, targetPos, elapsed / duration);
                yield return null;
            }
        }

        private void RotateWithAnimation(Vector3 axis, float angle)
        {
            StartCoroutine(SmoothRotation(axis, angle));
            PlaySound(rotateSound);
        }

        private IEnumerator SmoothRotation(Vector3 axis, float angle)
        {
            Quaternion startRotation = transform.rotation;
            Quaternion targetRotation = transform.rotation * Quaternion.AngleAxis(angle, axis);

            float elapsed = 0f;
            float duration = 0.15f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float smoothT = t * t * (3f - 2f * t);
                transform.rotation = Quaternion.Slerp(startRotation, targetRotation, smoothT);
                yield return null;
            }

            transform.rotation = targetRotation;
            UpdatePlacementPreview();
        }

        private void UpdatePlacementPreview()
        {
            if (gameBoard == null) return;

            Vector3Int gridPos = gameBoard.WorldToGrid(transform.position);
            bool canPlace = gameBoard.CanPlacePiece(this, gridPos);

            if (canPlace)
            {
                if (currentState != PlacementState.ValidPlacement)
                {
                    SetState(PlacementState.ValidPlacement);
                }
                lastValidPosition = gameBoard.GridToWorld(gridPos.x, gridPos.y, gridPos.z);
            }
            else
            {
                if (currentState != PlacementState.InvalidPlacement && currentState != PlacementState.Dragging)
                {
                    SetState(PlacementState.InvalidPlacement);
                }
            }

            gameBoard.HighlightValidPlacement(gridPos, this);
        }

        private IEnumerator SnapToPosition(Vector3 targetPosition)
        {
            Vector3 startPosition = transform.position;
            float elapsed = 0f;

            while (elapsed < snapAnimationDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / snapAnimationDuration;
                float smoothT = 1f - Mathf.Pow(1f - t, 3f);
                transform.position = Vector3.Lerp(startPosition, targetPosition, smoothT);
                yield return null;
            }

            transform.position = targetPosition;
            SetState(PlacementState.Placed);
            isSelected = false;
            OnPieceDeselected?.Invoke(this);
        }

        private IEnumerator ShakeAndReturn()
        {
            if (shakeCoroutine != null)
            {
                StopCoroutine(shakeCoroutine);
            }

            Vector3 shakeStartPos = transform.position;
            float elapsed = 0f;

            while (elapsed < shakeDuration)
            {
                elapsed += Time.deltaTime;
                float shakeX = Mathf.Sin(elapsed * 50f) * shakeIntensity * (1f - elapsed / shakeDuration);
                transform.position = shakeStartPos + new Vector3(shakeX, 0f, 0f);
                yield return null;
            }

            ReturnToOriginalPosition();
        }

        private void ReturnToOriginalPosition()
        {
            StartCoroutine(AnimateReturnToOriginal());
        }

        private IEnumerator AnimateReturnToOriginal()
        {
            Vector3 startPosition = transform.position;
            Quaternion startRotation = transform.rotation;
            float elapsed = 0f;
            float duration = 0.3f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float smoothT = t * t * (3f - 2f * t);
                transform.position = Vector3.Lerp(startPosition, originalPosition, smoothT);
                transform.rotation = Quaternion.Slerp(startRotation, originalRotation, smoothT);
                yield return null;
            }

            transform.position = originalPosition;
            transform.rotation = originalRotation;
            isPlaced = false;
            isSelected = false;
            SetState(PlacementState.Default);
            OnPieceDeselected?.Invoke(this);
        }

        private void PlaySound(AudioClip clip)
        {
            if (audioSource != null && clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }

        public List<Vector3Int> GetBlockPositions()
        {
            List<Vector3Int> rotatedPositions = new List<Vector3Int>();

            foreach (Vector3Int originalPos in blockPositions)
            {
                Vector3 rotated = transform.rotation * originalPos;
                Vector3Int roundedPos = new Vector3Int(
                    Mathf.RoundToInt(rotated.x),
                    Mathf.RoundToInt(rotated.y),
                    Mathf.RoundToInt(rotated.z)
                );
                rotatedPositions.Add(roundedPos);
            }

            return rotatedPositions;
        }

        public void SetPlaced(bool placed)
        {
            isPlaced = placed;

            if (placed)
            {
                SetState(PlacementState.Placed);
            }
            else
            {
                SetState(PlacementState.Default);
            }
        }

        public void SetPieceColor(Color color)
        {
            pieceColor = color;
            UpdateVisualFeedback();
        }

        public void SetBlockPositions(List<Vector3Int> positions)
        {
            blockPositions = new List<Vector3Int>(positions);
            CreateBlockVisuals();
        }

        public int GetMaxHeight()
        {
            int maxHeight = 0;
            foreach (Vector3Int pos in blockPositions)
            {
                if (pos.y > maxHeight)
                {
                    maxHeight = pos.y;
                }
            }
            return maxHeight;
        }

        public bool HasMultipleLayers()
        {
            return GetMaxHeight() > 0;
        }

        public void HighlightLayer(int layer)
        {
            for (int i = 0; i < blockObjects.Count; i++)
            {
                if (i >= blockPositions.Count) continue;

                Renderer renderer = blockObjects[i].GetComponent<Renderer>();
                if (renderer == null) continue;

                if (blockPositions[i].y == layer)
                {
                    Color highlightColor = renderer.material.color;
                    highlightColor.a = 1f;
                    renderer.material.color = highlightColor;
                }
                else
                {
                    Color dimColor = renderer.material.color;
                    dimColor.a = 0.4f;
                    renderer.material.color = dimColor;
                }
            }
        }

        public void ResetLayerHighlight()
        {
            UpdateVisualFeedback();
        }
    }
}
