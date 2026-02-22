using System.Collections.Generic;
using UnityEngine;

namespace Ubongo.Domain.Board
{
    public readonly struct PlacedPiece
    {
        public string PieceId { get; }
        public Vector3Int Origin { get; }
        public IReadOnlyList<Vector3Int> LocalBlocks { get; }

        public PlacedPiece(string pieceId, Vector3Int origin, IReadOnlyList<Vector3Int> localBlocks)
        {
            PieceId = pieceId;
            Origin = origin;

            if (localBlocks == null)
            {
                LocalBlocks = System.Array.Empty<Vector3Int>();
            }
            else
            {
                List<Vector3Int> copiedBlocks = new List<Vector3Int>(localBlocks.Count);
                for (int i = 0; i < localBlocks.Count; i++)
                {
                    copiedBlocks.Add(localBlocks[i]);
                }
                LocalBlocks = copiedBlocks;
            }
        }

        public List<Vector3Int> ToWorldCells()
        {
            List<Vector3Int> worldCells = new List<Vector3Int>(LocalBlocks.Count);
            for (int i = 0; i < LocalBlocks.Count; i++)
            {
                worldCells.Add(Origin + LocalBlocks[i]);
            }
            return worldCells;
        }
    }
}
