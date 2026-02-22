using System.Collections.Generic;
using UnityEngine;

namespace Ubongo.Domain.Board
{
    public interface IBoardCommand
    {
        void Resize(int width, int height, int depth);
        void Clear();
        bool TryPlace(string pieceId, IReadOnlyList<Vector3Int> worldCells);
        bool Remove(string pieceId, out IReadOnlyList<Vector3Int> removedCells);
    }
}
