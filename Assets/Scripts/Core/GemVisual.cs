using UnityEngine;
using System.Collections;

namespace Ubongo
{
    /// <summary>
    /// 보석 비주얼 관리 - 4종 보석 색상/형태, 획득 애니메이션, 아이콘 스타일
    /// </summary>
    public class GemVisual : MonoBehaviour
    {
        #region Constants

        /// <summary>
        /// 보석 기본 크기 (0.3 x 0.3 x 0.2 Unity units)
        /// </summary>
        public static readonly Vector3 GEM_SIZE = new Vector3(0.3f, 0.3f, 0.2f);

        /// <summary>
        /// 보석 아이콘 크기 (64x64px base)
        /// </summary>
        public const int ICON_SIZE = 64;

        /// <summary>
        /// 보석 투명도
        /// </summary>
        public const float GEM_TRANSPARENCY = 0.85f;

        /// <summary>
        /// 굴절률 (Index of Refraction)
        /// </summary>
        public const float INDEX_OF_REFRACTION = 1.5f;

        /// <summary>
        /// 스페큘러 값
        /// </summary>
        public const float SPECULAR = 0.9f;

        /// <summary>
        /// 매끄러움 값
        /// </summary>
        public const float SMOOTHNESS = 0.95f;

        #endregion

        #region Animation Constants

        /// <summary>
        /// 전체 획득 애니메이션 시간
        /// </summary>
        public const float TOTAL_ANIMATION_DURATION = 1.2f;

        /// <summary>
        /// Phase 1 (Pop) 시간
        /// </summary>
        public const float PHASE1_DURATION = 0.3f;

        /// <summary>
        /// Phase 2 (Float and Shine) 시간
        /// </summary>
        public const float PHASE2_DURATION = 0.5f;

        /// <summary>
        /// Phase 3 (Collect) 시간
        /// </summary>
        public const float PHASE3_DURATION = 0.4f;

        #endregion

        #region Serialized Fields

        [Header("Gem Configuration")]
        [SerializeField] private GameColors.GemType gemType = GameColors.GemType.Ruby;
        [SerializeField] private int facetCount = 16;

        [Header("Visual Settings")]
        [SerializeField] private bool useSparkleEffect = true;
        [SerializeField] private float sparkleInterval = 0.5f;
        [SerializeField] private int sparkleParticleCount = 12;

        [Header("Animation Settings")]
        [SerializeField] private float popScale = 1.2f;
        [SerializeField] private float floatHeight = 0.5f;
        [SerializeField] private float rotationSpeed = 360f;
        [SerializeField] private AnimationCurve popCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] private AnimationCurve floatCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] private AnimationCurve collectCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("References")]
        [SerializeField] private ParticleSystem sparkleParticles;
        [SerializeField] private Transform targetUIPosition;

        #endregion

        #region Private Fields

        private Material gemMaterial;
        private Color gemColor;
        private Vector3 originalPosition;
        private Vector3 originalScale;
        private bool isAnimating = false;
        private bool isCollected = false;

        // Material property IDs
        private static readonly int ColorProperty = Shader.PropertyToID("_Color");
        private static readonly int EmissionColorProperty = Shader.PropertyToID("_EmissionColor");
        private static readonly int SmoothnessProperty = Shader.PropertyToID("_Glossiness");

        #endregion

        #region Properties

        public GameColors.GemType Type => gemType;
        public Color GemColor => gemColor;
        public bool IsCollected => isCollected;
        public bool IsAnimating => isAnimating;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            Initialize();
        }

        private void Start()
        {
            originalPosition = transform.position;
            originalScale = transform.localScale;

            if (useSparkleEffect)
            {
                StartCoroutine(SparkleRoutine());
            }
        }

        #endregion

        #region Initialization

        /// <summary>
        /// 보석 비주얼 초기화
        /// </summary>
        public void Initialize()
        {
            gemColor = GameColors.Gems.GetGemColor(gemType);
            facetCount = GetFacetCountForType(gemType);

            CreateGemMaterial();
            CreateGemMesh();
            SetupSparkleParticles();
        }

        /// <summary>
        /// 특정 타입으로 초기화
        /// </summary>
        public void Initialize(GameColors.GemType type)
        {
            gemType = type;
            Initialize();
        }

        private void CreateGemMaterial()
        {
            // 투명 셰이더 사용
            gemMaterial = new Material(Shader.Find("Standard"));
            gemMaterial.SetFloat("_Mode", 3); // Transparent mode
            gemMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
            gemMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            gemMaterial.SetInt("_ZWrite", 0);
            gemMaterial.DisableKeyword("_ALPHATEST_ON");
            gemMaterial.DisableKeyword("_ALPHABLEND_ON");
            gemMaterial.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            gemMaterial.renderQueue = 3000;

            Color transparentColor = gemColor;
            transparentColor.a = GEM_TRANSPARENCY;
            gemMaterial.SetColor(ColorProperty, transparentColor);
            gemMaterial.SetFloat(SmoothnessProperty, SMOOTHNESS);

            // Inner glow effect via emission
            Color emissionColor = gemColor * 0.2f;
            gemMaterial.EnableKeyword("_EMISSION");
            gemMaterial.SetColor(EmissionColorProperty, emissionColor);

            Renderer renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = gemMaterial;
            }
        }

        private void CreateGemMesh()
        {
            MeshFilter meshFilter = GetComponent<MeshFilter>();
            if (meshFilter == null)
            {
                meshFilter = gameObject.AddComponent<MeshFilter>();
            }

            Mesh gemMesh = CreateGemMeshForType(gemType);
            meshFilter.mesh = gemMesh;

            MeshRenderer renderer = GetComponent<MeshRenderer>();
            if (renderer == null)
            {
                renderer = gameObject.AddComponent<MeshRenderer>();
            }
            renderer.material = gemMaterial;

            // Collider for interaction
            MeshCollider collider = GetComponent<MeshCollider>();
            if (collider == null)
            {
                collider = gameObject.AddComponent<MeshCollider>();
                collider.convex = true;
                collider.isTrigger = true;
            }

            transform.localScale = GEM_SIZE;
        }

        private void SetupSparkleParticles()
        {
            if (sparkleParticles == null)
            {
                GameObject particleObj = new GameObject("SparkleParticles");
                particleObj.transform.SetParent(transform);
                particleObj.transform.localPosition = Vector3.zero;

                sparkleParticles = particleObj.AddComponent<ParticleSystem>();

                var main = sparkleParticles.main;
                main.startSize = 0.02f;
                main.startLifetime = 0.5f;
                main.startSpeed = 0.5f;
                main.startColor = Color.white;
                main.maxParticles = 20;
                main.playOnAwake = false;

                var emission = sparkleParticles.emission;
                emission.rateOverTime = 0;

                var shape = sparkleParticles.shape;
                shape.shapeType = ParticleSystemShapeType.Sphere;
                shape.radius = 0.1f;

                var colorOverLifetime = sparkleParticles.colorOverLifetime;
                colorOverLifetime.enabled = true;
                Gradient gradient = new Gradient();
                gradient.SetKeys(
                    new GradientColorKey[] {
                        new GradientColorKey(Color.white, 0f),
                        new GradientColorKey(gemColor, 0.5f),
                        new GradientColorKey(Color.white, 1f)
                    },
                    new GradientAlphaKey[] {
                        new GradientAlphaKey(0f, 0f),
                        new GradientAlphaKey(1f, 0.3f),
                        new GradientAlphaKey(0f, 1f)
                    }
                );
                colorOverLifetime.color = gradient;
            }
        }

        #endregion

        #region Gem Mesh Generation

        private Mesh CreateGemMeshForType(GameColors.GemType type)
        {
            return type switch
            {
                GameColors.GemType.Ruby => CreateOvalCutMesh(16),      // Oval Cut, 16 facets
                GameColors.GemType.Sapphire => CreateRoundBrilliantMesh(24),  // Round Brilliant, 24 facets
                GameColors.GemType.Emerald => CreateEmeraldCutMesh(12),   // Emerald Cut, 12 facets
                GameColors.GemType.Amber => CreateCushionCutMesh(18),     // Cushion Cut, 18 facets
                _ => CreateRoundBrilliantMesh(16)
            };
        }

        /// <summary>
        /// Ruby용 Oval Cut 메시 생성
        /// </summary>
        private Mesh CreateOvalCutMesh(int facets)
        {
            Mesh mesh = new Mesh();

            // 간단한 Oval 형태 (실제로는 더 복잡한 facet 구조 필요)
            int segments = facets;
            float radiusX = 0.5f;
            float radiusY = 0.35f;
            float height = 0.3f;

            Vector3[] vertices = new Vector3[segments * 2 + 2];
            int[] triangles = new int[segments * 12];

            // Top center
            vertices[0] = new Vector3(0, height * 0.5f, 0);
            // Bottom center
            vertices[1] = new Vector3(0, -height * 0.5f, 0);

            // Create oval vertices
            for (int i = 0; i < segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                float x = Mathf.Cos(angle) * radiusX;
                float z = Mathf.Sin(angle) * radiusY;

                vertices[2 + i] = new Vector3(x, 0, z);
                vertices[2 + segments + i] = new Vector3(x, 0, z);
            }

            // Create triangles
            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % segments;
                int baseIndex = i * 12;

                // Top cone
                triangles[baseIndex] = 0;
                triangles[baseIndex + 1] = 2 + next;
                triangles[baseIndex + 2] = 2 + i;

                // Bottom cone
                triangles[baseIndex + 3] = 1;
                triangles[baseIndex + 4] = 2 + i;
                triangles[baseIndex + 5] = 2 + next;
            }

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }

        /// <summary>
        /// Sapphire용 Round Brilliant 메시 생성
        /// </summary>
        private Mesh CreateRoundBrilliantMesh(int facets)
        {
            Mesh mesh = new Mesh();

            int segments = facets;
            float radius = 0.4f;
            float crownHeight = 0.15f;
            float pavilionDepth = 0.25f;

            Vector3[] vertices = new Vector3[segments + 2];
            int[] triangles = new int[segments * 6];

            // Table (top)
            vertices[0] = new Vector3(0, crownHeight, 0);
            // Culet (bottom)
            vertices[1] = new Vector3(0, -pavilionDepth, 0);

            // Girdle vertices
            for (int i = 0; i < segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                vertices[2 + i] = new Vector3(
                    Mathf.Cos(angle) * radius,
                    0,
                    Mathf.Sin(angle) * radius
                );
            }

            // Create triangles
            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % segments;
                int baseIndex = i * 6;

                // Crown
                triangles[baseIndex] = 0;
                triangles[baseIndex + 1] = 2 + next;
                triangles[baseIndex + 2] = 2 + i;

                // Pavilion
                triangles[baseIndex + 3] = 1;
                triangles[baseIndex + 4] = 2 + i;
                triangles[baseIndex + 5] = 2 + next;
            }

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }

        /// <summary>
        /// Emerald용 Emerald Cut 메시 생성 (직사각형 + 모서리 컷)
        /// </summary>
        private Mesh CreateEmeraldCutMesh(int facets)
        {
            Mesh mesh = new Mesh();

            // Emerald cut is rectangular with cut corners
            float width = 0.4f;
            float length = 0.5f;
            float height = 0.25f;
            float cornerCut = 0.1f;

            Vector3[] vertices = new Vector3[]
            {
                // Top face (octagonal)
                new Vector3(-width + cornerCut, height, -length),
                new Vector3(width - cornerCut, height, -length),
                new Vector3(width, height, -length + cornerCut),
                new Vector3(width, height, length - cornerCut),
                new Vector3(width - cornerCut, height, length),
                new Vector3(-width + cornerCut, height, length),
                new Vector3(-width, height, length - cornerCut),
                new Vector3(-width, height, -length + cornerCut),

                // Bottom point
                new Vector3(0, -height, 0)
            };

            int[] triangles = new int[]
            {
                // Top face
                0, 1, 2, 0, 2, 3, 0, 3, 4, 0, 4, 5, 0, 5, 6, 0, 6, 7,

                // Pavilion facets
                8, 0, 7, 8, 7, 6, 8, 6, 5, 8, 5, 4,
                8, 4, 3, 8, 3, 2, 8, 2, 1, 8, 1, 0
            };

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }

        /// <summary>
        /// Amber용 Cushion Cut 메시 생성 (둥근 사각형)
        /// </summary>
        private Mesh CreateCushionCutMesh(int facets)
        {
            Mesh mesh = new Mesh();

            int cornerSegments = facets / 4;
            float size = 0.4f;
            float height = 0.2f;
            float cornerRadius = 0.15f;

            System.Collections.Generic.List<Vector3> verticesList = new System.Collections.Generic.List<Vector3>();
            System.Collections.Generic.List<int> trianglesList = new System.Collections.Generic.List<int>();

            // Top center
            verticesList.Add(new Vector3(0, height, 0));
            // Bottom point
            verticesList.Add(new Vector3(0, -height * 0.8f, 0));

            // Create rounded rectangle outline
            float[] cornerAngles = { Mathf.PI * 0.25f, Mathf.PI * 0.75f, Mathf.PI * 1.25f, Mathf.PI * 1.75f };
            Vector3[] cornerCenters = {
                new Vector3(size - cornerRadius, 0, size - cornerRadius),
                new Vector3(-size + cornerRadius, 0, size - cornerRadius),
                new Vector3(-size + cornerRadius, 0, -size + cornerRadius),
                new Vector3(size - cornerRadius, 0, -size + cornerRadius)
            };

            for (int corner = 0; corner < 4; corner++)
            {
                float startAngle = cornerAngles[corner] - Mathf.PI * 0.25f;
                for (int i = 0; i <= cornerSegments; i++)
                {
                    float angle = startAngle + (Mathf.PI * 0.5f * i / cornerSegments);
                    Vector3 pos = cornerCenters[corner] + new Vector3(
                        Mathf.Cos(angle) * cornerRadius,
                        0,
                        Mathf.Sin(angle) * cornerRadius
                    );
                    verticesList.Add(pos);
                }
            }

            // Create triangles
            int girdelStart = 2;
            int girdelCount = verticesList.Count - 2;

            for (int i = 0; i < girdelCount; i++)
            {
                int current = girdelStart + i;
                int next = girdelStart + (i + 1) % girdelCount;

                // Crown
                trianglesList.Add(0);
                trianglesList.Add(next);
                trianglesList.Add(current);

                // Pavilion
                trianglesList.Add(1);
                trianglesList.Add(current);
                trianglesList.Add(next);
            }

            mesh.vertices = verticesList.ToArray();
            mesh.triangles = trianglesList.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }

        #endregion

        #region Animation

        /// <summary>
        /// 보석 획득 애니메이션 시작
        /// </summary>
        public void PlayAcquisitionAnimation(Transform uiTarget = null)
        {
            if (isAnimating || isCollected) return;

            targetUIPosition = uiTarget;
            StartCoroutine(AcquisitionAnimationRoutine());
        }

        private IEnumerator AcquisitionAnimationRoutine()
        {
            isAnimating = true;

            // Phase 1: Pop (0-0.3s)
            yield return StartCoroutine(Phase1Pop());

            // Phase 2: Float and Shine (0.3-0.8s)
            yield return StartCoroutine(Phase2FloatAndShine());

            // Phase 3: Collect (0.8-1.2s)
            yield return StartCoroutine(Phase3Collect());

            isCollected = true;
            isAnimating = false;

            // Trigger collection event
            OnGemCollected();
        }

        private IEnumerator Phase1Pop()
        {
            float elapsed = 0f;
            Vector3 startScale = originalScale * 0.5f;
            Vector3 targetScale = originalScale * popScale;
            Vector3 startPos = originalPosition;
            Vector3 targetPos = originalPosition + Vector3.up * floatHeight;

            transform.localScale = startScale;

            while (elapsed < PHASE1_DURATION)
            {
                float t = elapsed / PHASE1_DURATION;
                float curveValue = popCurve.Evaluate(t);

                transform.localScale = Vector3.Lerp(startScale, targetScale, curveValue);
                transform.position = Vector3.Lerp(startPos, targetPos, curveValue);
                transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);

                elapsed += Time.deltaTime;
                yield return null;
            }

            transform.localScale = targetScale;
            transform.position = targetPos;
        }

        private IEnumerator Phase2FloatAndShine()
        {
            float elapsed = 0f;
            Vector3 startScale = transform.localScale;
            Vector3 targetScale = originalScale;

            // Burst sparkle particles
            if (sparkleParticles != null)
            {
                sparkleParticles.Emit(sparkleParticleCount);
            }

            while (elapsed < PHASE2_DURATION)
            {
                float t = elapsed / PHASE2_DURATION;
                float curveValue = floatCurve.Evaluate(t);

                transform.localScale = Vector3.Lerp(startScale, targetScale, curveValue);

                // Gentle bob animation
                float bob = Mathf.Sin(elapsed * Mathf.PI * 4f) * 0.02f;
                transform.position = originalPosition + Vector3.up * (floatHeight + bob);

                // Continue rotation
                transform.Rotate(Vector3.up, rotationSpeed * 0.5f * Time.deltaTime);

                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        private IEnumerator Phase3Collect()
        {
            float elapsed = 0f;
            Vector3 startPos = transform.position;
            Vector3 startScale = transform.localScale;
            Vector3 targetScale = Vector3.one * 0.1f;

            Vector3 targetPos = targetUIPosition != null ?
                targetUIPosition.position :
                Camera.main.ViewportToWorldPoint(new Vector3(0.9f, 0.9f, 5f));

            while (elapsed < PHASE3_DURATION)
            {
                float t = elapsed / PHASE3_DURATION;
                float curveValue = collectCurve.Evaluate(t);

                transform.position = Vector3.Lerp(startPos, targetPos, curveValue);
                transform.localScale = Vector3.Lerp(startScale, targetScale, curveValue);

                // Fade effect
                if (gemMaterial != null)
                {
                    Color c = gemMaterial.color;
                    c.a = GEM_TRANSPARENCY * (1f - curveValue * 0.5f);
                    gemMaterial.color = c;
                }

                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        private IEnumerator SparkleRoutine()
        {
            while (!isCollected)
            {
                yield return new WaitForSeconds(sparkleInterval + Random.Range(-0.2f, 0.2f));

                if (sparkleParticles != null && !isAnimating)
                {
                    sparkleParticles.Emit(Random.Range(1, 4));
                }
            }
        }

        #endregion

        #region Events

        private void OnGemCollected()
        {
            // Trigger event or callback
            gameObject.SetActive(false);
        }

        #endregion

        #region Static Helpers

        /// <summary>
        /// 보석 타입별 facet 수 반환
        /// </summary>
        public static int GetFacetCountForType(GameColors.GemType type)
        {
            return type switch
            {
                GameColors.GemType.Ruby => 16,
                GameColors.GemType.Sapphire => 24,
                GameColors.GemType.Emerald => 12,
                GameColors.GemType.Amber => 18,
                _ => 16
            };
        }

        /// <summary>
        /// 보석 타입별 컷 이름 반환
        /// </summary>
        public static string GetCutNameForType(GameColors.GemType type)
        {
            return type switch
            {
                GameColors.GemType.Ruby => "Oval Cut",
                GameColors.GemType.Sapphire => "Round Brilliant",
                GameColors.GemType.Emerald => "Emerald Cut",
                GameColors.GemType.Amber => "Cushion Cut",
                _ => "Unknown"
            };
        }

        /// <summary>
        /// 새 보석 오브젝트 생성
        /// </summary>
        public static GemVisual CreateGem(GameColors.GemType type, Vector3 position)
        {
            GameObject gemObj = new GameObject($"Gem_{type}");
            gemObj.transform.position = position;

            GemVisual visual = gemObj.AddComponent<GemVisual>();
            visual.Initialize(type);

            return visual;
        }

        #endregion

        #region Icon Generation (2D)

        /// <summary>
        /// 보석 아이콘 정보를 담는 구조체
        /// </summary>
        public struct GemIconInfo
        {
            public GameColors.GemType Type;
            public Color MainColor;
            public Color HighlightColor;
            public Color GradientStart;
            public Color GradientEnd;
            public string Description;
        }

        /// <summary>
        /// 보석 타입별 아이콘 정보 반환
        /// </summary>
        public static GemIconInfo GetIconInfo(GameColors.GemType type)
        {
            return type switch
            {
                GameColors.GemType.Ruby => new GemIconInfo
                {
                    Type = type,
                    MainColor = GameColors.Gems.Ruby,
                    HighlightColor = Color.white,
                    GradientStart = new Color(0.9f, 0.2f, 0.3f),
                    GradientEnd = new Color(0.7f, 0.1f, 0.2f),
                    Description = "Red oval with white highlight, warm gradient"
                },
                GameColors.GemType.Sapphire => new GemIconInfo
                {
                    Type = type,
                    MainColor = GameColors.Gems.Sapphire,
                    HighlightColor = Color.white,
                    GradientStart = new Color(0.2f, 0.5f, 0.9f),
                    GradientEnd = new Color(0.1f, 0.3f, 0.7f),
                    Description = "Blue circle with star highlight, cool gradient"
                },
                GameColors.GemType.Emerald => new GemIconInfo
                {
                    Type = type,
                    MainColor = GameColors.Gems.Emerald,
                    HighlightColor = Color.white,
                    GradientStart = new Color(0.2f, 0.6f, 0.3f),
                    GradientEnd = new Color(0.1f, 0.4f, 0.2f),
                    Description = "Green rectangle with corner cuts, natural gradient"
                },
                GameColors.GemType.Amber => new GemIconInfo
                {
                    Type = type,
                    MainColor = GameColors.Gems.Amber,
                    HighlightColor = Color.white,
                    GradientStart = new Color(1f, 0.8f, 0.3f),
                    GradientEnd = new Color(0.9f, 0.6f, 0.1f),
                    Description = "Orange rounded square, warm honey gradient"
                },
                _ => new GemIconInfo
                {
                    Type = type,
                    MainColor = Color.white,
                    HighlightColor = Color.white,
                    GradientStart = Color.white,
                    GradientEnd = Color.gray,
                    Description = "Unknown gem"
                }
            };
        }

        #endregion

        #region Cleanup

        private void OnDestroy()
        {
            if (gemMaterial != null)
            {
                Destroy(gemMaterial);
            }
        }

        #endregion
    }
}
