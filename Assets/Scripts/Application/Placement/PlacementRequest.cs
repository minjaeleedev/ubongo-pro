using System.Collections.Generic;
using UnityEngine;
using Ubongo.Core;

namespace Ubongo.Application.Placement
{
    public readonly struct PlacementRequest
    {
        public string PieceId { get; }
        public IReadOnlyList<Vector3Int> LocalBlocks { get; }
        public Vector3Int GridPosition { get; }
        public TargetArea TargetArea { get; }

        public PlacementRequest(
            string pieceId,
            IReadOnlyList<Vector3Int> localBlocks,
            Vector3Int gridPosition,
            TargetArea targetArea)
        {
            PieceId = pieceId;
            LocalBlocks = localBlocks;
            GridPosition = gridPosition;
            TargetArea = targetArea;
        }
    }
}
