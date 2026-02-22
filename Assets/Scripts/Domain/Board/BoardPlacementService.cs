using System.Collections.Generic;
using UnityEngine;
using Ubongo.Core;

namespace Ubongo.Domain.Board
{
    public class BoardPlacementService
    {
        public PlacementValidity Validate(IBoardQuery board, TargetArea targetArea, IReadOnlyList<Vector3Int> worldCells)
        {
            if (board == null || worldCells == null || worldCells.Count == 0)
            {
                return PlacementValidity.OutOfBounds;
            }

            for (int i = 0; i < worldCells.Count; i++)
            {
                Vector3Int cell = worldCells[i];

                if (cell.x < 0 || cell.x >= board.Width ||
                    cell.z < 0 || cell.z >= board.Depth)
                {
                    return PlacementValidity.OutOfBounds;
                }

                if (cell.y < 0 || cell.y >= board.Height)
                {
                    return PlacementValidity.HeightExceeded;
                }

                if (targetArea != null && !targetArea.Contains(cell.x, cell.z))
                {
                    return PlacementValidity.OutsideTarget;
                }

                if (board.IsOccupied(cell))
                {
                    return PlacementValidity.Collision;
                }
            }

            return PlacementValidity.Valid;
        }
    }
}
