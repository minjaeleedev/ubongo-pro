using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ubongo.Core
{
    /// <summary>
    /// Defines the target area that must be filled in a puzzle.
    /// Ubongo 3D requires exactly 2 layers to be filled.
    /// </summary>
    [Serializable]
    public class TargetArea
    {
        public const int RequiredHeight = 2;

        private HashSet<Vector2Int> footprint;
        private int width;
        private int depth;

        /// <summary>
        /// Number of columns in the footprint (XZ positions).
        /// </summary>
        public int FootprintSize => footprint?.Count ?? 0;

        /// <summary>
        /// Total cells to fill (footprint * height).
        /// </summary>
        public int TotalCells => FootprintSize * RequiredHeight;

        /// <summary>
        /// Width of the target area bounding box.
        /// </summary>
        public int Width => width;

        /// <summary>
        /// Depth of the target area bounding box.
        /// </summary>
        public int Depth => depth;

        /// <summary>
        /// Creates an empty target area.
        /// </summary>
        public TargetArea()
        {
            footprint = new HashSet<Vector2Int>();
            width = 0;
            depth = 0;
        }

        /// <summary>
        /// Creates a rectangular target area.
        /// </summary>
        public TargetArea(int width, int depth)
        {
            this.width = width;
            this.depth = depth;
            footprint = new HashSet<Vector2Int>();

            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < depth; z++)
                {
                    footprint.Add(new Vector2Int(x, z));
                }
            }
        }

        /// <summary>
        /// Creates a target area from a list of XZ positions.
        /// </summary>
        public TargetArea(IEnumerable<Vector2Int> positions)
        {
            footprint = new HashSet<Vector2Int>(positions);
            RecalculateBounds();
        }

        /// <summary>
        /// Creates a target area from a list of 3D positions (Y is ignored).
        /// </summary>
        public TargetArea(IEnumerable<Vector3Int> positions)
        {
            footprint = new HashSet<Vector2Int>();
            foreach (var pos in positions)
            {
                footprint.Add(new Vector2Int(pos.x, pos.z));
            }
            RecalculateBounds();
        }

        private void RecalculateBounds()
        {
            if (footprint == null || footprint.Count == 0)
            {
                width = 0;
                depth = 0;
                return;
            }

            int minX = int.MaxValue;
            int maxX = int.MinValue;
            int minZ = int.MaxValue;
            int maxZ = int.MinValue;

            foreach (var pos in footprint)
            {
                if (pos.x < minX) minX = pos.x;
                if (pos.x > maxX) maxX = pos.x;
                if (pos.y < minZ) minZ = pos.y;
                if (pos.y > maxZ) maxZ = pos.y;
            }

            width = maxX - minX + 1;
            depth = maxZ - minZ + 1;
        }

        /// <summary>
        /// Checks if an XZ position is within the target area.
        /// </summary>
        public bool Contains(int x, int z)
        {
            return footprint.Contains(new Vector2Int(x, z));
        }

        /// <summary>
        /// Checks if a 3D position is within the target area (including height check).
        /// </summary>
        public bool Contains(Vector3Int position)
        {
            if (position.y < 0 || position.y >= RequiredHeight)
            {
                return false;
            }

            return Contains(position.x, position.z);
        }

        /// <summary>
        /// Checks if a 3D position is within the footprint (ignores Y).
        /// </summary>
        public bool ContainsXZ(Vector3Int position)
        {
            return Contains(position.x, position.z);
        }

        /// <summary>
        /// Returns all column (XZ) positions in the target area.
        /// </summary>
        public IEnumerable<Vector2Int> GetColumnPositions()
        {
            return footprint;
        }

        /// <summary>
        /// Returns all 3D cell positions in the target area (both layers).
        /// </summary>
        public IEnumerable<Vector3Int> GetAllCells()
        {
            foreach (var pos in footprint)
            {
                for (int y = 0; y < RequiredHeight; y++)
                {
                    yield return new Vector3Int(pos.x, y, pos.y);
                }
            }
        }

        /// <summary>
        /// Returns all cell positions for a specific layer.
        /// </summary>
        public IEnumerable<Vector3Int> GetLayerCells(int layer)
        {
            if (layer < 0 || layer >= RequiredHeight)
            {
                yield break;
            }

            foreach (var pos in footprint)
            {
                yield return new Vector3Int(pos.x, layer, pos.y);
            }
        }

        /// <summary>
        /// Adds a column position to the target area.
        /// </summary>
        public void AddColumn(int x, int z)
        {
            footprint.Add(new Vector2Int(x, z));
            RecalculateBounds();
        }

        /// <summary>
        /// Removes a column position from the target area.
        /// </summary>
        public bool RemoveColumn(int x, int z)
        {
            bool removed = footprint.Remove(new Vector2Int(x, z));
            if (removed)
            {
                RecalculateBounds();
            }
            return removed;
        }

        /// <summary>
        /// Clears all positions from the target area.
        /// </summary>
        public void Clear()
        {
            footprint.Clear();
            width = 0;
            depth = 0;
        }

        /// <summary>
        /// Creates a deep copy of this target area.
        /// </summary>
        public TargetArea Clone()
        {
            return new TargetArea(footprint);
        }

        /// <summary>
        /// Creates a rectangular target area.
        /// </summary>
        public static TargetArea CreateRectangular(int width, int depth)
        {
            return new TargetArea(width, depth);
        }

        /// <summary>
        /// Creates an L-shaped target area.
        /// </summary>
        public static TargetArea CreateLShaped(int width, int depth, int cutWidth, int cutDepth)
        {
            var area = new TargetArea();

            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < depth; z++)
                {
                    // Cut out the corner
                    if (x >= width - cutWidth && z >= depth - cutDepth)
                    {
                        continue;
                    }
                    area.AddColumn(x, z);
                }
            }

            return area;
        }

        /// <summary>
        /// Creates a T-shaped target area.
        /// </summary>
        public static TargetArea CreateTShaped(int topWidth, int topDepth, int stemWidth, int stemDepth)
        {
            var area = new TargetArea();

            // Top bar
            int stemOffsetX = (topWidth - stemWidth) / 2;
            for (int x = 0; x < topWidth; x++)
            {
                for (int z = 0; z < topDepth; z++)
                {
                    area.AddColumn(x, z);
                }
            }

            // Stem
            for (int x = stemOffsetX; x < stemOffsetX + stemWidth; x++)
            {
                for (int z = topDepth; z < topDepth + stemDepth; z++)
                {
                    area.AddColumn(x, z);
                }
            }

            return area;
        }

        /// <summary>
        /// Creates a target area from a 2D boolean mask.
        /// True values indicate included cells.
        /// </summary>
        public static TargetArea CreateFromMask(bool[,] mask)
        {
            var area = new TargetArea();

            int width = mask.GetLength(0);
            int depth = mask.GetLength(1);

            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < depth; z++)
                {
                    if (mask[x, z])
                    {
                        area.AddColumn(x, z);
                    }
                }
            }

            return area;
        }
    }

    /// <summary>
    /// Tracks the fill state of the target area.
    /// </summary>
    [Serializable]
    public struct FillState
    {
        /// <summary>
        /// Progress of layer 0 (0.0 to 1.0).
        /// </summary>
        public float Layer0Progress;

        /// <summary>
        /// Progress of layer 1 (0.0 to 1.0).
        /// </summary>
        public float Layer1Progress;

        /// <summary>
        /// Total progress (0.0 to 1.0).
        /// </summary>
        public float TotalProgress;

        /// <summary>
        /// Number of filled cells in layer 0.
        /// </summary>
        public int Layer0FilledCount;

        /// <summary>
        /// Number of filled cells in layer 1.
        /// </summary>
        public int Layer1FilledCount;

        /// <summary>
        /// Total number of target cells.
        /// </summary>
        public int TotalTargetCells;

        /// <summary>
        /// True if the target area is completely filled.
        /// </summary>
        public bool IsComplete;

        public FillState(int layer0Filled, int layer1Filled, int totalTarget)
        {
            Layer0FilledCount = layer0Filled;
            Layer1FilledCount = layer1Filled;
            TotalTargetCells = totalTarget;

            int targetPerLayer = totalTarget / 2;
            Layer0Progress = targetPerLayer > 0 ? (float)layer0Filled / targetPerLayer : 0f;
            Layer1Progress = targetPerLayer > 0 ? (float)layer1Filled / targetPerLayer : 0f;
            TotalProgress = totalTarget > 0 ? (float)(layer0Filled + layer1Filled) / totalTarget : 0f;
            IsComplete = layer0Filled == targetPerLayer && layer1Filled == targetPerLayer;
        }
    }
}
