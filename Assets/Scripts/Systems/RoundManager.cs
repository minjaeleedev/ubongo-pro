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
        Transitioning,
        SecondChance,           // 재도전 라운드 (모든 플레이어 실패 시)
        SecondChanceInProgress, // 재도전 진행 중
        SecondChanceFailed      // 재도전도 실패
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
                    _instance = FindAnyObjectByType<RoundManager>();
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
        [SerializeField] private bool enableSecondChance = true;

        private int _currentRound;
        private RoundState _currentState;
        private float _roundStartTime;
        private float _currentTimeLimit;
        private DifficultyLevel _currentDifficulty;
        private List<RoundResult> _roundResults;
        private Coroutine _timerCoroutine;
        private bool _isSecondChanceRound;
        private int _playersCompletedCount;
        private int _totalPlayersInRound;

        // Events
        public event Action<int, RoundConfig> OnRoundStarting;
        public event Action<int> OnRoundStarted;
        public event Action<float> OnRoundTimeUpdated;
        public event Action<RoundResult> OnRoundCompleted;
        public event Action<RoundResult> OnRoundFailed;
        public event Action<List<RoundResult>> OnGameCompleted;
        public event Action OnGameReset;
        public event Action OnSecondChanceStarted;
        public event Action<RoundResult> OnSecondChanceCompleted;
        public event Action OnSecondChanceFailed;

        // Properties
        public int CurrentRound => _currentRound;
        public int TotalRounds => totalRounds;
        public RoundState CurrentState => _currentState;
        public float RemainingTime => _currentTimeLimit - (Time.time - _roundStartTime);
        public float CurrentTimeLimit => _currentTimeLimit;
        public IReadOnlyList<RoundResult> RoundResults => _roundResults.AsReadOnly();
        public bool IsLastRound => _currentRound >= totalRounds;
        public bool IsGameActive => _currentState == RoundState.InProgress || _currentState == RoundState.SecondChanceInProgress;
        public bool IsSecondChanceRound => _isSecondChanceRound;
        public bool EnableSecondChance => enableSecondChance;

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
            _isSecondChanceRound = false;
            _playersCompletedCount = 0;
            _totalPlayersInRound = 1; // 싱글플레이어 기본값
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
            _isSecondChanceRound = false;
            _playersCompletedCount = 0;

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
            while (_currentState == RoundState.InProgress || _currentState == RoundState.SecondChanceInProgress)
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
            // Second Chance 라운드 중인 경우 별도 처리
            if (_currentState == RoundState.SecondChanceInProgress)
            {
                CompleteSecondChanceRound();
                return;
            }

            if (_currentState != RoundState.InProgress) return;

            _currentState = RoundState.Completed;
            _isSecondChanceRound = false;

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
            if (_currentState != RoundState.InProgress && _currentState != RoundState.SecondChanceInProgress) return;

            if (_timerCoroutine != null)
            {
                StopCoroutine(_timerCoroutine);
                _timerCoroutine = null;
            }

            // Second Chance 라운드 중 실패
            if (_currentState == RoundState.SecondChanceInProgress)
            {
                HandleSecondChanceFailed();
                return;
            }

            // 일반 라운드 실패 - Second Chance 가능 여부 확인
            // 멀티플레이어에서 모든 플레이어가 실패한 경우에만 Second Chance 발동
            if (enableSecondChance && ShouldTriggerSecondChance())
            {
                StartSecondChanceRound();
                return;
            }

            _currentState = RoundState.Failed;

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

        /// <summary>
        /// Second Chance 발동 조건 확인
        /// 멀티플레이어: 모든 플레이어가 실패
        /// 싱글플레이어: 적용 안함 (false 반환)
        /// </summary>
        private bool ShouldTriggerSecondChance()
        {
            // 이미 Second Chance를 사용한 경우
            if (_isSecondChanceRound) return false;

            // 싱글플레이어는 Second Chance 미적용
            if (_totalPlayersInRound <= 1) return false;

            // 멀티플레이어: 누구도 완료하지 못한 경우에만 발동
            return _playersCompletedCount == 0;
        }

        /// <summary>
        /// Second Chance 라운드 시작
        /// </summary>
        private void StartSecondChanceRound()
        {
            _currentState = RoundState.SecondChance;
            _isSecondChanceRound = true;

            OnSecondChanceStarted?.Invoke();

            StartCoroutine(StartSecondChanceSequence());
        }

        private IEnumerator StartSecondChanceSequence()
        {
            // 짧은 대기 후 재시작
            yield return new WaitForSeconds(1f);

            _currentState = RoundState.SecondChanceInProgress;
            _roundStartTime = Time.time;
            _playersCompletedCount = 0;

            // 타이머 재시작
            if (_timerCoroutine != null)
            {
                StopCoroutine(_timerCoroutine);
            }
            _timerCoroutine = StartCoroutine(RoundTimer());
        }

        /// <summary>
        /// Second Chance 라운드 완료 처리
        /// 첫 완성자만 랜덤 보석 1개 획득
        /// </summary>
        public void CompleteSecondChanceRound()
        {
            if (_currentState != RoundState.SecondChanceInProgress) return;

            _currentState = RoundState.Completed;
            _isSecondChanceRound = false;

            if (_timerCoroutine != null)
            {
                StopCoroutine(_timerCoroutine);
                _timerCoroutine = null;
            }

            float timeSpent = Time.time - _roundStartTime;

            // Second Chance: 랜덤 보석 1개만 지급 (고정 보석 없음)
            var gemReward = GemSystem.Instance?.AwardRandomGemOnly()
                ?? new GemRewardResult(null, null);

            var result = new RoundResult(
                roundNumber: _currentRound,
                completed: true,
                timeSpent: timeSpent,
                timeLimit: _currentTimeLimit,
                gemReward: gemReward
            );

            _roundResults.Add(result);
            OnSecondChanceCompleted?.Invoke(result);

            StartCoroutine(TransitionToNextRound());
        }

        /// <summary>
        /// Second Chance 라운드도 실패한 경우
        /// </summary>
        private void HandleSecondChanceFailed()
        {
            _currentState = RoundState.SecondChanceFailed;
            _isSecondChanceRound = false;

            OnSecondChanceFailed?.Invoke();

            var result = new RoundResult(
                roundNumber: _currentRound,
                completed: false,
                timeSpent: _currentTimeLimit * 2, // 총 2번의 시간 사용
                timeLimit: _currentTimeLimit,
                gemReward: new GemRewardResult(null, null)
            );

            _roundResults.Add(result);
            OnRoundFailed?.Invoke(result);

            StartCoroutine(TransitionToNextRound());
        }

        /// <summary>
        /// 멀티플레이어 플레이어 수 설정
        /// </summary>
        public void SetTotalPlayers(int count)
        {
            _totalPlayersInRound = Mathf.Max(1, count);
        }

        /// <summary>
        /// 플레이어 완료 등록 (멀티플레이어용)
        /// </summary>
        public void RegisterPlayerCompletion()
        {
            _playersCompletedCount++;
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
