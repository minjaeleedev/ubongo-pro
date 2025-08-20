using UnityEngine;
using System.Collections.Generic;

namespace Ubongo
{
    [System.Serializable]
    public class PieceShape
    {
        public string name;
        public List<Vector3Int> blocks;
        public Color color;
        
        public PieceShape(string n, List<Vector3Int> b, Color c)
        {
            name = n;
            blocks = b;
            color = c;
        }
    }
    
    [System.Serializable]
    public class LevelData
    {
        public int levelNumber;
        public float timeLimit;
        public List<PieceShape> pieces;
        public Vector3Int boardSize;
    }
    
    public class LevelGenerator : MonoBehaviour
    {
        [Header("Piece Spawn Settings")]
        [SerializeField] private GameObject piecePrefab;
        [SerializeField] private Transform pieceSpawnArea;
        [SerializeField] private float spawnSpacing = 3f;
        
        [Header("Predefined Shapes")]
        private List<PieceShape> availableShapes;
        private List<GameObject> currentPieces = new List<GameObject>();
        
        private void Awake()
        {
            InitializeShapes();
        }
        
        private void InitializeShapes()
        {
            availableShapes = new List<PieceShape>
            {
                new PieceShape("L-Shape", new List<Vector3Int> {
                    new Vector3Int(0, 0, 0),
                    new Vector3Int(1, 0, 0),
                    new Vector3Int(0, 0, 1)
                }, new Color(1f, 0.2f, 0.2f)),
                
                new PieceShape("T-Shape", new List<Vector3Int> {
                    new Vector3Int(0, 0, 0),
                    new Vector3Int(1, 0, 0),
                    new Vector3Int(2, 0, 0),
                    new Vector3Int(1, 0, 1)
                }, new Color(0.2f, 1f, 0.2f)),
                
                new PieceShape("Line", new List<Vector3Int> {
                    new Vector3Int(0, 0, 0),
                    new Vector3Int(1, 0, 0),
                    new Vector3Int(2, 0, 0)
                }, new Color(0.2f, 0.2f, 1f)),
                
                new PieceShape("Cube", new List<Vector3Int> {
                    new Vector3Int(0, 0, 0),
                    new Vector3Int(1, 0, 0),
                    new Vector3Int(0, 0, 1),
                    new Vector3Int(1, 0, 1),
                    new Vector3Int(0, 1, 0),
                    new Vector3Int(1, 1, 0),
                    new Vector3Int(0, 1, 1),
                    new Vector3Int(1, 1, 1)
                }, new Color(1f, 1f, 0.2f)),
                
                new PieceShape("Z-Shape", new List<Vector3Int> {
                    new Vector3Int(0, 0, 0),
                    new Vector3Int(1, 0, 0),
                    new Vector3Int(1, 0, 1),
                    new Vector3Int(2, 0, 1)
                }, new Color(1f, 0.2f, 1f)),
                
                new PieceShape("Stairs", new List<Vector3Int> {
                    new Vector3Int(0, 0, 0),
                    new Vector3Int(1, 0, 0),
                    new Vector3Int(1, 1, 0),
                    new Vector3Int(2, 1, 0)
                }, new Color(0.2f, 1f, 1f)),
                
                new PieceShape("Corner", new List<Vector3Int> {
                    new Vector3Int(0, 0, 0),
                    new Vector3Int(1, 0, 0),
                    new Vector3Int(0, 0, 1),
                    new Vector3Int(0, 1, 0)
                }, new Color(1f, 0.5f, 0.2f)),
                
                new PieceShape("Plus", new List<Vector3Int> {
                    new Vector3Int(1, 0, 0),
                    new Vector3Int(0, 0, 1),
                    new Vector3Int(1, 0, 1),
                    new Vector3Int(2, 0, 1),
                    new Vector3Int(1, 0, 2)
                }, new Color(0.5f, 0.2f, 1f))
            };
        }
        
        public void GenerateLevel(int levelNumber)
        {
            ClearCurrentPieces();
            
            int pieceCount = Mathf.Min(3 + levelNumber / 2, 6);
            List<PieceShape> levelPieces = SelectPiecesForLevel(levelNumber, pieceCount);
            
            SpawnPieces(levelPieces);
        }
        
        private List<PieceShape> SelectPiecesForLevel(int levelNumber, int count)
        {
            List<PieceShape> selectedPieces = new List<PieceShape>();
            List<PieceShape> tempShapes = new List<PieceShape>(availableShapes);
            
            for (int i = 0; i < count && tempShapes.Count > 0; i++)
            {
                int randomIndex = Random.Range(0, tempShapes.Count);
                selectedPieces.Add(tempShapes[randomIndex]);
                tempShapes.RemoveAt(randomIndex);
            }
            
            return selectedPieces;
        }
        
        private void SpawnPieces(List<PieceShape> pieces)
        {
            if (pieceSpawnArea == null)
            {
                GameObject spawnArea = new GameObject("PieceSpawnArea");
                spawnArea.transform.position = new Vector3(8f, 0f, 0f);
                pieceSpawnArea = spawnArea.transform;
            }
            
            float currentX = 0f;
            float currentZ = 0f;
            int piecesPerRow = 3;
            int currentPieceIndex = 0;
            
            foreach (PieceShape shape in pieces)
            {
                GameObject pieceObject = CreatePieceObject(shape);
                
                Vector3 spawnPosition = pieceSpawnArea.position + new Vector3(currentX, 0f, currentZ);
                pieceObject.transform.position = spawnPosition;
                
                currentPieces.Add(pieceObject);
                
                currentPieceIndex++;
                currentX += spawnSpacing;
                
                if (currentPieceIndex % piecesPerRow == 0)
                {
                    currentX = 0f;
                    currentZ -= spawnSpacing;
                }
            }
        }
        
        private GameObject CreatePieceObject(PieceShape shape)
        {
            GameObject pieceObject;
            
            if (piecePrefab != null)
            {
                pieceObject = Instantiate(piecePrefab);
            }
            else
            {
                pieceObject = new GameObject($"Piece_{shape.name}");
            }
            
            PuzzlePiece puzzlePiece = pieceObject.GetComponent<PuzzlePiece>();
            if (puzzlePiece == null)
            {
                puzzlePiece = pieceObject.AddComponent<PuzzlePiece>();
            }
            
            puzzlePiece.SetBlockPositions(shape.blocks);
            puzzlePiece.SetPieceColor(shape.color);
            
            return pieceObject;
        }
        
        private void ClearCurrentPieces()
        {
            foreach (GameObject piece in currentPieces)
            {
                if (piece != null)
                {
                    Destroy(piece);
                }
            }
            currentPieces.Clear();
        }
        
        public LevelData GetLevelData(int levelNumber)
        {
            LevelData data = new LevelData();
            data.levelNumber = levelNumber;
            data.timeLimit = 60f + (levelNumber * 5f);
            data.boardSize = new Vector3Int(4, 3, 2);
            
            int pieceCount = Mathf.Min(3 + levelNumber / 2, 6);
            data.pieces = SelectPiecesForLevel(levelNumber, pieceCount);
            
            return data;
        }
    }
}