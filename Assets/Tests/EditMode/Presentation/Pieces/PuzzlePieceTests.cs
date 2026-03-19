using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Ubongo.Core;

namespace Ubongo.Tests.EditMode.Presentation.Pieces
{
    public class PuzzlePieceTests
    {
        [Test]
        public void PuzzlePiece_HeightProfile_AppliesExpectedLocalYOffset()
        {
            GameObject pieceObject = new GameObject("Piece");
            PuzzlePiece piece = pieceObject.AddComponent<PuzzlePiece>();

            piece.SetBlockPositions(new List<Vector3Int>
            {
                new Vector3Int(0, 0, 0),
                new Vector3Int(0, 1, 0)
            });

            float[] localY = pieceObject.transform
                .Cast<Transform>()
                .Where(child => child.name == "Cube")
                .Select(child => child.localPosition.y)
                .OrderBy(y => y)
                .ToArray();

            Assert.AreEqual(2, localY.Length);
            Assert.AreEqual(0.4f, localY[0], 0.001f);
            Assert.AreEqual(1.2f, localY[1], 0.001f);

            Object.DestroyImmediate(pieceObject);
        }

        [Test]
        public void PuzzlePiece_GetBlockPositions_NormalizesRotatedOffsets()
        {
            GameObject pieceObject = new GameObject("Piece");
            PuzzlePiece piece = pieceObject.AddComponent<PuzzlePiece>();
            piece.SetBlockPositions(new List<Vector3Int>
            {
                new Vector3Int(0, 0, 0),
                new Vector3Int(1, 0, 0)
            });

            pieceObject.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            List<Vector3Int> blocks = piece.GetBlockPositions();

            Assert.AreEqual(2, blocks.Count);
            Assert.IsTrue(blocks.All(b => b.x >= 0 && b.y >= 0 && b.z >= 0));
            CollectionAssert.AreEquivalent(
                new[]
                {
                    new Vector3Int(0, 0, 0),
                    new Vector3Int(1, 0, 0)
                },
                blocks
            );

            Object.DestroyImmediate(pieceObject);
        }

        [Test]
        public void PuzzlePiece_SetBlockPositions_EmptyList_UsesPieceCatalogFallback()
        {
            GameObject pieceObject = new GameObject("Piece");
            PuzzlePiece piece = pieceObject.AddComponent<PuzzlePiece>();

            piece.SetBlockPositions(new List<Vector3Int>());
            string generatedSignature = BuildNormalizedSignature(piece.GetBlockPositions());

            HashSet<string> catalogSignatures = PieceCatalog.GetAllPieces()
                .Select(definition => BuildNormalizedSignature(definition.Blocks))
                .ToHashSet();

            Assert.IsTrue(catalogSignatures.Contains(generatedSignature));
            Object.DestroyImmediate(pieceObject);
        }

        private static string BuildNormalizedSignature(IEnumerable<Vector3Int> blocks)
        {
            Vector3Int[] normalized = RotationUtil.NormalizeToOrigin(blocks.ToArray());
            return string.Join(
                "|",
                normalized
                    .OrderBy(position => position.x)
                    .ThenBy(position => position.y)
                    .ThenBy(position => position.z)
                    .Select(position => $"{position.x},{position.y},{position.z}"));
        }
    }
}
