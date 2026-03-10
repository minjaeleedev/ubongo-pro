using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Ubongo.Core;

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
        private const string PieceLayerName = "Piece";
        private static readonly int BaseColorPropertyId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorPropertyId = Shader.PropertyToID("_Color");

        [Header("Piece Configuration")]
        [SerializeField] private List<Vector3Int> blockPositions = new List<Vector3Int>();
        [SerializeField] private Color pieceColor = Color.blue;

        [Header("Drag Settings")]
        [SerializeField] private float dragHeight = 0.5f;
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

        [Header("Block Visual Profile")]
        [SerializeField] private float blockVisualHeight = 0.8f;
        [SerializeField] private float layerStepY = 0.8f;

        private bool isDragging = false;
        private bool isPlaced = false;
        private bool isSelected = false;
        private bool isHovering = false;
        private PlacementState currentState = PlacementState.Default;
        private Vector3 originalPosition;
        private GameBoard gameBoard;
        private InputManager inputManager;
        private List<GameObject> blockObjects = new List<GameObject>();
        private List<GameObject> outlineObjects = new List<GameObject>();
        private List<GameObject> heightIndicators = new List<GameObject>();
        private Coroutine pulseCoroutine;
        private Coroutine shakeCoroutine;
        private GameBoard previewBoard;
        private Vector3Int previewGridPosition;
        private bool previewCanPlace;
        private bool hasPlacementPreview;
        private bool subscribedToInput = false;
        private int pieceLayerIndex = -1;
        private float blockGridStep = 1f;
        private Vector3 dragReturnPosition;
        private bool hasDragReturnPosition;
        private Vector3Int previewAnchorOffset;
        private Coroutine hoverLiftCoroutine;
        private Vector3 hoverStartPosition;
        private bool hasHoverStartPosition;
        private readonly Dictionary<int, Color> rendererColorCache = new Dictionary<int, Color>();
        private MaterialPropertyBlock colorPropertyBlock;
        private Color currentOutlineColor = Color.clear;

        public bool IsDragging => isDragging;
        public bool IsPlaced => isPlaced;
        public bool IsSelected => isSelected;
        public PlacementState CurrentState => currentState;
        public float DragHeight => dragHeight;

        public event Action<PuzzlePiece> OnPieceSelected;
        public event Action<PuzzlePiece> OnPieceDeselected;
        public event Action<PuzzlePiece, bool> OnPlacementAttempt;

        private void Awake()
        {
            pieceLayerIndex = LayerMask.NameToLayer(PieceLayerName);
            if (pieceLayerIndex < 0)
            {
                Debug.LogWarning($"[{nameof(PuzzlePiece)}] Layer '{PieceLayerName}' not found. Piece keeps default layer.");
            }
            ApplyPieceLayer(gameObject);
        }

        private void Start()
        {
            gameBoard = FindFirstObjectByType<GameBoard>();
            originalPosition = transform.position;
            blockGridStep = ResolveGridStep();

            CreateBlockVisuals();
            SetState(PlacementState.Default);
            SubscribeToInputEvents();
        }

        private void OnDestroy()
        {
            if (hoverLiftCoroutine != null)
            {
                StopCoroutine(hoverLiftCoroutine);
                hoverLiftCoroutine = null;
            }

            UnsubscribeFromInputEvents();
        }

        private void SubscribeToInputEvents()
        {
            if (!ResolveInputManager()) return;
            if (subscribedToInput) return;

            inputManager.OnPieceHoverEnter += HandleHoverEnter;
            inputManager.OnPieceHoverExit += HandleHoverExit;
            inputManager.OnPieceSelectStart += HandleSelectStart;
            inputManager.OnPieceDrag += HandleDrag;
            inputManager.OnPieceSelectEnd += HandleSelectEnd;
            inputManager.OnPieceRotate += HandleRotate;
            subscribedToInput = true;
        }

        private void UnsubscribeFromInputEvents()
        {
            if (inputManager == null) return;

            inputManager.OnPieceHoverEnter -= HandleHoverEnter;
            inputManager.OnPieceHoverExit -= HandleHoverExit;
            inputManager.OnPieceSelectStart -= HandleSelectStart;
            inputManager.OnPieceDrag -= HandleDrag;
            inputManager.OnPieceSelectEnd -= HandleSelectEnd;
            inputManager.OnPieceRotate -= HandleRotate;
            subscribedToInput = false;
            inputManager = null;
        }

        private void HandleHoverEnter(PuzzlePiece piece)
        {
            if (piece != this) return;
            if (isDragging || isPlaced) return;

            if (hoverLiftCoroutine != null)
            {
                StopCoroutine(hoverLiftCoroutine);
                hoverLiftCoroutine = null;
            }

            hoverStartPosition = transform.position;
            hasHoverStartPosition = true;
            isHovering = true;
            SetState(PlacementState.Hovering);
            hoverLiftCoroutine = StartCoroutine(HoverLift());
        }

        private void HandleHoverExit(PuzzlePiece piece)
        {
            if (piece != this) return;
            if (isDragging || isSelected) return;

            if (hoverLiftCoroutine != null)
            {
                StopCoroutine(hoverLiftCoroutine);
                hoverLiftCoroutine = null;
            }

            isHovering = false;
            if (hasHoverStartPosition)
            {
                transform.position = hoverStartPosition;
            }
            hasHoverStartPosition = false;
            SetState(PlacementState.Default);
        }

        private void HandleSelectStart(PuzzlePiece piece)
        {
            if (piece != this) return;

            if (hoverLiftCoroutine != null)
            {
                StopCoroutine(hoverLiftCoroutine);
                hoverLiftCoroutine = null;
            }

            if (isHovering && hasHoverStartPosition)
            {
                transform.position = hoverStartPosition;
            }
            isHovering = false;
            hasHoverStartPosition = false;

            dragReturnPosition = transform.position;
            hasDragReturnPosition = true;

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
        }

        private void HandleDrag(PuzzlePiece piece, Vector3 newPosition)
        {
            if (piece != this) return;
            if (!isDragging) return;

            Vector3 adjustedPosition = newPosition;
            adjustedPosition.y += GetPlacementLiftFromOffset(GetPlacementAnchorOffset());
            transform.position = adjustedPosition;
        }

        private void HandleSelectEnd(PuzzlePiece piece)
        {
            if (piece != this) return;

            isDragging = false;
            ClearHeightIndicators();
            RefreshPlacementPreviewCache();

            if (hasPlacementPreview && previewBoard != null && previewCanPlace)
            {
                Vector3 targetPosition = GetSnappedRootWorldPosition(previewBoard, previewGridPosition, previewAnchorOffset);
                StartCoroutine(SnapToPosition(targetPosition));
                previewBoard.PlacePiece(this, previewGridPosition);
                PlaySound(validPlaceSound);
                OnPlacementAttempt?.Invoke(this, true);
            }
            else
            {
                StartCoroutine(ShakeAndReturn());
                PlaySound(invalidPlaceSound);
                OnPlacementAttempt?.Invoke(this, false);
            }

            ClearBoardHighlights();
        }

        private void HandleRotate(Vector3 axis, float angle)
        {
            if (!isDragging) return;
            if (!ResolveInputManager()) return;
            if (inputManager.SelectedPiece != this) return;

            RotateWithAnimation(axis, angle);
        }

        private void Update()
        {
            if (subscribedToInput && inputManager == null)
            {
                subscribedToInput = false;
            }

            if (!subscribedToInput && ResolveInputManager())
            {
                SubscribeToInputEvents();
            }

            if (gameBoard == null)
            {
                gameBoard = FindFirstObjectByType<GameBoard>();
                if (gameBoard != null)
                {
                    SyncGridStepWithBoard();
                }
            }

            if (isDragging)
            {
                UpdatePlacementPreview();
            }
        }

        private bool ResolveInputManager()
        {
            if (inputManager != null)
            {
                return true;
            }

            if (InputManager.TryGetExistingInstance(out inputManager))
            {
                return true;
            }

            return false;
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
                {
                    RemoveCachedRendererColor(block);
                    UnityObjectUtility.SafeDestroy(block);
                }
            }
            blockObjects.Clear();

            foreach (GameObject outline in outlineObjects)
            {
                if (outline != null)
                {
                    RemoveCachedRendererColor(outline);
                    UnityObjectUtility.SafeDestroy(outline);
                }
            }
            outlineObjects.Clear();

            ClearHeightIndicators();
        }

        private void ClearHeightIndicators()
        {
            foreach (GameObject indicator in heightIndicators)
            {
                if (indicator != null)
                {
                    RemoveCachedRendererColor(indicator);
                    UnityObjectUtility.SafeDestroy(indicator);
                }
            }
            heightIndicators.Clear();
        }

        private GameObject CreateBlock(Vector3Int blockPos)
        {
            GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.transform.parent = transform;
            block.transform.localPosition = GetBlockLocalPosition(blockPos);
            float blockSize = ResolveBlockVisualSize();
            block.transform.localScale = new Vector3(blockSize, blockVisualHeight, blockSize);
            ApplyPieceLayer(block);

            Renderer renderer = block.GetComponent<Renderer>();
            Color blockColor = GetColorForHeight(blockPos.y);
            ApplyRendererColor(renderer, blockColor);

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
            ApplyPieceLayer(outline);

            Renderer outlineRenderer = outline.GetComponent<Renderer>();
            outlineRenderer.sharedMaterial = outlineMaterial;
            ApplyRendererColor(outlineRenderer, Color.clear);

            Collider outlineCollider = outline.GetComponent<Collider>();
            UnityObjectUtility.SafeDestroy(outlineCollider);

            outline.SetActive(false);

            return outline;
        }

        private void GenerateDefaultShape()
        {
            PieceDefinition[] catalog = PieceCatalog.GetAllPieces();
            if (catalog == null || catalog.Length == 0)
            {
                blockPositions.Clear();
                blockPositions.Add(Vector3Int.zero);
                Debug.LogWarning($"[{nameof(PuzzlePiece)}] PieceCatalog is empty. Falling back to a single-block default.");
                return;
            }

            PieceDefinition fallbackDefinition = catalog[UnityEngine.Random.Range(0, catalog.Length)];
            blockPositions.Clear();

            Vector3Int[] fallbackBlocks = fallbackDefinition.Blocks;
            if (fallbackBlocks == null || fallbackBlocks.Length == 0)
            {
                blockPositions.Add(Vector3Int.zero);
                return;
            }

            for (int i = 0; i < fallbackBlocks.Length; i++)
            {
                blockPositions.Add(fallbackBlocks[i]);
            }
        }

        private void CalculateBounds(BoxCollider collider)
        {
            if (blockPositions.Count == 0) return;

            float blockHalfSize = ResolveBlockVisualSize() * 0.5f;
            Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);

            foreach (Vector3Int pos in blockPositions)
            {
                Vector3 center = GetBlockLocalPosition(pos);
                min = Vector3.Min(min, center - new Vector3(blockHalfSize, blockVisualHeight * 0.5f, blockHalfSize));
                max = Vector3.Max(max, center + new Vector3(blockHalfSize, blockVisualHeight * 0.5f, blockHalfSize));

                // Keep collider sufficiently thick to remain easy to pick.
                max.y = Mathf.Max(max.y, center.y + Mathf.Max(0.2f, blockVisualHeight * 0.5f));
            }

            collider.center = (min + max) * 0.5f;
            collider.size = new Vector3(
                Mathf.Max(0.2f, max.x - min.x),
                Mathf.Max(0.2f, max.y - min.y),
                Mathf.Max(0.2f, max.z - min.z)
            );
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
                    ApplyRendererColor(renderer, finalColor);
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
                    ApplyRendererColor(renderer, color);
                }
            }

            currentOutlineColor = color;
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
                        Color color = currentOutlineColor;
                        color.a = 0.6f + pulse;
                        ApplyRendererColor(renderer, color);
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
                float blockSize = ResolveBlockVisualSize();
                indicator.transform.localScale = new Vector3(blockSize * 0.95f, 0.05f, blockSize * 0.95f);

                Renderer renderer = indicator.GetComponent<Renderer>();
                ApplyRendererColor(renderer, blockPos.y == 1 ? layer2Color : layer1Color);

                Collider collider = indicator.GetComponent<Collider>();
                UnityObjectUtility.SafeDestroy(collider);
            }

            Vector3 blockLocalPosition = GetBlockLocalPosition(blockPos);
            indicator.transform.localPosition = new Vector3(
                blockLocalPosition.x,
                blockLocalPosition.y - (blockVisualHeight * 0.55f),
                blockLocalPosition.z
            );
            ApplyPieceLayer(indicator);
            heightIndicators.Add(indicator);
        }

        private IEnumerator HoverLift()
        {
            if (!hasHoverStartPosition)
            {
                hoverLiftCoroutine = null;
                yield break;
            }

            Vector3 startPosition = hoverStartPosition;
            Vector3 targetPosition = startPosition + (Vector3.up * hoverLiftHeight);
            float elapsed = 0f;
            float duration = 0.15f;

            while (elapsed < duration)
            {
                if (!isHovering || isDragging)
                {
                    hoverLiftCoroutine = null;
                    yield break;
                }

                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                transform.position = Vector3.Lerp(startPosition, targetPosition, t);
                yield return null;
            }

            transform.position = targetPosition;
            hoverLiftCoroutine = null;
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
            RefreshPlacementPreviewCache();
            if (!hasPlacementPreview || previewBoard == null)
            {
                return;
            }

            if (previewCanPlace)
            {
                if (currentState != PlacementState.ValidPlacement)
                {
                    SetState(PlacementState.ValidPlacement);
                }
            }
            else
            {
                if (currentState != PlacementState.InvalidPlacement)
                {
                    SetState(PlacementState.InvalidPlacement);
                }
            }

            previewBoard.HighlightValidPlacement(previewGridPosition, this);
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

            ReturnToDragStartPosition();
        }

        private void ReturnToDragStartPosition()
        {
            StartCoroutine(AnimateReturnToDragStart());
        }

        private IEnumerator AnimateReturnToDragStart()
        {
            Vector3 startPosition = transform.position;
            Quaternion lockedRotation = transform.rotation;
            Vector3 returnPosition = hasDragReturnPosition ? dragReturnPosition : originalPosition;
            float elapsed = 0f;
            float duration = 0.3f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float smoothT = t * t * (3f - 2f * t);
                transform.position = Vector3.Lerp(startPosition, returnPosition, smoothT);
                transform.rotation = lockedRotation;
                yield return null;
            }

            transform.position = returnPosition;
            transform.rotation = lockedRotation;
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
            List<Vector3Int> rawRotated = GetRawRotatedBlockPositions();
            Vector3Int minOffset = GetMinOffset(rawRotated);
            List<Vector3Int> normalized = new List<Vector3Int>(rawRotated.Count);

            foreach (Vector3Int raw in rawRotated)
            {
                normalized.Add(raw - minOffset);
            }

            return normalized;
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

        private float GetBaseLayerCenterY()
        {
            return blockVisualHeight * 0.5f;
        }

        private Vector3 GetBlockLocalPosition(Vector3Int blockPos)
        {
            return new Vector3(
                blockPos.x * blockGridStep,
                GetBaseLayerCenterY() + (blockPos.y * layerStepY),
                blockPos.z * blockGridStep
            );
        }

        private void RefreshPlacementPreviewCache()
        {
            if (gameBoard == null)
            {
                gameBoard = FindFirstObjectByType<GameBoard>();
            }

            if (gameBoard == null)
            {
                hasPlacementPreview = false;
                previewBoard = null;
                previewCanPlace = false;
                return;
            }

            SyncGridStepWithBoard();

            Vector3Int rootGridPos = gameBoard.WorldToGrid(transform.position);
            previewAnchorOffset = GetPlacementAnchorOffset();
            previewBoard = gameBoard;
            previewGridPosition = new Vector3Int(
                rootGridPos.x + previewAnchorOffset.x,
                0,
                rootGridPos.z + previewAnchorOffset.z
            );
            previewCanPlace = gameBoard.CanPlacePiece(this, previewGridPosition);
            hasPlacementPreview = true;
        }

        private float ResolveGridStep()
        {
            if (gameBoard == null)
            {
                return 1f;
            }

            return Mathf.Max(0.01f, gameBoard.GridStep);
        }

        private float ResolveBlockVisualSize()
        {
            if (gameBoard == null)
            {
                return 0.92f;
            }

            return Mathf.Max(0.1f, gameBoard.CellSize * gameBoard.BoardFootprintRatio);
        }

        private void SyncGridStepWithBoard()
        {
            float resolvedStep = ResolveGridStep();
            if (Mathf.Abs(blockGridStep - resolvedStep) <= 0.001f)
            {
                return;
            }

            blockGridStep = resolvedStep;
            CreateBlockVisuals();
        }

        private List<Vector3Int> GetRawRotatedBlockPositions()
        {
            List<Vector3Int> rotatedPositions = new List<Vector3Int>(blockPositions.Count);

            foreach (Vector3Int originalPos in blockPositions)
            {
                Vector3 rotated = transform.rotation * originalPos;
                rotatedPositions.Add(new Vector3Int(
                    Mathf.RoundToInt(rotated.x),
                    Mathf.RoundToInt(rotated.y),
                    Mathf.RoundToInt(rotated.z)
                ));
            }

            return rotatedPositions;
        }

        private static Vector3Int GetMinOffset(IReadOnlyList<Vector3Int> positions)
        {
            if (positions == null || positions.Count == 0)
            {
                return Vector3Int.zero;
            }

            Vector3Int min = positions[0];
            for (int i = 1; i < positions.Count; i++)
            {
                Vector3Int candidate = positions[i];
                if (candidate.x < min.x) min.x = candidate.x;
                if (candidate.y < min.y) min.y = candidate.y;
                if (candidate.z < min.z) min.z = candidate.z;
            }

            return min;
        }

        private Vector3Int GetPlacementAnchorOffset()
        {
            return GetMinOffset(GetRawRotatedBlockPositions());
        }

        private float GetPlacementLiftFromOffset(Vector3Int anchorOffset)
        {
            return Mathf.Max(0f, -anchorOffset.y) * layerStepY;
        }

        private Vector3 GetSnappedRootWorldPosition(GameBoard board, Vector3Int placementGridPosition, Vector3Int anchorOffset)
        {
            Vector3Int rootGridPosition = new Vector3Int(
                placementGridPosition.x - anchorOffset.x,
                0,
                placementGridPosition.z - anchorOffset.z
            );

            Vector3 worldPosition = board.GridToWorld(rootGridPosition.x, 0, rootGridPosition.z);
            worldPosition.y += GetPlacementLiftFromOffset(anchorOffset);
            return worldPosition;
        }

        private void ClearBoardHighlights()
        {
            if (previewBoard != null)
            {
                previewBoard.ClearHighlights();
            }

            if (gameBoard != null && gameBoard != previewBoard)
            {
                gameBoard.ClearHighlights();
            }
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
                    Color highlightColor = GetRendererColor(renderer);
                    highlightColor.a = 1f;
                    ApplyRendererColor(renderer, highlightColor);
                }
                else
                {
                    Color dimColor = GetRendererColor(renderer);
                    dimColor.a = 0.4f;
                    ApplyRendererColor(renderer, dimColor);
                }
            }
        }

        public void ResetLayerHighlight()
        {
            UpdateVisualFeedback();
        }

        private void ApplyRendererColor(Renderer renderer, Color color)
        {
            if (renderer == null)
            {
                return;
            }

            if (colorPropertyBlock == null)
            {
                colorPropertyBlock = new MaterialPropertyBlock();
            }

            int colorPropertyId = ResolveColorPropertyId(renderer.sharedMaterial);
            renderer.GetPropertyBlock(colorPropertyBlock);
            colorPropertyBlock.SetColor(colorPropertyId, color);
            renderer.SetPropertyBlock(colorPropertyBlock);
            rendererColorCache[renderer.GetInstanceID()] = color;
        }

        private Color GetRendererColor(Renderer renderer)
        {
            if (renderer == null)
            {
                return Color.clear;
            }

            if (rendererColorCache.TryGetValue(renderer.GetInstanceID(), out Color cachedColor))
            {
                return cachedColor;
            }

            Material sharedMaterial = renderer.sharedMaterial;
            int colorPropertyId = ResolveColorPropertyId(sharedMaterial);
            if (sharedMaterial != null && sharedMaterial.HasProperty(colorPropertyId))
            {
                return sharedMaterial.GetColor(colorPropertyId);
            }

            return Color.white;
        }

        private void RemoveCachedRendererColor(GameObject target)
        {
            if (target == null)
            {
                return;
            }

            Renderer renderer = target.GetComponent<Renderer>();
            if (renderer != null)
            {
                rendererColorCache.Remove(renderer.GetInstanceID());
            }
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

        private void ApplyPieceLayer(GameObject target)
        {
            if (target == null || pieceLayerIndex < 0)
            {
                return;
            }

            target.layer = pieceLayerIndex;
        }
    }
}
