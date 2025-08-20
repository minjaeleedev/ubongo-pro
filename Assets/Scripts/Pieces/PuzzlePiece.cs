using UnityEngine;
using System.Collections.Generic;

namespace Ubongo
{
    public class PuzzlePiece : MonoBehaviour
    {
        [Header("Piece Configuration")]
        [SerializeField] private List<Vector3Int> blockPositions = new List<Vector3Int>();
        [SerializeField] private Color pieceColor = Color.blue;
        
        [Header("Drag Settings")]
        [SerializeField] private float dragHeight = 2f;
        [SerializeField] private float rotationSpeed = 90f;
        [SerializeField] private LayerMask boardLayer;
        
        private bool isDragging = false;
        private bool isPlaced = false;
        private Vector3 originalPosition;
        private Quaternion originalRotation;
        private GameBoard gameBoard;
        private Camera mainCamera;
        private Vector3 dragOffset;
        private List<GameObject> blockObjects = new List<GameObject>();
        
        public bool IsDragging => isDragging;
        public bool IsPlaced => isPlaced;
        
        private void Start()
        {
            mainCamera = Camera.main;
            gameBoard = FindObjectOfType<GameBoard>();
            originalPosition = transform.position;
            originalRotation = transform.rotation;
            
            CreateBlockVisuals();
        }
        
        private void CreateBlockVisuals()
        {
            foreach (GameObject block in blockObjects)
            {
                if (block != null)
                    Destroy(block);
            }
            blockObjects.Clear();
            
            if (blockPositions.Count == 0)
            {
                GenerateDefaultShape();
            }
            
            foreach (Vector3Int blockPos in blockPositions)
            {
                GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
                block.transform.parent = transform;
                block.transform.localPosition = blockPos;
                block.transform.localScale = Vector3.one * 0.95f;
                
                Renderer renderer = block.GetComponent<Renderer>();
                renderer.material.color = pieceColor;
                
                Collider collider = block.GetComponent<Collider>();
                collider.enabled = true;
                
                blockObjects.Add(block);
            }
            
            BoxCollider pieceCollider = gameObject.AddComponent<BoxCollider>();
            CalculateBounds(pieceCollider);
        }
        
        private void GenerateDefaultShape()
        {
            int shapeType = Random.Range(0, 5);
            blockPositions.Clear();
            
            switch (shapeType)
            {
                case 0:
                    blockPositions.Add(new Vector3Int(0, 0, 0));
                    blockPositions.Add(new Vector3Int(1, 0, 0));
                    blockPositions.Add(new Vector3Int(0, 0, 1));
                    break;
                    
                case 1:
                    blockPositions.Add(new Vector3Int(0, 0, 0));
                    blockPositions.Add(new Vector3Int(1, 0, 0));
                    blockPositions.Add(new Vector3Int(2, 0, 0));
                    break;
                    
                case 2:
                    blockPositions.Add(new Vector3Int(0, 0, 0));
                    blockPositions.Add(new Vector3Int(1, 0, 0));
                    blockPositions.Add(new Vector3Int(0, 1, 0));
                    blockPositions.Add(new Vector3Int(1, 1, 0));
                    break;
                    
                case 3:
                    blockPositions.Add(new Vector3Int(0, 0, 0));
                    blockPositions.Add(new Vector3Int(1, 0, 0));
                    blockPositions.Add(new Vector3Int(1, 0, 1));
                    blockPositions.Add(new Vector3Int(2, 0, 1));
                    break;
                    
                case 4:
                    blockPositions.Add(new Vector3Int(0, 0, 0));
                    blockPositions.Add(new Vector3Int(0, 1, 0));
                    blockPositions.Add(new Vector3Int(0, 0, 1));
                    break;
            }
        }
        
        private void CalculateBounds(BoxCollider collider)
        {
            if (blockPositions.Count == 0) return;
            
            Vector3 min = blockPositions[0];
            Vector3 max = blockPositions[0];
            
            foreach (Vector3Int pos in blockPositions)
            {
                min = Vector3.Min(min, pos);
                max = Vector3.Max(max, pos);
            }
            
            collider.center = (min + max) * 0.5f;
            collider.size = max - min + Vector3.one;
        }
        
        private void OnMouseDown()
        {
            if (isPlaced)
            {
                gameBoard.RemovePiece(this);
            }
            
            isDragging = true;
            
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                dragOffset = transform.position - hit.point;
                dragOffset.y = 0;
            }
        }
        
        private void OnMouseDrag()
        {
            if (!isDragging) return;
            
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            Plane dragPlane = new Plane(Vector3.up, Vector3.up * dragHeight);
            
            if (dragPlane.Raycast(ray, out float distance))
            {
                Vector3 point = ray.GetPoint(distance);
                transform.position = point + dragOffset;
            }
            
            if (Input.GetKeyDown(KeyCode.Q))
            {
                transform.Rotate(Vector3.up, -90f);
            }
            else if (Input.GetKeyDown(KeyCode.E))
            {
                transform.Rotate(Vector3.up, 90f);
            }
            else if (Input.GetKeyDown(KeyCode.R))
            {
                transform.Rotate(Vector3.right, 90f);
            }
            else if (Input.GetKeyDown(KeyCode.F))
            {
                transform.Rotate(Vector3.forward, 90f);
            }
        }
        
        private void OnMouseUp()
        {
            isDragging = false;
            
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f, boardLayer))
            {
                GameBoard board = hit.collider.GetComponentInParent<GameBoard>();
                if (board != null)
                {
                    Vector3Int gridPos = board.WorldToGrid(transform.position);
                    
                    if (board.CanPlacePiece(this, gridPos))
                    {
                        Vector3 snapPosition = board.GridToWorld(gridPos.x, gridPos.y, gridPos.z);
                        transform.position = snapPosition;
                        board.PlacePiece(this, gridPos);
                    }
                    else
                    {
                        ReturnToOriginalPosition();
                    }
                }
                else
                {
                    ReturnToOriginalPosition();
                }
            }
            else
            {
                ReturnToOriginalPosition();
            }
            
            gameBoard.ClearHighlights();
        }
        
        private void ReturnToOriginalPosition()
        {
            transform.position = originalPosition;
            transform.rotation = originalRotation;
            isPlaced = false;
        }
        
        public List<Vector3Int> GetBlockPositions()
        {
            List<Vector3Int> rotatedPositions = new List<Vector3Int>();
            
            foreach (Vector3Int originalPos in blockPositions)
            {
                Vector3 rotated = transform.rotation * originalPos;
                Vector3Int roundedPos = new Vector3Int(
                    Mathf.RoundToInt(rotated.x),
                    Mathf.RoundToInt(rotated.y),
                    Mathf.RoundToInt(rotated.z)
                );
                rotatedPositions.Add(roundedPos);
            }
            
            return rotatedPositions;
        }
        
        public void SetPlaced(bool placed)
        {
            isPlaced = placed;
            
            foreach (GameObject block in blockObjects)
            {
                if (block != null)
                {
                    Renderer renderer = block.GetComponent<Renderer>();
                    Color color = pieceColor;
                    color.a = placed ? 1f : 0.8f;
                    renderer.material.color = color;
                }
            }
        }
        
        public void SetPieceColor(Color color)
        {
            pieceColor = color;
            foreach (GameObject block in blockObjects)
            {
                if (block != null)
                {
                    Renderer renderer = block.GetComponent<Renderer>();
                    renderer.material.color = color;
                }
            }
        }
        
        public void SetBlockPositions(List<Vector3Int> positions)
        {
            blockPositions = new List<Vector3Int>(positions);
            CreateBlockVisuals();
        }
    }
}