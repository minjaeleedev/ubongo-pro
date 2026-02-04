using UnityEngine;
using System;
using System.Collections.Generic;

namespace Ubongo.Systems
{
    /// <summary>
    /// 난이도 레벨 열거형
    /// </summary>
    public enum DifficultyLevel
    {
        Easy = 1,       // 초록 - 입문자용
        Medium = 2,     // 노랑 - 기본 난이도
        Hard = 3,       // 파랑 - 숙련자용
        Expert = 4      // 빨강 - 전문가용
    }

    /// <summary>
    /// 난이도별 설정 데이터 (불변)
    /// </summary>
    public readonly struct DifficultyConfig
    {
        public DifficultyLevel Level { get; }
        public string DisplayName { get; }
        public Color DisplayColor { get; }
        public int PieceCount { get; }
        public Vector2Int BoardSize { get; }
        public float TimeLimit { get; }
        public int SolutionCount { get; }
        public float ScoreMultiplier { get; }

        public DifficultyConfig(
            DifficultyLevel level,
            string displayName,
            Color displayColor,
            int pieceCount,
            Vector2Int boardSize,
            float timeLimit,
            int solutionCount,
            float scoreMultiplier)
        {
            Level = level;
            DisplayName = displayName;
            DisplayColor = displayColor;
            PieceCount = pieceCount;
            BoardSize = boardSize;
            TimeLimit = timeLimit;
            SolutionCount = solutionCount;
            ScoreMultiplier = scoreMultiplier;
        }

        public static DifficultyConfig CreateDefault(DifficultyLevel level)
        {
            return level switch
            {
                DifficultyLevel.Easy => new DifficultyConfig(
                    level: DifficultyLevel.Easy,
                    displayName: "Easy",
                    displayColor: new Color(0.2f, 0.8f, 0.2f),  // 초록
                    pieceCount: 3,
                    boardSize: new Vector2Int(3, 3),
                    timeLimit: 90f,
                    solutionCount: 4,
                    scoreMultiplier: 1.0f
                ),
                DifficultyLevel.Medium => new DifficultyConfig(
                    level: DifficultyLevel.Medium,
                    displayName: "Medium",
                    displayColor: new Color(1f, 0.9f, 0.2f),    // 노랑
                    pieceCount: 4,
                    boardSize: new Vector2Int(4, 3),
                    timeLimit: 75f,
                    solutionCount: 3,
                    scoreMultiplier: 1.5f
                ),
                DifficultyLevel.Hard => new DifficultyConfig(
                    level: DifficultyLevel.Hard,
                    displayName: "Hard",
                    displayColor: new Color(0.2f, 0.4f, 0.9f),  // 파랑
                    pieceCount: 5,
                    boardSize: new Vector2Int(4, 4),
                    timeLimit: 60f,
                    solutionCount: 2,
                    scoreMultiplier: 2.0f
                ),
                DifficultyLevel.Expert => new DifficultyConfig(
                    level: DifficultyLevel.Expert,
                    displayName: "Expert",
                    displayColor: new Color(0.9f, 0.2f, 0.2f),  // 빨강
                    pieceCount: 6,
                    boardSize: new Vector2Int(5, 4),
                    timeLimit: 45f,
                    solutionCount: 1,
                    scoreMultiplier: 2.5f
                ),
                _ => CreateDefault(DifficultyLevel.Easy)
            };
        }
    }

    /// <summary>
    /// 적응형 난이도 상태 (불변)
    /// </summary>
    public readonly struct AdaptiveDifficultyState
    {
        public int ConsecutiveSuccesses { get; }
        public int ConsecutiveFailures { get; }
        public DifficultyLevel RecommendedLevel { get; }
        public bool ShouldIncreaseDifficulty => ConsecutiveSuccesses >= 3;
        public bool ShouldDecreaseDifficulty => ConsecutiveFailures >= 2;

        public AdaptiveDifficultyState(int consecutiveSuccesses, int consecutiveFailures, DifficultyLevel recommendedLevel)
        {
            ConsecutiveSuccesses = consecutiveSuccesses;
            ConsecutiveFailures = consecutiveFailures;
            RecommendedLevel = recommendedLevel;
        }

        public AdaptiveDifficultyState RecordSuccess()
        {
            int newSuccesses = ConsecutiveSuccesses + 1;
            DifficultyLevel newRecommended = RecommendedLevel;

            if (newSuccesses >= 3 && (int)RecommendedLevel < (int)DifficultyLevel.Expert)
            {
                newRecommended = (DifficultyLevel)((int)RecommendedLevel + 1);
                newSuccesses = 0;
            }

            return new AdaptiveDifficultyState(newSuccesses, 0, newRecommended);
        }

        public AdaptiveDifficultyState RecordFailure()
        {
            int newFailures = ConsecutiveFailures + 1;
            DifficultyLevel newRecommended = RecommendedLevel;

            if (newFailures >= 2 && (int)RecommendedLevel > (int)DifficultyLevel.Easy)
            {
                newRecommended = (DifficultyLevel)((int)RecommendedLevel - 1);
                newFailures = 0;
            }

            return new AdaptiveDifficultyState(0, newFailures, newRecommended);
        }
    }

    /// <summary>
    /// 난이도 시스템 관리자
    /// </summary>
    public class DifficultySystem : MonoBehaviour
    {
        private static DifficultySystem _instance;
        public static DifficultySystem Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<DifficultySystem>();
                    if (_instance == null)
                    {
                        var go = new GameObject("DifficultySystem");
                        _instance = go.AddComponent<DifficultySystem>();
                    }
                }
                return _instance;
            }
        }

        [Header("Difficulty Settings")]
        [SerializeField] private bool enableAdaptiveDifficulty = false;

        private DifficultyLevel _currentDifficulty;
        private AdaptiveDifficultyState _adaptiveState;
        private Dictionary<DifficultyLevel, DifficultyConfig> _configCache;

        // Events
        public event Action<DifficultyLevel> OnDifficultyChanged;
        public event Action<AdaptiveDifficultyState> OnAdaptiveStateChanged;

        // Properties
        public DifficultyLevel CurrentDifficulty => _currentDifficulty;
        public DifficultyConfig CurrentConfig => GetDifficultyConfig(_currentDifficulty);
        public bool IsAdaptiveDifficultyEnabled => enableAdaptiveDifficulty;
        public AdaptiveDifficultyState AdaptiveState => _adaptiveState;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeDifficultySystem();
        }

        private void InitializeDifficultySystem()
        {
            _configCache = new Dictionary<DifficultyLevel, DifficultyConfig>();

            foreach (DifficultyLevel level in Enum.GetValues(typeof(DifficultyLevel)))
            {
                _configCache[level] = DifficultyConfig.CreateDefault(level);
            }

            _currentDifficulty = DifficultyLevel.Easy;
            _adaptiveState = new AdaptiveDifficultyState(0, 0, DifficultyLevel.Easy);
        }

        /// <summary>
        /// 난이도 설정
        /// </summary>
        public void SetDifficulty(DifficultyLevel level)
        {
            if (_currentDifficulty == level) return;

            _currentDifficulty = level;
            OnDifficultyChanged?.Invoke(_currentDifficulty);
        }

        /// <summary>
        /// 난이도 설정 조회
        /// </summary>
        public DifficultyConfig GetDifficultyConfig(DifficultyLevel level)
        {
            if (_configCache.TryGetValue(level, out var config))
            {
                return config;
            }
            return DifficultyConfig.CreateDefault(level);
        }

        /// <summary>
        /// 모든 난이도 설정 조회
        /// </summary>
        public IReadOnlyDictionary<DifficultyLevel, DifficultyConfig> GetAllDifficultyConfigs()
        {
            return _configCache;
        }

        /// <summary>
        /// 적응형 난이도 활성화/비활성화
        /// </summary>
        public void SetAdaptiveDifficulty(bool enabled)
        {
            enableAdaptiveDifficulty = enabled;
        }

        /// <summary>
        /// 라운드 성공 기록 (적응형 난이도용)
        /// </summary>
        public void RecordRoundSuccess()
        {
            if (!enableAdaptiveDifficulty) return;

            _adaptiveState = _adaptiveState.RecordSuccess();
            OnAdaptiveStateChanged?.Invoke(_adaptiveState);

            if (_adaptiveState.RecommendedLevel != _currentDifficulty)
            {
                SetDifficulty(_adaptiveState.RecommendedLevel);
            }
        }

        /// <summary>
        /// 라운드 실패 기록 (적응형 난이도용)
        /// </summary>
        public void RecordRoundFailure()
        {
            if (!enableAdaptiveDifficulty) return;

            _adaptiveState = _adaptiveState.RecordFailure();
            OnAdaptiveStateChanged?.Invoke(_adaptiveState);

            if (_adaptiveState.RecommendedLevel != _currentDifficulty)
            {
                SetDifficulty(_adaptiveState.RecommendedLevel);
            }
        }

        /// <summary>
        /// 적응형 난이도 상태 초기화
        /// </summary>
        public void ResetAdaptiveState()
        {
            _adaptiveState = new AdaptiveDifficultyState(0, 0, _currentDifficulty);
            OnAdaptiveStateChanged?.Invoke(_adaptiveState);
        }

        /// <summary>
        /// 난이도별 시간 제한 조회
        /// </summary>
        public float GetTimeLimit(DifficultyLevel level)
        {
            return GetDifficultyConfig(level).TimeLimit;
        }

        /// <summary>
        /// 난이도별 조각 수 조회
        /// </summary>
        public int GetPieceCount(DifficultyLevel level)
        {
            return GetDifficultyConfig(level).PieceCount;
        }

        /// <summary>
        /// 난이도별 보드 크기 조회
        /// </summary>
        public Vector2Int GetBoardSize(DifficultyLevel level)
        {
            return GetDifficultyConfig(level).BoardSize;
        }

        /// <summary>
        /// 난이도별 점수 배율 조회
        /// </summary>
        public float GetScoreMultiplier(DifficultyLevel level)
        {
            return GetDifficultyConfig(level).ScoreMultiplier;
        }

        /// <summary>
        /// 난이도 레벨을 정수로 변환
        /// </summary>
        public static int ToInt(DifficultyLevel level)
        {
            return (int)level;
        }

        /// <summary>
        /// 정수를 난이도 레벨로 변환
        /// </summary>
        public static DifficultyLevel FromInt(int value)
        {
            if (value < 1) return DifficultyLevel.Easy;
            if (value > 4) return DifficultyLevel.Expert;
            return (DifficultyLevel)value;
        }

        /// <summary>
        /// 난이도 표시 색상 조회
        /// </summary>
        public Color GetDifficultyColor(DifficultyLevel level)
        {
            return GetDifficultyConfig(level).DisplayColor;
        }

        /// <summary>
        /// 난이도 표시 이름 조회
        /// </summary>
        public string GetDifficultyDisplayName(DifficultyLevel level)
        {
            return GetDifficultyConfig(level).DisplayName;
        }
    }
}
