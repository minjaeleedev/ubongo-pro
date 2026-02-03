using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Ubongo.Systems
{
    /// <summary>
    /// 라운드 상태 열거형
    /// </summary>
    public enum RoundState
    {
        NotStarted,
        Starting,
        InProgress,
        Completed,
        Failed,
        Transitioning
    }

    /// <summary>
    /// 라운드 결과 데이터 (불변)
    /// </summary>
    public readonly struct RoundResult
    {
        public int RoundNumber { get; }
        public bool Completed { get; }
        public float TimeSpent { get; }
        public float TimeLimit { get; }
        public float RemainingTime => TimeLimit - TimeSpent;
        public float RemainingTimeRatio => TimeLimit > 0 ? Mathf.Clamp01(RemainingTime / TimeLimit) : 0f;
        public GemRewardResult GemReward { get; }

        public RoundResult(int roundNumber, bool completed, float timeSpent, float timeLimit, GemRewardResult gemReward)
        {
            RoundNumber = roundNumber;
            Completed = completed;
            TimeSpent = timeSpent;
            TimeLimit = timeLimit;
            GemReward = gemReward;
        }
    }

    /// <summary>
    /// 라운드 설정 데이터 (불변)
    /// </summary>
    public readonly struct RoundConfig
    {
        public int RoundNumber { get; }
        public float TimeLimit { get; }
        public DifficultyLevel Difficulty { get; }
        public int PuzzleId { get; }

        public RoundConfig(int roundNumber, float timeLimit, DifficultyLevel difficulty, int puzzleId)
        {
            RoundNumber = roundNumber;
            TimeLimit = timeLimit;
            Difficulty = difficulty;
            PuzzleId = puzzleId;
        }
    }

    /// <summary>
    /// 9라운드 시스템 관리자
    /// </summary>
    public class RoundManager : MonoBehaviour
    {
        private static RoundManager _instance;
        public static RoundManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<RoundManager>();
                    if (_instance == null)
                    {
                        var go = new GameObject("RoundManager");
                        _instance = go.AddComponent<RoundManager>();
                    }
                }
                return _instance;
            }
        }

        [Header("Round Settings")]
        [SerializeField] private int totalRounds = 9;
        [SerializeField] private float transitionDelay = 2f;

        private int _currentRound;
        private RoundState _currentState;
        private float _roundStartTime;
        private float _currentTimeLimit;
        private DifficultyLevel _currentDifficulty;
        private List<RoundResult> _roundResults;
        private Coroutine _timerCoroutine;

        // Events
        public event Action<int, RoundConfig> OnRoundStarting;
        public event Action<int> OnRoundStarted;
        public event Action<float> OnRoundTimeUpdated;
        public event Action<RoundResult> OnRoundCompleted;
        public event Action<RoundResult> OnRoundFailed;
        public event Action<List<RoundResult>> OnGameCompleted;
        public event Action OnGameReset;

        // Properties
        public int CurrentRound => _currentRound;
        public int TotalRounds => totalRounds;
        public RoundState CurrentState => _currentState;
        public float RemainingTime => _currentTimeLimit - (Time.time - _roundStartTime);
        public float CurrentTimeLimit => _currentTimeLimit;
        public IReadOnlyList<RoundResult> RoundResults => _roundResults.AsReadOnly();
        public bool IsLastRound => _currentRound >= totalRounds;
        public bool IsGameActive => _currentState == RoundState.InProgress;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeRoundManager();
        }

        private void InitializeRoundManager()
        {
            _roundResults = new List<RoundResult>();
            _currentState = RoundState.NotStarted;
            _currentRound = 0;
        }

        /// <summary>
        /// 새 게임 시작 - 라운드 시스템 초기화
        /// </summary>
        public void StartNewGame(DifficultyLevel difficulty)
        {
            _currentDifficulty = difficulty;
            _currentRound = 0;
            _roundResults = new List<RoundResult>();
            _currentState = RoundState.NotStarted;

            OnGameReset?.Invoke();

            StartNextRound();
        }

        /// <summary>
        /// 다음 라운드 시작
        /// </summary>
        public void StartNextRound()
        {
            if (_currentRound >= totalRounds)
            {
                CompleteGame();
                return;
            }

            _currentRound++;
            StartCoroutine(StartRoundSequence());
        }

        private IEnumerator StartRoundSequence()
        {
            _currentState = RoundState.Starting;

            var config = CreateRoundConfig();
            _currentTimeLimit = config.TimeLimit;

            OnRoundStarting?.Invoke(_currentRound, config);

            yield return new WaitForSeconds(0.5f);

            _currentState = RoundState.InProgress;
            _roundStartTime = Time.time;

            OnRoundStarted?.Invoke(_currentRound);

            if (_timerCoroutine != null)
            {
                StopCoroutine(_timerCoroutine);
            }
            _timerCoroutine = StartCoroutine(RoundTimer());
        }

        private RoundConfig CreateRoundConfig()
        {
            var difficultyConfig = DifficultySystem.Instance?.GetDifficultyConfig(_currentDifficulty)
                ?? DifficultyConfig.CreateDefault(_currentDifficulty);

            int puzzleId = GeneratePuzzleId();

            return new RoundConfig(
                roundNumber: _currentRound,
                timeLimit: difficultyConfig.TimeLimit,
                difficulty: _currentDifficulty,
                puzzleId: puzzleId
            );
        }

        private int GeneratePuzzleId()
        {
            return (_currentRound - 1) % 36 + 1;
        }

        private IEnumerator RoundTimer()
        {
            while (_currentState == RoundState.InProgress)
            {
                float remaining = RemainingTime;
                OnRoundTimeUpdated?.Invoke(remaining);

                if (remaining <= 0)
                {
                    FailRound();
                    yield break;
                }

                yield return null;
            }
        }

        /// <summary>
        /// 라운드 성공 처리
        /// </summary>
        public void CompleteRound()
        {
            if (_currentState != RoundState.InProgress) return;

            _currentState = RoundState.Completed;

            if (_timerCoroutine != null)
            {
                StopCoroutine(_timerCoroutine);
                _timerCoroutine = null;
            }

            float timeSpent = Time.time - _roundStartTime;
            float remainingTimeRatio = Mathf.Clamp01((_currentTimeLimit - timeSpent) / _currentTimeLimit);

            var gemReward = GemSystem.Instance?.AwardGemsForSinglePlayer(remainingTimeRatio)
                ?? new GemRewardResult(null, null);

            var result = new RoundResult(
                roundNumber: _currentRound,
                completed: true,
                timeSpent: timeSpent,
                timeLimit: _currentTimeLimit,
                gemReward: gemReward
            );

            _roundResults.Add(result);
            OnRoundCompleted?.Invoke(result);

            StartCoroutine(TransitionToNextRound());
        }

        /// <summary>
        /// 라운드 실패 처리
        /// </summary>
        private void FailRound()
        {
            if (_currentState != RoundState.InProgress) return;

            _currentState = RoundState.Failed;

            if (_timerCoroutine != null)
            {
                StopCoroutine(_timerCoroutine);
                _timerCoroutine = null;
            }

            var result = new RoundResult(
                roundNumber: _currentRound,
                completed: false,
                timeSpent: _currentTimeLimit,
                timeLimit: _currentTimeLimit,
                gemReward: new GemRewardResult(null, null)
            );

            _roundResults.Add(result);
            OnRoundFailed?.Invoke(result);

            StartCoroutine(TransitionToNextRound());
        }

        private IEnumerator TransitionToNextRound()
        {
            _currentState = RoundState.Transitioning;

            yield return new WaitForSeconds(transitionDelay);

            if (_currentRound >= totalRounds)
            {
                CompleteGame();
            }
            else
            {
                StartNextRound();
            }
        }

        private void CompleteGame()
        {
            _currentState = RoundState.NotStarted;
            OnGameCompleted?.Invoke(new List<RoundResult>(_roundResults));
        }

        /// <summary>
        /// 현재 라운드 재시작
        /// </summary>
        public void RestartCurrentRound()
        {
            if (_timerCoroutine != null)
            {
                StopCoroutine(_timerCoroutine);
                _timerCoroutine = null;
            }

            _currentRound--;

            if (_roundResults.Count > 0 && _roundResults[_roundResults.Count - 1].RoundNumber == _currentRound + 1)
            {
                _roundResults.RemoveAt(_roundResults.Count - 1);
            }

            StartNextRound();
        }

        /// <summary>
        /// 게임 일시정지
        /// </summary>
        public void PauseRound()
        {
            if (_currentState == RoundState.InProgress)
            {
                Time.timeScale = 0f;
            }
        }

        /// <summary>
        /// 게임 재개
        /// </summary>
        public void ResumeRound()
        {
            if (_currentState == RoundState.InProgress)
            {
                Time.timeScale = 1f;
            }
        }

        /// <summary>
        /// 게임 결과 요약 반환
        /// </summary>
        public GameResultSummary GetGameResultSummary()
        {
            int completedRounds = 0;
            float totalTimeSpent = 0f;
            int totalGemPoints = 0;

            foreach (var result in _roundResults)
            {
                if (result.Completed)
                {
                    completedRounds++;
                }
                totalTimeSpent += result.TimeSpent;
                totalGemPoints += result.GemReward.TotalPoints;
            }

            return new GameResultSummary(
                totalRounds: totalRounds,
                completedRounds: completedRounds,
                totalTimeSpent: totalTimeSpent,
                totalGemPoints: totalGemPoints,
                grade: GemSystem.Instance?.CalculateGrade() ?? 'D'
            );
        }

        private void OnDestroy()
        {
            if (_timerCoroutine != null)
            {
                StopCoroutine(_timerCoroutine);
            }
        }
    }

    /// <summary>
    /// 게임 결과 요약 (불변)
    /// </summary>
    public readonly struct GameResultSummary
    {
        public int TotalRounds { get; }
        public int CompletedRounds { get; }
        public float TotalTimeSpent { get; }
        public int TotalGemPoints { get; }
        public char Grade { get; }
        public float CompletionRate => TotalRounds > 0 ? (float)CompletedRounds / TotalRounds : 0f;

        public GameResultSummary(int totalRounds, int completedRounds, float totalTimeSpent, int totalGemPoints, char grade)
        {
            TotalRounds = totalRounds;
            CompletedRounds = completedRounds;
            TotalTimeSpent = totalTimeSpent;
            TotalGemPoints = totalGemPoints;
            Grade = grade;
        }
    }
}
