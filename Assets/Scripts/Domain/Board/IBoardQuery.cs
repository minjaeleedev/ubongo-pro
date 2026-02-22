using UnityEngine;

namespace Ubongo.Domain.Board
{
    public interface IBoardQuery
    {
        int Width { get; }
        int Height { get; }
        int Depth { get; }

        bool IsWithinBounds(Vector3Int position);
        bool IsOccupied(Vector3Int position);
    }
}
