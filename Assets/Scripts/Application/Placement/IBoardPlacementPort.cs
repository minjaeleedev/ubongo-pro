using System.Collections.Generic;
using UnityEngine;
using Ubongo.Core;
using Ubongo.Domain.Board;

namespace Ubongo.Application.Placement
{
    public interface IBoardPlacementPort
    {
        BoardState State { get; }

        bool TryPlace(string pieceId, IReadOnlyList<Vector3Int> worldCells);

        bool TryRemove(string pieceId, out IReadOnlyList<Vector3Int> removedCells);

        FillState GetFillState(TargetArea targetArea);
    }
}
