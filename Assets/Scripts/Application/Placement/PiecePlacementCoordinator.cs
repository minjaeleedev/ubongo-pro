using System;
using System.Collections.Generic;
using UnityEngine;
using Ubongo.Core;
using Ubongo.Domain.Board;

namespace Ubongo.Application.Placement
{
    public sealed class PiecePlacementCoordinator : IPiecePlacementUseCase
    {
        private readonly IBoardPlacementPort boardPlacementPort;
        private readonly BoardPlacementService boardPlacementService;

        public PiecePlacementCoordinator(
            IBoardPlacementPort boardPlacementPort,
            BoardPlacementService boardPlacementService)
        {
            this.boardPlacementPort = boardPlacementPort ?? throw new ArgumentNullException(nameof(boardPlacementPort));
            this.boardPlacementService = boardPlacementService ?? throw new ArgumentNullException(nameof(boardPlacementService));
        }

        public PlacementResult Preview(PlacementRequest request)
        {
            List<Vector3Int> worldCells = BuildWorldCells(request.LocalBlocks, request.GridPosition);
            PlacementValidity validity = boardPlacementService.Validate(boardPlacementPort.State, request.TargetArea, worldCells);
            return new PlacementResult(validity, worldCells, boardPlacementPort.GetFillState(request.TargetArea));
        }

        public PlacementResult Place(PlacementRequest request)
        {
            PlacementResult preview = Preview(request);
            if (!preview.Succeeded)
            {
                return preview;
            }

            if (!boardPlacementPort.TryPlace(request.PieceId, preview.WorldCells))
            {
                return new PlacementResult(PlacementValidity.Collision, preview.WorldCells, boardPlacementPort.GetFillState(request.TargetArea));
            }

            return new PlacementResult(PlacementValidity.Valid, preview.WorldCells, boardPlacementPort.GetFillState(request.TargetArea));
        }

        public bool Remove(string pieceId, out PlacementResult result)
        {
            if (!boardPlacementPort.TryRemove(pieceId, out IReadOnlyList<Vector3Int> removedCells))
            {
                result = default;
                return false;
            }

            result = new PlacementResult(
                PlacementValidity.Valid,
                removedCells,
                new FillState(0, 0, 0));
            return true;
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
    }
}
