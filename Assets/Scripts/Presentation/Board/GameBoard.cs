using System;
using System.Collections.Generic;
using UnityEngine;
using Ubongo.Application.Placement;
using Ubongo.Core;
using Ubongo.Domain.Board;

namespace Ubongo
{
    public sealed class BoardRuntimeServices
    {
        public BoardPlacementService PlacementService { get; }
        public BoardWinConditionService WinConditionService { get; }

        public BoardRuntimeServices(
            BoardPlacementService placementService,
            BoardWinConditionService winConditionService)
        {
            PlacementService = placementService ?? throw new ArgumentNullException(nameof(placementService));
            WinConditionService = winConditionService ?? throw new ArgumentNullException(nameof(winConditionService));
        }

        public static BoardRuntimeServices CreateDefault()
        {
            return new BoardRuntimeServices(
                new BoardPlacementService(),
                new BoardWinConditionService());
        }
    }

    // TODO: Rename/split this type into a clearer board presentation adapter once
    // placement orchestration is moved fully behind an application-facing port.
    public class GameBoard : MonoBehaviour, IBoardPlacementPort
    {
        private const int MinimumBoardDimension = 1;
        private const string BoardLayerName = "Board";

        [Header("Board Settings")]
        [SerializeField, Min(MinimumBoardDimension)] private int width = 4;
        [SerializeField, Min(MinimumBoardDimension)] private int depth = 2;
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

        private TargetArea targetArea;
        private BoardState boardState;
        private BoardPlacementService placementService;
        private BoardWinConditionService winConditionService;
        private BoardFloorView floorView;
        private string[,,] occupiedPieceIds;
        private PuzzlePiece[,,] occupyingPieces;
        private readonly HashSet<Vector2Int> highlightedFootprint = new HashSet<Vector2Int>();
        private FloorTileHighlightMode currentHighlightMode = FloorTileHighlightMode.None;
        private int boardLayerIndex = -1;
        private bool isConstructed;

        public int Width => width;
        public int Height => TargetArea.RequiredHeight;
        public int Depth => depth;
        public float CellSize => cellSize;
        public float GridStep => cellSize + cellSpacing;
        public float BoardFootprintRatio => boardFootprintRatio;
        public float BoardFootprintSize => cellSize * boardFootprintRatio;
        public TargetArea CurrentTargetArea => targetArea;
        public bool IsConstructed => isConstructed;
        public BoardState State => boardState;

        public event Action<FillState> OnFillStateChanged;
        public event Action OnPuzzleSolved;

        private void Awake()
        {
            NormalizeBoardDimensions();

            boardLayerIndex = LayerMask.NameToLayer(BoardLayerName);
            if (boardLayerIndex < 0)
            {
                Debug.LogWarning($"[{nameof(GameBoard)}] Layer '{BoardLayerName}' not found. Using existing object layers.");
            }

            ApplyBoardLayer(gameObject);
        }

        public void SetTargetArea(TargetArea area)
        {
            Debug.Log($"[RoundFlow][F{Time.frameCount}] GameBoard.SetTargetArea: area null={area == null}, cells={area?.Width * area?.Depth ?? 0}");
            targetArea = area ?? new TargetArea(width, depth);
            RefreshFloorVisuals();
        }

        public void InitializeGrid(Vector3Int size)
        {
            EnsureConstructedOrThrow();

            Vector2Int normalizedSize = NormalizeBoardSize(size.x, size.z);
            width = normalizedSize.x;
            depth = normalizedSize.y;

            EnsureBoardState();
            boardState.Resize(width, TargetArea.RequiredHeight, depth);
            ResizeOccupancyTracking();

            RebuildFloorView();
            SetupDefaultTargetArea();
            ClearHighlights();
            NotifyFillStateChanged();
        }

        public FloorTileView GetCell(int x, int y, int z)
        {
            if (x < 0 || x >= width || y != 0 || z < 0 || z >= depth)
            {
                return null;
            }

            return floorView?.GetTile(x, z);
        }

        public bool IsOccupied(int x, int y, int z)
        {
            return boardState != null && boardState.IsOccupied(new Vector3Int(x, y, z));
        }

        public bool IsWithinBounds(Vector3Int position)
        {
            return position.x >= 0 && position.x < width &&
                   position.y >= 0 && position.y < TargetArea.RequiredHeight &&
                   position.z >= 0 && position.z < depth;
        }

        public Vector3Int WorldToGrid(Vector3 worldPosition)
        {
            Vector3 localPos = transform.InverseTransformPoint(worldPosition);
            float totalCellSize = cellSize + cellSpacing;
            Vector3 startPos = GetLocalGridStartPosition(totalCellSize);
            Vector3 relativePos = localPos - startPos;

            return new Vector3Int(
                Mathf.RoundToInt(relativePos.x / totalCellSize),
                0,
                Mathf.RoundToInt(relativePos.z / totalCellSize));
        }

        public Vector3 GetBoardCenterWorld()
        {
            return transform.position;
        }

        public Bounds GetWorldBounds()
        {
            float totalCellSize = cellSize + cellSpacing;
            Vector3 size = new Vector3(width * totalCellSize, 1f, depth * totalCellSize);
            return new Bounds(transform.TransformPoint(Vector3.zero), size);
        }

        public Vector3 GridToWorld(int x, int y, int z)
        {
            float totalCellSize = cellSize + cellSpacing;
            Vector3 localPos = GetLocalGridStartPosition(totalCellSize) + new Vector3(x * totalCellSize, 0f, z * totalCellSize);
            return transform.TransformPoint(localPos);
        }

        public bool CanPlacePiece(PuzzlePiece piece, Vector3Int gridPosition)
        {
            return ValidatePlacement(piece, gridPosition) == PlacementValidity.Valid;
        }

        public PlacementValidity ValidatePlacement(PuzzlePiece piece, Vector3Int gridPosition)
        {
            EnsureConstructedOrThrow();

            if (piece == null || boardState == null)
            {
                return PlacementValidity.OutOfBounds;
            }

            List<Vector3Int> worldCells = BuildWorldCells(piece.GetBlockPositions(), gridPosition);
            return placementService.Validate(boardState, targetArea, worldCells);
        }

        public void PlacePiece(PuzzlePiece piece, Vector3Int gridPosition)
        {
            EnsureConstructedOrThrow();

            if (piece == null || boardState == null)
            {
                return;
            }

            List<Vector3Int> worldCells = BuildWorldCells(piece.GetBlockPositions(), gridPosition);
            if (placementService.Validate(boardState, targetArea, worldCells) != PlacementValidity.Valid)
            {
                return;
            }

            string pieceId = GetPieceId(piece);
            if (!boardState.TryPlace(pieceId, worldCells))
            {
                return;
            }

            TrackPlacedCells(pieceId, piece, worldCells);
            piece.SetPlaced(true);
            Debug.Log($"[RoundFlow][F{Time.frameCount}] GameBoard.PlacePiece: pieceId={pieceId}, pos={gridPosition}, cellCount={worldCells.Count}");
            ClearHighlights();
            RefreshFloorVisuals();
            NotifyFillStateChanged();
            CheckWinCondition();
        }

        public void RemovePiece(PuzzlePiece piece)
        {
            if (piece == null || boardState == null)
            {
                return;
            }

            string pieceId = GetPieceId(piece);
            bool removedFromBoardState = boardState.Remove(pieceId, out IReadOnlyList<Vector3Int> removedCells);
            Debug.Log($"[RoundFlow][F{Time.frameCount}] GameBoard.RemovePiece: pieceId={pieceId}, removed={removedFromBoardState}, cells={removedCells?.Count ?? 0}");

            if (removedFromBoardState && removedCells != null)
            {
                ClearTrackedCells(removedCells);
            }
            else if (TryClearTrackedPieceCells(piece, pieceId, out List<Vector3Int> fallbackRemovedCells))
            {
                RebuildBoardStateFromTrackedOccupancy();
                removedCells = fallbackRemovedCells;
            }

            piece.SetPlaced(false);
            ClearHighlights();
            RefreshFloorVisuals();
            NotifyFillStateChanged();
        }

        bool IBoardPlacementPort.TryPlace(string pieceId, IReadOnlyList<Vector3Int> worldCells)
        {
            EnsureConstructedOrThrow();

            if (boardState == null || string.IsNullOrWhiteSpace(pieceId) || worldCells == null)
            {
                return false;
            }

            if (!boardState.TryPlace(pieceId, worldCells))
            {
                return false;
            }

            TrackPlacedCells(pieceId, null, worldCells);
            RefreshFloorVisuals();
            NotifyFillStateChanged();
            CheckWinCondition();
            return true;
        }

        bool IBoardPlacementPort.TryRemove(string pieceId, out IReadOnlyList<Vector3Int> removedCells)
        {
            EnsureConstructedOrThrow();

            if (boardState == null)
            {
                removedCells = null;
                return false;
            }

            if (!boardState.Remove(pieceId, out removedCells) || removedCells == null)
            {
                return false;
            }

            ClearTrackedCells(removedCells);
            RefreshFloorVisuals();
            NotifyFillStateChanged();
            return true;
        }

        FillState IBoardPlacementPort.GetFillState(TargetArea placementTargetArea)
        {
            EnsureConstructedOrThrow();
            if (boardState == null || placementTargetArea == null)
            {
                return new FillState(0, 0, 0);
            }

            return winConditionService.CalculateFillState(boardState, placementTargetArea);
        }

        public FillState GetFillState()
        {
            EnsureConstructedOrThrow();

            if (boardState == null || targetArea == null)
            {
                return new FillState(0, 0, 0);
            }

            return winConditionService.CalculateFillState(boardState, targetArea);
        }

        public void HighlightValidPlacement(Vector3Int gridPosition, PuzzlePiece piece)
        {
            highlightedFootprint.Clear();
            currentHighlightMode = FloorTileHighlightMode.None;

            if (piece == null)
            {
                RefreshFloorVisuals();
                return;
            }

            currentHighlightMode = CanPlacePiece(piece, gridPosition)
                ? FloorTileHighlightMode.Valid
                : FloorTileHighlightMode.Invalid;

            foreach (Vector2Int footprint in GetFootprintCells(piece.GetBlockPositions(), gridPosition))
            {
                highlightedFootprint.Add(footprint);
            }

            RefreshFloorVisuals();
        }

        public void ClearHighlights()
        {
            highlightedFootprint.Clear();
            currentHighlightMode = FloorTileHighlightMode.None;
            RefreshFloorVisuals();
        }

        public void ClearBoard()
        {
            if (boardState != null)
            {
                boardState.Clear();
            }

            ClearTrackedOccupancy();
            ClearHighlights();
            RefreshFloorVisuals();
            NotifyFillStateChanged();
        }

        public Vector3Int FindLowestValidPosition(PuzzlePiece piece, int gridX, int gridZ)
        {
            for (int y = 0; y < TargetArea.RequiredHeight; y++)
            {
                Vector3Int testPos = new Vector3Int(gridX, y, gridZ);
                if (CanPlacePiece(piece, testPos))
                {
                    return testPos;
                }
            }

            return new Vector3Int(-1, -1, -1);
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

        public void Construct(BoardRuntimeServices services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (isConstructed)
            {
                throw new InvalidOperationException($"[{nameof(GameBoard)}] {nameof(Construct)} can only be called once.");
            }

            placementService = services.PlacementService ?? throw new ArgumentNullException(nameof(services.PlacementService));
            winConditionService = services.WinConditionService ?? throw new ArgumentNullException(nameof(services.WinConditionService));
            EnsureBoardState();
            ResizeOccupancyTracking();
            isConstructed = true;
        }

        private void CheckWinCondition()
        {
            EnsureConstructedOrThrow();

            if (targetArea == null || boardState == null)
            {
                return;
            }

            ValidationResult result = winConditionService.ValidateSolution(boardState, targetArea);
            if (result.IsSolved)
            {
                OnPuzzleSolved?.Invoke();
            }
        }

        private void NotifyFillStateChanged()
        {
            FillState state = GetFillState();
            OnFillStateChanged?.Invoke(state);
        }

        private void SetupDefaultTargetArea()
        {
            targetArea = new TargetArea(width, depth);
            RefreshFloorVisuals();
        }

        private void RefreshFloorVisuals()
        {
            if (floorView == null)
            {
                return;
            }

            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < depth; z++)
                {
                    floorView.ApplyVisualState(x, z, BuildFloorTileState(x, z));
                }
            }
        }

        private FloorTileVisualState BuildFloorTileState(int x, int z)
        {
            bool isTarget = targetArea != null && targetArea.Contains(x, z);
            bool isOccupied = IsFootprintOccupied(x, z);
            FloorTileHighlightMode highlightMode = highlightedFootprint.Contains(new Vector2Int(x, z))
                ? currentHighlightMode
                : FloorTileHighlightMode.None;

            return new FloorTileVisualState(isTarget, isOccupied, highlightMode);
        }

        private bool IsFootprintOccupied(int x, int z)
        {
            if (occupiedPieceIds == null || x < 0 || x >= width || z < 0 || z >= depth)
            {
                return false;
            }

            for (int y = 0; y < TargetArea.RequiredHeight; y++)
            {
                if (!string.IsNullOrWhiteSpace(occupiedPieceIds[x, y, z]))
                {
                    return true;
                }
            }

            return false;
        }

        private void TrackPlacedCells(string pieceId, PuzzlePiece piece, IReadOnlyList<Vector3Int> worldCells)
        {
            if (occupiedPieceIds == null || occupyingPieces == null)
            {
                return;
            }

            for (int i = 0; i < worldCells.Count; i++)
            {
                Vector3Int cell = worldCells[i];
                if (!IsWithinBounds(cell))
                {
                    continue;
                }

                occupiedPieceIds[cell.x, cell.y, cell.z] = pieceId;
                occupyingPieces[cell.x, cell.y, cell.z] = piece;
            }
        }

        private void ClearTrackedCells(IReadOnlyList<Vector3Int> cells)
        {
            if (occupiedPieceIds == null || occupyingPieces == null || cells == null)
            {
                return;
            }

            for (int i = 0; i < cells.Count; i++)
            {
                Vector3Int cell = cells[i];
                if (!IsWithinBounds(cell))
                {
                    continue;
                }

                occupiedPieceIds[cell.x, cell.y, cell.z] = string.Empty;
                occupyingPieces[cell.x, cell.y, cell.z] = null;
            }
        }

        private bool TryClearTrackedPieceCells(PuzzlePiece piece, string pieceId, out List<Vector3Int> removedCells)
        {
            removedCells = new List<Vector3Int>();
            if (occupiedPieceIds == null || occupyingPieces == null)
            {
                return false;
            }

            bool foundAny = false;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < TargetArea.RequiredHeight; y++)
                {
                    for (int z = 0; z < depth; z++)
                    {
                        bool matchesPieceReference = occupyingPieces[x, y, z] == piece;
                        bool matchesPieceId = !string.IsNullOrWhiteSpace(occupiedPieceIds[x, y, z]) &&
                                              string.Equals(occupiedPieceIds[x, y, z], pieceId, StringComparison.Ordinal);
                        if (!matchesPieceReference && !matchesPieceId)
                        {
                            continue;
                        }

                        occupiedPieceIds[x, y, z] = string.Empty;
                        occupyingPieces[x, y, z] = null;
                        removedCells.Add(new Vector3Int(x, y, z));
                        foundAny = true;
                    }
                }
            }

            return foundAny;
        }

        private void RebuildBoardStateFromTrackedOccupancy()
        {
            if (boardState == null)
            {
                return;
            }

            boardState.Resize(width, TargetArea.RequiredHeight, depth);
            if (occupiedPieceIds == null)
            {
                return;
            }

            Dictionary<string, List<Vector3Int>> pieceCellsById = new Dictionary<string, List<Vector3Int>>();

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < TargetArea.RequiredHeight; y++)
                {
                    for (int z = 0; z < depth; z++)
                    {
                        string pieceId = occupiedPieceIds[x, y, z];
                        if (string.IsNullOrWhiteSpace(pieceId))
                        {
                            continue;
                        }

                        if (!pieceCellsById.TryGetValue(pieceId, out List<Vector3Int> cells))
                        {
                            cells = new List<Vector3Int>();
                            pieceCellsById[pieceId] = cells;
                        }

                        cells.Add(new Vector3Int(x, y, z));
                    }
                }
            }

            foreach (KeyValuePair<string, List<Vector3Int>> entry in pieceCellsById)
            {
                if (!boardState.TryPlace(entry.Key, entry.Value))
                {
                    Debug.LogWarning($"[{nameof(GameBoard)}] Failed to rebuild board state for piece '{entry.Key}'.");
                }
            }
        }

        private void ClearTrackedOccupancy()
        {
            if (occupiedPieceIds != null)
            {
                Array.Clear(occupiedPieceIds, 0, occupiedPieceIds.Length);
            }

            if (occupyingPieces != null)
            {
                Array.Clear(occupyingPieces, 0, occupyingPieces.Length);
            }
        }

        private void ResizeOccupancyTracking()
        {
            occupiedPieceIds = new string[width, TargetArea.RequiredHeight, depth];
            occupyingPieces = new PuzzlePiece[width, TargetArea.RequiredHeight, depth];
        }

        private void RebuildFloorView()
        {
            if (floorView != null)
            {
                floorView.Dispose();
            }

            floorView = new BoardFloorView(
                transform,
                boardLayerIndex,
                cellPrefab,
                defaultMaterial,
                showCellGrid,
                gridLineColor,
                gridLineWidth,
                gridLineYOffset);
            floorView.Rebuild(width, depth, cellSize, cellSpacing, BoardFootprintSize);
        }

        private Vector3 GetLocalGridStartPosition(float totalCellSize)
        {
            return new Vector3(
                -(width - 1) * totalCellSize * 0.5f,
                0f,
                -(depth - 1) * totalCellSize * 0.5f);
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

        private static string GetPieceId(PuzzlePiece piece)
        {
            return piece == null ? string.Empty : piece.GetInstanceID().ToString();
        }

        private static List<Vector3Int> BuildWorldCells(IReadOnlyList<Vector3Int> localBlocks, Vector3Int gridPosition)
        {
            List<Vector3Int> worldCells = new List<Vector3Int>(localBlocks?.Count ?? 0);
            if (localBlocks == null)
            {
                return worldCells;
            }

            for (int i = 0; i < localBlocks.Count; i++)
            {
                worldCells.Add(gridPosition + localBlocks[i]);
            }

            return worldCells;
        }

        private void OnValidate()
        {
            NormalizeBoardDimensions();
            boardFootprintRatio = Mathf.Clamp(boardFootprintRatio, 0.75f, 1f);
            gridLineWidth = Mathf.Max(0.002f, gridLineWidth);
            gridLineYOffset = Mathf.Max(0f, gridLineYOffset);

            if (!UnityEngine.Application.isPlaying || floorView == null)
            {
                return;
            }

            Debug.Log($"[RoundFlow][F{Time.frameCount}] GameBoard.OnValidate: triggering RebuildFloorView (isPlaying={UnityEngine.Application.isPlaying})");
            RebuildFloorView();
            RefreshFloorVisuals();
        }

        private void OnDestroy()
        {
            Debug.Log($"[RoundFlow][F{Time.frameCount}] GameBoard.OnDestroy: hasFloorView={floorView != null}");
            if (floorView != null)
            {
                floorView.Dispose();
                floorView = null;
            }
        }

        private void NormalizeBoardDimensions()
        {
            width = Mathf.Max(MinimumBoardDimension, width);
            depth = Mathf.Max(MinimumBoardDimension, depth);
        }

        private static Vector2Int NormalizeBoardSize(int candidateWidth, int candidateDepth)
        {
            int normalizedWidth = Mathf.Max(MinimumBoardDimension, candidateWidth);
            int normalizedDepth = Mathf.Max(MinimumBoardDimension, candidateDepth);
            return new Vector2Int(normalizedWidth, normalizedDepth);
        }

        private void EnsureBoardState()
        {
            if (boardState != null)
            {
                return;
            }

            boardState = new BoardState(width, TargetArea.RequiredHeight, depth);
        }

        private void EnsureConstructedOrThrow()
        {
            if (isConstructed && placementService != null && winConditionService != null)
            {
                return;
            }

            throw new InvalidOperationException(
                $"[{nameof(GameBoard)}] Runtime services are not initialized. Call {nameof(Construct)} before using board APIs.");
        }
    }
}
