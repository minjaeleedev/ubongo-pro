using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ubongo.Core
{
    /// <summary>
    /// Defines a 3D polycube piece with its block positions and metadata.
    /// </summary>
    [Serializable]
    public struct PieceDefinition
    {
        public string Id;
        public string Name;
        public Vector3Int[] Blocks;
        public Color DefaultColor;
        public int SymmetryGroup;

        public int BlockCount => Blocks?.Length ?? 0;

        public PieceDefinition(string id, string name, Vector3Int[] blocks, Color color, int symmetryGroup = 0)
        {
            Id = id;
            Name = name;
            Blocks = blocks;
            DefaultColor = color;
            SymmetryGroup = symmetryGroup;
        }

        /// <summary>
        /// Returns normalized block positions (translated to origin).
        /// </summary>
        public Vector3Int[] GetNormalizedBlocks()
        {
            if (Blocks == null || Blocks.Length == 0)
            {
                return Array.Empty<Vector3Int>();
            }

            return RotationUtil.NormalizeToOrigin(Blocks);
        }
    }

    /// <summary>
    /// Catalog of standard Ubongo 3D pieces (8 unique shapes).
    /// </summary>
    public static class PieceCatalog
    {
        public static readonly PieceDefinition SmallL = new PieceDefinition(
            "1",
            "Small-L",
            new Vector3Int[]
            {
                new Vector3Int(0, 0, 0),
                new Vector3Int(1, 0, 0),
                new Vector3Int(0, 0, 1)
            },
            new Color(1f, 0.2f, 0.2f), // Red
            1
        );

        public static readonly PieceDefinition Line3 = new PieceDefinition(
            "2",
            "Line-3",
            new Vector3Int[]
            {
                new Vector3Int(0, 0, 0),
                new Vector3Int(1, 0, 0),
                new Vector3Int(2, 0, 0)
            },
            new Color(0.2f, 0.4f, 1f), // Blue
            2
        );

        public static readonly PieceDefinition Corner3D = new PieceDefinition(
            "3",
            "Corner-3D",
            new Vector3Int[]
            {
                new Vector3Int(0, 0, 0),
                new Vector3Int(1, 0, 0),
                new Vector3Int(0, 1, 0)
            },
            new Color(0.2f, 0.8f, 0.2f), // Green
            3
        );

        public static readonly PieceDefinition TShape = new PieceDefinition(
            "4",
            "T-Shape",
            new Vector3Int[]
            {
                new Vector3Int(0, 0, 0),
                new Vector3Int(1, 0, 0),
                new Vector3Int(2, 0, 0),
                new Vector3Int(1, 0, 1)
            },
            new Color(1f, 1f, 0.2f), // Yellow
            4
        );

        public static readonly PieceDefinition LShape = new PieceDefinition(
            "5",
            "L-Shape",
            new Vector3Int[]
            {
                new Vector3Int(0, 0, 0),
                new Vector3Int(0, 0, 1),
                new Vector3Int(0, 0, 2),
                new Vector3Int(1, 0, 2)
            },
            new Color(0.6f, 0.2f, 0.8f), // Purple
            5
        );

        public static readonly PieceDefinition ZShape = new PieceDefinition(
            "6",
            "Z-Shape",
            new Vector3Int[]
            {
                new Vector3Int(0, 0, 0),
                new Vector3Int(1, 0, 0),
                new Vector3Int(1, 0, 1),
                new Vector3Int(2, 0, 1)
            },
            new Color(1f, 0.5f, 0.1f), // Orange
            6
        );

        public static readonly PieceDefinition Stairs3D = new PieceDefinition(
            "7",
            "Stairs-3D",
            new Vector3Int[]
            {
                new Vector3Int(0, 0, 0),
                new Vector3Int(1, 0, 0),
                new Vector3Int(1, 1, 0),
                new Vector3Int(2, 1, 0)
            },
            new Color(0.2f, 0.9f, 0.9f), // Cyan
            7
        );

        public static readonly PieceDefinition Tower = new PieceDefinition(
            "8",
            "Tower",
            new Vector3Int[]
            {
                new Vector3Int(0, 0, 0),
                new Vector3Int(1, 0, 0),
                new Vector3Int(0, 1, 0),
                new Vector3Int(1, 1, 0)
            },
            new Color(0.6f, 0.4f, 0.2f), // Brown
            8
        );

        /// <summary>
        /// Returns all 8 standard Ubongo 3D pieces.
        /// </summary>
        public static PieceDefinition[] GetAllPieces()
        {
            return new PieceDefinition[]
            {
                SmallL,
                Line3,
                Corner3D,
                TShape,
                LShape,
                ZShape,
                Stairs3D,
                Tower
            };
        }

        /// <summary>
        /// Returns only 3-block pieces.
        /// </summary>
        public static PieceDefinition[] GetThreeBlockPieces()
        {
            return new PieceDefinition[]
            {
                SmallL,
                Line3,
                Corner3D
            };
        }

        /// <summary>
        /// Returns only 4-block pieces.
        /// </summary>
        public static PieceDefinition[] GetFourBlockPieces()
        {
            return new PieceDefinition[]
            {
                TShape,
                LShape,
                ZShape,
                Stairs3D,
                Tower
            };
        }

        /// <summary>
        /// Gets a piece definition by its ID.
        /// </summary>
        public static PieceDefinition? GetPieceById(string id)
        {
            foreach (var piece in GetAllPieces())
            {
                if (piece.Id == id)
                {
                    return piece;
                }
            }
            return null;
        }

        /// <summary>
        /// Gets a piece definition by its name.
        /// </summary>
        public static PieceDefinition? GetPieceByName(string name)
        {
            foreach (var piece in GetAllPieces())
            {
                if (piece.Name == name)
                {
                    return piece;
                }
            }
            return null;
        }
    }

    /// <summary>
    /// Utility class for 3D rotations (24 cube symmetry group operations).
    /// </summary>
    public static class RotationUtil
    {
        /// <summary>
        /// All 24 rotation matrices for cube symmetry group.
        /// Each matrix is stored as a 3x3 integer array [row][col].
        /// </summary>
        public static readonly int[][,] AllRotationMatrices = GenerateAll24Rotations();

        private static int[][,] GenerateAll24Rotations()
        {
            var rotations = new List<int[,]>();

            // Identity matrix
            int[,] identity = new int[,] { { 1, 0, 0 }, { 0, 1, 0 }, { 0, 0, 1 } };

            // Basic rotation matrices (90 degrees)
            int[,] rotX90 = new int[,] { { 1, 0, 0 }, { 0, 0, -1 }, { 0, 1, 0 } };
            int[,] rotY90 = new int[,] { { 0, 0, 1 }, { 0, 1, 0 }, { -1, 0, 0 } };
            int[,] rotZ90 = new int[,] { { 0, -1, 0 }, { 1, 0, 0 }, { 0, 0, 1 } };

            // Generate all 24 unique rotations
            var uniqueRotations = new HashSet<string>();

            int[,] current = (int[,])identity.Clone();

            // Generate by combining rotations
            for (int faceRot = 0; faceRot < 6; faceRot++)
            {
                for (int spinRot = 0; spinRot < 4; spinRot++)
                {
                    string key = MatrixToKey(current);
                    if (!uniqueRotations.Contains(key))
                    {
                        uniqueRotations.Add(key);
                        rotations.Add((int[,])current.Clone());
                    }

                    // Rotate around Z axis
                    current = MultiplyMatrices(current, rotZ90);
                }

                // Change face
                if (faceRot < 3)
                {
                    current = MultiplyMatrices(current, rotX90);
                }
                else if (faceRot == 3)
                {
                    current = MultiplyMatrices(current, rotY90);
                }
                else if (faceRot == 4)
                {
                    current = MultiplyMatrices(current, rotY90);
                    current = MultiplyMatrices(current, rotY90);
                }
            }

            // Ensure we have exactly 24 rotations by exploring systematically
            if (rotations.Count < 24)
            {
                rotations.Clear();
                uniqueRotations.Clear();
                ExploreAllRotations(identity, rotX90, rotY90, rotZ90, rotations, uniqueRotations);
            }

            return rotations.ToArray();
        }

        private static void ExploreAllRotations(
            int[,] identity,
            int[,] rotX90,
            int[,] rotY90,
            int[,] rotZ90,
            List<int[,]> rotations,
            HashSet<string> uniqueRotations)
        {
            var queue = new Queue<int[,]>();
            queue.Enqueue(identity);
            uniqueRotations.Add(MatrixToKey(identity));
            rotations.Add((int[,])identity.Clone());

            while (queue.Count > 0 && rotations.Count < 24)
            {
                var current = queue.Dequeue();

                var neighbors = new[]
                {
                    MultiplyMatrices(current, rotX90),
                    MultiplyMatrices(current, rotY90),
                    MultiplyMatrices(current, rotZ90)
                };

                foreach (var neighbor in neighbors)
                {
                    string key = MatrixToKey(neighbor);
                    if (!uniqueRotations.Contains(key))
                    {
                        uniqueRotations.Add(key);
                        rotations.Add((int[,])neighbor.Clone());
                        queue.Enqueue(neighbor);
                    }
                }
            }
        }

        private static string MatrixToKey(int[,] matrix)
        {
            return $"{matrix[0, 0]},{matrix[0, 1]},{matrix[0, 2]}," +
                   $"{matrix[1, 0]},{matrix[1, 1]},{matrix[1, 2]}," +
                   $"{matrix[2, 0]},{matrix[2, 1]},{matrix[2, 2]}";
        }

        private static int[,] MultiplyMatrices(int[,] a, int[,] b)
        {
            int[,] result = new int[3, 3];
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    result[i, j] = a[i, 0] * b[0, j] + a[i, 1] * b[1, j] + a[i, 2] * b[2, j];
                }
            }
            return result;
        }

        /// <summary>
        /// Applies a rotation matrix to a single block position.
        /// </summary>
        public static Vector3Int ApplyRotation(Vector3Int block, int rotationIndex)
        {
            if (rotationIndex < 0 || rotationIndex >= AllRotationMatrices.Length)
            {
                return block;
            }

            var matrix = AllRotationMatrices[rotationIndex];
            return new Vector3Int(
                matrix[0, 0] * block.x + matrix[0, 1] * block.y + matrix[0, 2] * block.z,
                matrix[1, 0] * block.x + matrix[1, 1] * block.y + matrix[1, 2] * block.z,
                matrix[2, 0] * block.x + matrix[2, 1] * block.y + matrix[2, 2] * block.z
            );
        }

        /// <summary>
        /// Rotates all blocks of a piece by the specified rotation index.
        /// </summary>
        public static Vector3Int[] RotatePiece(Vector3Int[] blocks, int rotationIndex)
        {
            if (blocks == null || blocks.Length == 0)
            {
                return Array.Empty<Vector3Int>();
            }

            var rotated = new Vector3Int[blocks.Length];
            for (int i = 0; i < blocks.Length; i++)
            {
                rotated[i] = ApplyRotation(blocks[i], rotationIndex);
            }

            return NormalizeToOrigin(rotated);
        }

        /// <summary>
        /// Normalizes block positions to origin (minimum coordinates become 0).
        /// </summary>
        public static Vector3Int[] NormalizeToOrigin(Vector3Int[] blocks)
        {
            if (blocks == null || blocks.Length == 0)
            {
                return Array.Empty<Vector3Int>();
            }

            int minX = int.MaxValue;
            int minY = int.MaxValue;
            int minZ = int.MaxValue;

            foreach (var block in blocks)
            {
                if (block.x < minX) minX = block.x;
                if (block.y < minY) minY = block.y;
                if (block.z < minZ) minZ = block.z;
            }

            var normalized = new Vector3Int[blocks.Length];
            for (int i = 0; i < blocks.Length; i++)
            {
                normalized[i] = new Vector3Int(
                    blocks[i].x - minX,
                    blocks[i].y - minY,
                    blocks[i].z - minZ
                );
            }

            // Sort for consistent ordering
            Array.Sort(normalized, (a, b) =>
            {
                int cmpX = a.x.CompareTo(b.x);
                if (cmpX != 0) return cmpX;
                int cmpY = a.y.CompareTo(b.y);
                if (cmpY != 0) return cmpY;
                return a.z.CompareTo(b.z);
            });

            return normalized;
        }

        /// <summary>
        /// Returns indices of unique rotations for a piece (accounts for symmetry).
        /// </summary>
        public static int[] GetUniqueRotations(Vector3Int[] normalizedBlocks)
        {
            if (normalizedBlocks == null || normalizedBlocks.Length == 0)
            {
                return new int[] { 0 };
            }

            var uniqueOrientations = new Dictionary<string, int>();

            for (int i = 0; i < AllRotationMatrices.Length; i++)
            {
                var rotated = RotatePiece(normalizedBlocks, i);
                string key = BlocksToKey(rotated);

                if (!uniqueOrientations.ContainsKey(key))
                {
                    uniqueOrientations[key] = i;
                }
            }

            return uniqueOrientations.Values.ToArray();
        }

        private static string BlocksToKey(Vector3Int[] blocks)
        {
            var sorted = blocks.OrderBy(b => b.x).ThenBy(b => b.y).ThenBy(b => b.z);
            return string.Join(";", sorted.Select(b => $"{b.x},{b.y},{b.z}"));
        }

        /// <summary>
        /// Gets the canonical form of a piece (lexicographically smallest across all rotations).
        /// </summary>
        public static Vector3Int[] GetCanonicalForm(Vector3Int[] blocks)
        {
            if (blocks == null || blocks.Length == 0)
            {
                return Array.Empty<Vector3Int>();
            }

            Vector3Int[] canonical = NormalizeToOrigin(blocks);
            string canonicalKey = BlocksToKey(canonical);

            for (int i = 1; i < AllRotationMatrices.Length; i++)
            {
                var rotated = RotatePiece(blocks, i);
                string key = BlocksToKey(rotated);

                if (string.CompareOrdinal(key, canonicalKey) < 0)
                {
                    canonical = rotated;
                    canonicalKey = key;
                }
            }

            return canonical;
        }

        /// <summary>
        /// Rotates a piece 90 degrees around the X axis.
        /// </summary>
        public static Vector3Int[] RotateX90(Vector3Int[] blocks)
        {
            // Find the rotation index for X+90
            // X+90 matrix: { {1,0,0}, {0,0,-1}, {0,1,0} }
            return RotatePiece(blocks, 1);
        }

        /// <summary>
        /// Rotates a piece 90 degrees around the Y axis.
        /// </summary>
        public static Vector3Int[] RotateY90(Vector3Int[] blocks)
        {
            // Find the rotation index for Y+90
            return RotatePiece(blocks, 4);
        }

        /// <summary>
        /// Rotates a piece 90 degrees around the Z axis.
        /// </summary>
        public static Vector3Int[] RotateZ90(Vector3Int[] blocks)
        {
            // Find the rotation index for Z+90
            return RotatePiece(blocks, 8);
        }
    }
}
