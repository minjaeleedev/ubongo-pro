using System.Collections.Generic;
using UnityEngine;
using Ubongo.Core;

namespace Ubongo.Application.Placement
{
    public readonly struct PlacementResult
    {
        public PlacementValidity Validity { get; }
        public IReadOnlyList<Vector3Int> WorldCells { get; }
        public FillState FillState { get; }

        public bool Succeeded => Validity == PlacementValidity.Valid;

        public PlacementResult(
            PlacementValidity validity,
            IReadOnlyList<Vector3Int> worldCells,
            FillState fillState)
        {
            Validity = validity;
            WorldCells = worldCells ?? System.Array.Empty<Vector3Int>();
            FillState = fillState;
        }
    }
}
