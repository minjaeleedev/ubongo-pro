using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ubongo.Domain.Board
{
    public class BoardState : IBoardQuery, IBoardCommand
    {
        private bool[,,] occupancy;
        private readonly Dictionary<string, List<Vector3Int>> pieceCellsById = new Dictionary<string, List<Vector3Int>>();

        public int Width => occupancy?.GetLength(0) ?? 0;
        public int Height => occupancy?.GetLength(1) ?? 0;
        public int Depth => occupancy?.GetLength(2) ?? 0;

        public BoardState(int width, int height, int depth)
        {
            Resize(width, height, depth);
        }

        public void Resize(int width, int height, int depth)
        {
            int safeWidth = Mathf.Max(1, width);
            int safeHeight = Mathf.Max(1, height);
            int safeDepth = Mathf.Max(1, depth);

            occupancy = new bool[safeWidth, safeHeight, safeDepth];
            pieceCellsById.Clear();
        }

        public void Clear()
        {
            if (occupancy != null)
            {
                System.Array.Clear(occupancy, 0, occupancy.Length);
            }

            pieceCellsById.Clear();
        }

        public bool IsWithinBounds(Vector3Int position)
        {
            return position.x >= 0 && position.x < Width &&
                   position.y >= 0 && position.y < Height &&
                   position.z >= 0 && position.z < Depth;
        }

        public bool IsOccupied(Vector3Int position)
        {
            if (!IsWithinBounds(position))
            {
                return false;
            }

            return occupancy[position.x, position.y, position.z];
        }

        public bool TryPlace(string pieceId, IReadOnlyList<Vector3Int> worldCells)
        {
            if (string.IsNullOrWhiteSpace(pieceId) || worldCells == null || worldCells.Count == 0)
            {
                return false;
            }

            if (pieceCellsById.ContainsKey(pieceId))
            {
                return false;
            }

            for (int i = 0; i < worldCells.Count; i++)
            {
                Vector3Int cell = worldCells[i];
                if (!IsWithinBounds(cell) || occupancy[cell.x, cell.y, cell.z])
                {
                    return false;
                }
            }

            List<Vector3Int> storedCells = new List<Vector3Int>(worldCells.Count);
            for (int i = 0; i < worldCells.Count; i++)
            {
                Vector3Int cell = worldCells[i];
                occupancy[cell.x, cell.y, cell.z] = true;
                storedCells.Add(cell);
            }

            pieceCellsById[pieceId] = storedCells;
            return true;
        }

        public bool Remove(string pieceId, out IReadOnlyList<Vector3Int> removedCells)
        {
            if (string.IsNullOrWhiteSpace(pieceId) || !pieceCellsById.TryGetValue(pieceId, out List<Vector3Int> storedCells))
            {
                removedCells = null;
                return false;
            }

            for (int i = 0; i < storedCells.Count; i++)
            {
                Vector3Int cell = storedCells[i];
                if (IsWithinBounds(cell))
                {
                    occupancy[cell.x, cell.y, cell.z] = false;
                }
            }

            pieceCellsById.Remove(pieceId);
            removedCells = storedCells;
            return true;
        }

        public bool[,,] CreateOccupancySnapshot()
        {
            if (occupancy == null)
            {
                return null;
            }

            bool[,,] snapshot = new bool[Width, Height, Depth];
            CopyOccupancyTo(snapshot);
            return snapshot;
        }

        public void CopyOccupancyTo(bool[,,] destination)
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            if (occupancy == null)
            {
                return;
            }

            if (destination.GetLength(0) != Width ||
                destination.GetLength(1) != Height ||
                destination.GetLength(2) != Depth)
            {
                throw new ArgumentException("Destination dimensions must match current board dimensions.", nameof(destination));
            }

            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    for (int z = 0; z < Depth; z++)
                    {
                        destination[x, y, z] = occupancy[x, y, z];
                    }
                }
            }
        }
    }
}
