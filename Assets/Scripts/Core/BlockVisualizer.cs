using UnityEngine;
using System.Collections.Generic;

namespace Ubongo
{
    /// <summary>
    /// 블록 비주얼 관리 - 상태별 머티리얼, 베벨/라운드 처리, 하이라이트 효과
    /// </summary>
    public class BlockVisualizer : MonoBehaviour
    {
        #region Enums

        /// <summary>
        /// 블록의 시각적 상태
        /// </summary>
        public enum BlockState
        {
            Default,    // 기본 상태
            Hover,      // 마우스 호버
            Selected,   // 선택됨 (드래그 중)
            Placed,     // 배치 완료
            Invalid,    // 무효 위치
            Locked      // 잠김 상태
        }

        #endregion

        #region Constants

        /// <summary>
        /// 베벨 반경 (0.05 units = 5% of block unit size)
        /// </summary>
        public const float BEVEL_RADIUS = 0.05f;

        /// <summary>
        /// 베벨 세그먼트 수
        /// </summary>
        public const int BEVEL_SEGMENTS = 3;

        /// <summary>
        /// 블록 시각적 크기 (5% 갭)
        /// </summary>
        public const float VISUAL_BLOCK_SIZE = 0.95f;

        /// <summary>
        /// 콜리전 크기 (약간의 여유)
        /// </summary>
        public const float COLLISION_BLOCK_SIZE = 0.98f;

        /// <summary>
        /// 엣지 하이라이트 밝기 증가량
        /// </summary>
        public const float EDGE_HIGHLIGHT_BRIGHTNESS = 0.1f;

        /// <summary>
        /// 선택 상태 펄스 속도
        /// </summary>
        public const float PULSE_SPEED = 2f;

        /// <summary>
        /// 이미션 강도 (선택 상태)
        /// </summary>
        public const float EMISSION_INTENSITY = 0.3f;

        #endregion

        #region Serialized Fields

        [Header("Block Configuration")]
        [SerializeField] private GameColors.BlockColorId blockColorId = GameColors.BlockColorId.SunsetOrange;
        [SerializeField] private bool useBeveledMesh = true;

        [Header("Material Settings")]
        [SerializeField] private float smoothness = 0.7f;
        [SerializeField] private float metallic = 0.1f;

        [Header("State Effects")]
        [SerializeField] private float hoverScale = 1.05f;
        [SerializeField] private float selectedScale = 1.1f;
        [SerializeField] private float pulseAmount = 0.05f;

        [Header("Glow Effect")]
        [SerializeField] private float glowIntensity = 0.5f;
        [SerializeField] private Color glowColor = Color.white;

        #endregion

        #region Private Fields

        private BlockState currentState = BlockState.Default;
        private Color baseColor;
        private Material blockMaterial;
        private List<Renderer> blockRenderers = new List<Renderer>();
        private Vector3 originalScale;
        private float pulseTimer = 0f;
        private bool isInitialized = false;

        // Material property IDs for performance
        private static readonly int ColorProperty = Shader.PropertyToID("_Color");
        private static readonly int EmissionColorProperty = Shader.PropertyToID("_EmissionColor");
        private static readonly int SmoothnessProperty = Shader.PropertyToID("_Glossiness");
        private static readonly int MetallicProperty = Shader.PropertyToID("_Metallic");

        #endregion

        #region Properties

        public BlockState CurrentState => currentState;
        public Color BaseColor => baseColor;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            Initialize();
        }

        private void Update()
        {
            if (currentState == BlockState.Selected)
            {
                UpdatePulseEffect();
            }
        }

        #endregion

        #region Initialization

        /// <summary>
        /// 블록 비주얼라이저 초기화
        /// </summary>
        public void Initialize()
        {
            if (isInitialized) return;

            baseColor = GameColors.GetBlockColor(blockColorId);
            originalScale = transform.localScale;

            CollectRenderers();
            CreateMaterials();

            isInitialized = true;
        }

        /// <summary>
        /// 특정 색상으로 초기화
        /// </summary>
        public void Initialize(Color color)
        {
            baseColor = color;
            originalScale = transform.localScale;

            CollectRenderers();
            CreateMaterials();

            isInitialized = true;
        }

        /// <summary>
        /// 블록 색상 ID로 초기화
        /// </summary>
        public void Initialize(GameColors.BlockColorId colorId)
        {
            blockColorId = colorId;
            Initialize();
        }

        private void CollectRenderers()
        {
            blockRenderers.Clear();

            Renderer selfRenderer = GetComponent<Renderer>();
            if (selfRenderer != null)
            {
                blockRenderers.Add(selfRenderer);
            }

            Renderer[] childRenderers = GetComponentsInChildren<Renderer>();
            foreach (var renderer in childRenderers)
            {
                if (!blockRenderers.Contains(renderer))
                {
                    blockRenderers.Add(renderer);
                }
            }
        }

        private void CreateMaterials()
        {
            blockMaterial = new Material(Shader.Find("Standard"));

            Color albedoColor = GameColors.GetBlockAlbedoColor(baseColor);
            blockMaterial.SetColor(ColorProperty, albedoColor);
            blockMaterial.SetFloat(SmoothnessProperty, smoothness);
            blockMaterial.SetFloat(MetallicProperty, metallic);

            ApplyMaterialToRenderers(blockMaterial);
        }

        private void ApplyMaterialToRenderers(Material material)
        {
            foreach (var renderer in blockRenderers)
            {
                if (renderer != null)
                {
                    renderer.material = material;
                }
            }
        }

        #endregion

        #region State Management

        /// <summary>
        /// 블록 상태 설정
        /// </summary>
        public void SetState(BlockState newState)
        {
            if (currentState == newState) return;

            BlockState previousState = currentState;
            currentState = newState;

            ApplyStateVisuals(previousState, newState);
        }

        private void ApplyStateVisuals(BlockState previousState, BlockState newState)
        {
            switch (newState)
            {
                case BlockState.Default:
                    ApplyDefaultState();
                    break;
                case BlockState.Hover:
                    ApplyHoverState();
                    break;
                case BlockState.Selected:
                    ApplySelectedState();
                    break;
                case BlockState.Placed:
                    ApplyPlacedState();
                    break;
                case BlockState.Invalid:
                    ApplyInvalidState();
                    break;
                case BlockState.Locked:
                    ApplyLockedState();
                    break;
            }
        }

        #endregion

        #region State Visual Applications

        private void ApplyDefaultState()
        {
            Color albedoColor = GameColors.GetBlockAlbedoColor(baseColor);
            SetMaterialColor(albedoColor);
            SetEmission(Color.black);
            transform.localScale = originalScale;
        }

        private void ApplyHoverState()
        {
            Color hoverColor = GameColors.GetHoverColor(baseColor);
            SetMaterialColor(hoverColor);
            SetEmission(glowColor * glowIntensity * 0.3f);
            transform.localScale = originalScale * hoverScale;
        }

        private void ApplySelectedState()
        {
            Color selectedColor = GameColors.GetHoverColor(baseColor);
            SetMaterialColor(selectedColor);
            EnableEmission(true);
            pulseTimer = 0f;
        }

        private void ApplyPlacedState()
        {
            Color placedColor = GameColors.GetPlacedColor(baseColor);
            SetMaterialColor(placedColor);
            SetEmission(Color.black);
            transform.localScale = originalScale;
        }

        private void ApplyInvalidState()
        {
            Color invalidColor = GameColors.GetInvalidColor(baseColor);
            SetMaterialColor(invalidColor);
            SetEmission(GameColors.UI.Error * 0.2f);
        }

        private void ApplyLockedState()
        {
            Color lockedColor = GameColors.GetLockedColor(baseColor);
            SetMaterialColor(lockedColor);
            SetEmission(Color.black);
        }

        #endregion

        #region Material Helpers

        private void SetMaterialColor(Color color)
        {
            if (blockMaterial == null) return;

            blockMaterial.SetColor(ColorProperty, color);
        }

        private void SetEmission(Color emissionColor)
        {
            if (blockMaterial == null) return;

            if (emissionColor == Color.black)
            {
                blockMaterial.DisableKeyword("_EMISSION");
            }
            else
            {
                blockMaterial.EnableKeyword("_EMISSION");
                blockMaterial.SetColor(EmissionColorProperty, emissionColor);
            }
        }

        private void EnableEmission(bool enable)
        {
            if (blockMaterial == null) return;

            if (enable)
            {
                blockMaterial.EnableKeyword("_EMISSION");
            }
            else
            {
                blockMaterial.DisableKeyword("_EMISSION");
            }
        }

        #endregion

        #region Effects

        private void UpdatePulseEffect()
        {
            pulseTimer += Time.deltaTime * PULSE_SPEED;

            float pulse = Mathf.Sin(pulseTimer * Mathf.PI * 2f);
            float normalizedPulse = (pulse + 1f) * 0.5f;

            float scale = 1f + (pulseAmount * normalizedPulse);
            transform.localScale = originalScale * selectedScale * scale;

            Color emissionColor = baseColor * EMISSION_INTENSITY * (0.5f + normalizedPulse * 0.5f);
            SetEmission(emissionColor);
        }

        /// <summary>
        /// 하이라이트 효과 적용 (배치 유효성 표시)
        /// </summary>
        public void SetHighlight(bool isValid)
        {
            if (isValid)
            {
                SetEmission(GameColors.Board.ValidPlacement);
            }
            else
            {
                SetEmission(GameColors.Board.InvalidPlacement);
            }
        }

        /// <summary>
        /// 하이라이트 효과 제거
        /// </summary>
        public void ClearHighlight()
        {
            if (currentState == BlockState.Default || currentState == BlockState.Placed)
            {
                SetEmission(Color.black);
            }
        }

        #endregion

        #region Block Creation Helpers

        /// <summary>
        /// 베벨 처리된 큐브 메시 생성
        /// </summary>
        public static Mesh CreateBeveledCubeMesh(float size, float bevelRadius, int bevelSegments)
        {
            // 간단한 베벨 큐브 생성 (실제 구현은 더 복잡할 수 있음)
            Mesh mesh = new Mesh();

            float halfSize = size * 0.5f;
            float innerHalf = halfSize - bevelRadius;

            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();
            List<Vector3> normals = new List<Vector3>();

            // 기본 큐브 버텍스 (베벨 시뮬레이션을 위해 살짝 안쪽으로)
            Vector3[] baseVertices = new Vector3[]
            {
                // Front face
                new Vector3(-innerHalf, -innerHalf, halfSize),
                new Vector3(innerHalf, -innerHalf, halfSize),
                new Vector3(innerHalf, innerHalf, halfSize),
                new Vector3(-innerHalf, innerHalf, halfSize),

                // Back face
                new Vector3(innerHalf, -innerHalf, -halfSize),
                new Vector3(-innerHalf, -innerHalf, -halfSize),
                new Vector3(-innerHalf, innerHalf, -halfSize),
                new Vector3(innerHalf, innerHalf, -halfSize),

                // Top face
                new Vector3(-innerHalf, halfSize, -innerHalf),
                new Vector3(innerHalf, halfSize, -innerHalf),
                new Vector3(innerHalf, halfSize, innerHalf),
                new Vector3(-innerHalf, halfSize, innerHalf),

                // Bottom face
                new Vector3(-innerHalf, -halfSize, innerHalf),
                new Vector3(innerHalf, -halfSize, innerHalf),
                new Vector3(innerHalf, -halfSize, -innerHalf),
                new Vector3(-innerHalf, -halfSize, -innerHalf),

                // Right face
                new Vector3(halfSize, -innerHalf, innerHalf),
                new Vector3(halfSize, -innerHalf, -innerHalf),
                new Vector3(halfSize, innerHalf, -innerHalf),
                new Vector3(halfSize, innerHalf, innerHalf),

                // Left face
                new Vector3(-halfSize, -innerHalf, -innerHalf),
                new Vector3(-halfSize, -innerHalf, innerHalf),
                new Vector3(-halfSize, innerHalf, innerHalf),
                new Vector3(-halfSize, innerHalf, -innerHalf)
            };

            Vector3[] faceNormals = new Vector3[]
            {
                Vector3.forward,
                Vector3.back,
                Vector3.up,
                Vector3.down,
                Vector3.right,
                Vector3.left
            };

            for (int face = 0; face < 6; face++)
            {
                int baseIndex = vertices.Count;

                for (int i = 0; i < 4; i++)
                {
                    vertices.Add(baseVertices[face * 4 + i]);
                    normals.Add(faceNormals[face]);
                }

                triangles.Add(baseIndex);
                triangles.Add(baseIndex + 1);
                triangles.Add(baseIndex + 2);
                triangles.Add(baseIndex);
                triangles.Add(baseIndex + 2);
                triangles.Add(baseIndex + 3);
            }

            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.normals = normals.ToArray();
            mesh.RecalculateBounds();

            return mesh;
        }

        /// <summary>
        /// 블록 오브젝트에 비주얼라이저 추가
        /// </summary>
        public static BlockVisualizer AddToBlock(GameObject blockObject, GameColors.BlockColorId colorId)
        {
            BlockVisualizer visualizer = blockObject.GetComponent<BlockVisualizer>();
            if (visualizer == null)
            {
                visualizer = blockObject.AddComponent<BlockVisualizer>();
            }

            visualizer.Initialize(colorId);
            return visualizer;
        }

        /// <summary>
        /// 블록 오브젝트에 비주얼라이저 추가 (색상 직접 지정)
        /// </summary>
        public static BlockVisualizer AddToBlock(GameObject blockObject, Color color)
        {
            BlockVisualizer visualizer = blockObject.GetComponent<BlockVisualizer>();
            if (visualizer == null)
            {
                visualizer = blockObject.AddComponent<BlockVisualizer>();
            }

            visualizer.Initialize(color);
            return visualizer;
        }

        #endregion

        #region Color Utilities

        /// <summary>
        /// 현재 색상을 새로운 색상으로 변경
        /// </summary>
        public void SetBaseColor(Color newColor)
        {
            baseColor = newColor;

            if (isInitialized)
            {
                ApplyStateVisuals(currentState, currentState);
            }
        }

        /// <summary>
        /// 블록 색상 ID로 색상 변경
        /// </summary>
        public void SetBlockColorId(GameColors.BlockColorId colorId)
        {
            blockColorId = colorId;
            SetBaseColor(GameColors.GetBlockColor(colorId));
        }

        /// <summary>
        /// 현재 상태에 따른 실제 표시 색상 반환
        /// </summary>
        public Color GetCurrentDisplayColor()
        {
            return currentState switch
            {
                BlockState.Default => GameColors.GetBlockAlbedoColor(baseColor),
                BlockState.Hover => GameColors.GetHoverColor(baseColor),
                BlockState.Selected => GameColors.GetHoverColor(baseColor),
                BlockState.Placed => GameColors.GetPlacedColor(baseColor),
                BlockState.Invalid => GameColors.GetInvalidColor(baseColor),
                BlockState.Locked => GameColors.GetLockedColor(baseColor),
                _ => baseColor
            };
        }

        #endregion

        #region Cleanup

        private void OnDestroy()
        {
            if (blockMaterial != null)
            {
                Destroy(blockMaterial);
            }
        }

        #endregion
    }
}
