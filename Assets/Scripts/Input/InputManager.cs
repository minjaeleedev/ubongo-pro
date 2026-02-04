using UnityEngine;
using UnityEngine.InputSystem;
using System;

namespace Ubongo
{
    public class InputManager : MonoBehaviour
    {
        public static InputManager Instance { get; private set; }

        [Header("Layer Masks")]
        [SerializeField] private LayerMask pieceLayerMask = -1;
        [SerializeField] private LayerMask boardLayerMask = -1;

        [Header("Settings")]
        [SerializeField] private float dragHeight = 2f;

        private UbongoInputActions inputActions;
        private Camera mainCamera;

        private PuzzlePiece hoveredPiece;
        private PuzzlePiece selectedPiece;
        private bool isDragging;
        private Vector3 dragOffset;

        private Vector2 currentPointerPosition;

        public float DragHeight => dragHeight;
        public Vector2 PointerPosition => currentPointerPosition;
        public PuzzlePiece SelectedPiece => selectedPiece;
        public bool IsDragging => isDragging;

        public event Action<PuzzlePiece> OnPieceHoverEnter;
        public event Action<PuzzlePiece> OnPieceHoverExit;
        public event Action<PuzzlePiece> OnPieceSelectStart;
        public event Action<PuzzlePiece, Vector3> OnPieceDrag;
        public event Action<PuzzlePiece> OnPieceSelectEnd;
        public event Action<Vector3, float> OnPieceRotate;

        public event Action OnToggleHelp;
        public event Action OnToggleDebugPanel;
        public event Action OnToggleGenerator;
        public event Action OnToggleRotation;
        public event Action OnQuickGenerate;
        public event Action OnAutoSolve;
        public event Action OnStepSolution;
        public event Action OnExport;
        public event Action OnToggleGrid;
        public event Action OnToggleWireframe;
        public event Action OnToggleStats;
        public event Action OnResetPuzzle;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            inputActions = new UbongoInputActions();
            mainCamera = Camera.main;
        }

        private void OnEnable()
        {
            inputActions.Enable();
            SubscribeToGameplayActions();
            SubscribeToDebugActions();
        }

        private void OnDisable()
        {
            UnsubscribeFromGameplayActions();
            UnsubscribeFromDebugActions();
            inputActions.Disable();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
            inputActions?.Dispose();
        }

        private void Update()
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            UpdatePointerPosition();
            UpdateHover();
            UpdateDrag();
        }

        private void SubscribeToGameplayActions()
        {
            inputActions.Gameplay.Point.performed += OnPointPerformed;
            inputActions.Gameplay.Click.started += OnClickStarted;
            inputActions.Gameplay.Click.canceled += OnClickCanceled;
            inputActions.Gameplay.RotateYNeg.performed += OnRotateYNeg;
            inputActions.Gameplay.RotateYPos.performed += OnRotateYPos;
            inputActions.Gameplay.RotateXPos.performed += OnRotateXPos;
            inputActions.Gameplay.RotateZPos.performed += OnRotateZPos;
        }

        private void UnsubscribeFromGameplayActions()
        {
            inputActions.Gameplay.Point.performed -= OnPointPerformed;
            inputActions.Gameplay.Click.started -= OnClickStarted;
            inputActions.Gameplay.Click.canceled -= OnClickCanceled;
            inputActions.Gameplay.RotateYNeg.performed -= OnRotateYNeg;
            inputActions.Gameplay.RotateYPos.performed -= OnRotateYPos;
            inputActions.Gameplay.RotateXPos.performed -= OnRotateXPos;
            inputActions.Gameplay.RotateZPos.performed -= OnRotateZPos;
        }

        private void SubscribeToDebugActions()
        {
            inputActions.Debug.Help.performed += ctx => OnToggleHelp?.Invoke();
            inputActions.Debug.ToggleDebug.performed += ctx => OnToggleDebugPanel?.Invoke();
            inputActions.Debug.ToggleGenerator.performed += ctx => OnToggleGenerator?.Invoke();
            inputActions.Debug.ToggleRotation.performed += ctx => OnToggleRotation?.Invoke();
            inputActions.Debug.QuickGenerate.performed += ctx => OnQuickGenerate?.Invoke();
            inputActions.Debug.AutoSolve.performed += ctx => OnAutoSolve?.Invoke();
            inputActions.Debug.StepSolution.performed += ctx => OnStepSolution?.Invoke();
            inputActions.Debug.Export.performed += ctx => OnExport?.Invoke();
            inputActions.Debug.ToggleGrid.performed += ctx => OnToggleGrid?.Invoke();
            inputActions.Debug.ToggleWireframe.performed += ctx => OnToggleWireframe?.Invoke();
            inputActions.Debug.ToggleStats.performed += ctx => OnToggleStats?.Invoke();
            inputActions.Debug.ResetPuzzle.performed += ctx => OnResetPuzzle?.Invoke();
        }

        private void UnsubscribeFromDebugActions()
        {
            inputActions.Debug.Help.performed -= ctx => OnToggleHelp?.Invoke();
            inputActions.Debug.ToggleDebug.performed -= ctx => OnToggleDebugPanel?.Invoke();
            inputActions.Debug.ToggleGenerator.performed -= ctx => OnToggleGenerator?.Invoke();
            inputActions.Debug.ToggleRotation.performed -= ctx => OnToggleRotation?.Invoke();
            inputActions.Debug.QuickGenerate.performed -= ctx => OnQuickGenerate?.Invoke();
            inputActions.Debug.AutoSolve.performed -= ctx => OnAutoSolve?.Invoke();
            inputActions.Debug.StepSolution.performed -= ctx => OnStepSolution?.Invoke();
            inputActions.Debug.Export.performed -= ctx => OnExport?.Invoke();
            inputActions.Debug.ToggleGrid.performed -= ctx => OnToggleGrid?.Invoke();
            inputActions.Debug.ToggleWireframe.performed -= ctx => OnToggleWireframe?.Invoke();
            inputActions.Debug.ToggleStats.performed -= ctx => OnToggleStats?.Invoke();
            inputActions.Debug.ResetPuzzle.performed -= ctx => OnResetPuzzle?.Invoke();
        }

        private void OnPointPerformed(InputAction.CallbackContext ctx)
        {
            currentPointerPosition = ctx.ReadValue<Vector2>();
        }

        private void UpdatePointerPosition()
        {
            if (Mouse.current != null)
            {
                currentPointerPosition = Mouse.current.position.ReadValue();
            }
        }

        private void OnClickStarted(InputAction.CallbackContext ctx)
        {
            if (mainCamera == null) return;

            Ray ray = mainCamera.ScreenPointToRay(currentPointerPosition);

            if (Physics.Raycast(ray, out RaycastHit hit, 100f, pieceLayerMask))
            {
                PuzzlePiece piece = hit.collider.GetComponentInParent<PuzzlePiece>();
                if (piece != null)
                {
                    selectedPiece = piece;
                    isDragging = true;

                    dragOffset = piece.transform.position - hit.point;
                    dragOffset.y = 0;

                    OnPieceSelectStart?.Invoke(piece);
                }
            }
        }

        private void OnClickCanceled(InputAction.CallbackContext ctx)
        {
            if (selectedPiece != null)
            {
                isDragging = false;
                OnPieceSelectEnd?.Invoke(selectedPiece);
                selectedPiece = null;
            }
        }

        private void UpdateHover()
        {
            if (isDragging || mainCamera == null) return;

            Ray ray = mainCamera.ScreenPointToRay(currentPointerPosition);

            if (Physics.Raycast(ray, out RaycastHit hit, 100f, pieceLayerMask))
            {
                PuzzlePiece piece = hit.collider.GetComponentInParent<PuzzlePiece>();

                if (piece != hoveredPiece)
                {
                    if (hoveredPiece != null)
                    {
                        OnPieceHoverExit?.Invoke(hoveredPiece);
                    }

                    hoveredPiece = piece;

                    if (hoveredPiece != null)
                    {
                        OnPieceHoverEnter?.Invoke(hoveredPiece);
                    }
                }
            }
            else if (hoveredPiece != null)
            {
                OnPieceHoverExit?.Invoke(hoveredPiece);
                hoveredPiece = null;
            }
        }

        private void UpdateDrag()
        {
            if (!isDragging || selectedPiece == null || mainCamera == null) return;

            Ray ray = mainCamera.ScreenPointToRay(currentPointerPosition);
            Plane dragPlane = new Plane(Vector3.up, Vector3.up * dragHeight);

            if (dragPlane.Raycast(ray, out float distance))
            {
                Vector3 point = ray.GetPoint(distance);
                Vector3 newPosition = point + dragOffset;
                OnPieceDrag?.Invoke(selectedPiece, newPosition);
            }
        }

        private void OnRotateYNeg(InputAction.CallbackContext ctx)
        {
            if (isDragging && selectedPiece != null)
            {
                OnPieceRotate?.Invoke(Vector3.up, -90f);
            }
        }

        private void OnRotateYPos(InputAction.CallbackContext ctx)
        {
            if (isDragging && selectedPiece != null)
            {
                OnPieceRotate?.Invoke(Vector3.up, 90f);
            }
        }

        private void OnRotateXPos(InputAction.CallbackContext ctx)
        {
            if (isDragging && selectedPiece != null)
            {
                OnPieceRotate?.Invoke(Vector3.right, 90f);
            }
        }

        private void OnRotateZPos(InputAction.CallbackContext ctx)
        {
            if (isDragging && selectedPiece != null)
            {
                OnPieceRotate?.Invoke(Vector3.forward, 90f);
            }
        }

        public Ray GetPointerRay()
        {
            if (mainCamera == null)
            {
                return new Ray(Vector3.zero, Vector3.forward);
            }
            return mainCamera.ScreenPointToRay(currentPointerPosition);
        }

        public Vector3 GetWorldPosition(float height = 0f)
        {
            if (mainCamera == null) return Vector3.zero;

            Ray ray = mainCamera.ScreenPointToRay(currentPointerPosition);
            Plane plane = new Plane(Vector3.up, Vector3.up * height);

            if (plane.Raycast(ray, out float distance))
            {
                return ray.GetPoint(distance);
            }

            return Vector3.zero;
        }

        public bool TryGetBoardHit(out RaycastHit hit)
        {
            if (mainCamera == null)
            {
                hit = default;
                return false;
            }

            Ray ray = mainCamera.ScreenPointToRay(currentPointerPosition);
            return Physics.Raycast(ray, out hit, 100f, boardLayerMask);
        }
    }
}
