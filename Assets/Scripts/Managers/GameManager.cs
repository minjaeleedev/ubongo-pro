using UnityEngine;
using System;
using System.Collections;
using Ubongo.Systems;

namespace Ubongo
{
    /// <summary>
    /// 게임 상태 열거형
    /// </summary>
    public enum GameState
    {
        Menu,
        DifficultySelect,
        RoundStarting,
        Playing,
        Paused,
        RoundComplete,
        RoundFailed,
        SecondChance,           // 재도전 라운드
        GameComplete,
        Tiebreaker,             // 타이브레이커 진행 중
        TiebreakerComplete,     // 타이브레이커 완료
        GameOver
    }

    /// <summary>
    /// 게임 모드 열거형
    /// </summary>
    public enum GameMode
    {
        Classic,        // 클래식 9라운드
        TimeAttack,     // 타임 어택
        Zen,            // 젠 모드 (시간 제한 없음)
        Multiplayer     // 멀티플레이어
    }

    /// <summary>
    /// 게임 매니저 - 전체 게임 흐름 및 상태 관리
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        private static GameManager _instance;
        public static GameManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<GameManager>();
                    if (_instance == null)
                    {
                        var go = new GameObject("GameManager");
                        _instance = go.AddComponent<GameManager>();
                    }
                }
                return _instance;
            }
        }

        [Header("Game Settings")]
        [SerializeField] private GameMode currentMode = GameMode.Classic;
        [SerializeField] private bool enableHints = false;

        [Header("References")]
        [SerializeField] private GemSystem gemSystem;
        [SerializeField] private RoundManager roundManager;
        [SerializeField] private DifficultySystem difficultySystem;
        [SerializeField] private TiebreakerManager tiebreakerManager;

        private GameState _currentState = GameState.Menu;
        private int _bonusScore = 0;
        private int _consecutiveClears = 0;

        // Events
        public event Action<GameState> OnGameStateChanged;
        public event Action<int> OnBonusScoreChanged;
        public event Action<GameMode> OnGameModeChanged;
        public event Action OnPuzzleSolved;
        public event Action OnSecondChanceStarted;
        public event Action<TiebreakerResult> OnTiebreakerEnded;

        // Properties
        public GameState CurrentState => _currentState;
        public GameMode CurrentMode => currentMode;
        public bool EnableHints => enableHints;
        public int BonusScore => _bonusScore;
        public int ConsecutiveClears => _consecutiveClears;

        // System References
        public GemSystem GemSystem => gemSystem != null ? gemSystem : GemSystem.Instance;
        public RoundManager RoundManager => roundManager != null ? roundManager : RoundManager.Instance;
        public DifficultySystem DifficultySystem => difficultySystem != null ? difficultySystem : DifficultySystem.Instance;
        public TiebreakerManager TiebreakerManager => tiebreakerManager != null ? tiebreakerManager : TiebreakerManager.Instance;

        // Computed Properties
        public int CurrentRound => RoundManager?.CurrentRound ?? 0;
        public int TotalRounds => RoundManager?.TotalRounds ?? 9;
        public float RemainingTime => RoundManager?.RemainingTime ?? 0f;
        public int TotalGemPoints => GemSystem?.TotalPoints ?? 0;
        public DifficultyLevel CurrentDifficulty => DifficultySystem?.CurrentDifficulty ?? DifficultyLevel.Easy;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeSystemReferences();
        }

        private void Start()
        {
            SubscribeToEvents();
            ChangeState(GameState.Menu);
        }

        private void InitializeSystemReferences()
        {
            if (gemSystem == null)
            {
                gemSystem = FindObjectOfType<GemSystem>();
            }
            if (roundManager == null)
            {
                roundManager = FindObjectOfType<RoundManager>();
            }
            if (difficultySystem == null)
            {
                difficultySystem = FindObjectOfType<DifficultySystem>();
            }
            if (tiebreakerManager == null)
            {
                tiebreakerManager = FindObjectOfType<TiebreakerManager>();
            }
        }

        private void SubscribeToEvents()
        {
            if (RoundManager != null)
            {
                RoundManager.OnRoundStarting += HandleRoundStarting;
                RoundManager.OnRoundStarted += HandleRoundStarted;
                RoundManager.OnRoundCompleted += HandleRoundCompleted;
                RoundManager.OnRoundFailed += HandleRoundFailed;
                RoundManager.OnGameCompleted += HandleGameCompleted;
                RoundManager.OnSecondChanceStarted += HandleSecondChanceStarted;
            }

            if (TiebreakerManager != null)
            {
                TiebreakerManager.OnTiebreakerStarting += HandleTiebreakerStarting;
                TiebreakerManager.OnTiebreakerEnded += HandleTiebreakerEnded;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (RoundManager != null)
            {
                RoundManager.OnRoundStarting -= HandleRoundStarting;
                RoundManager.OnRoundStarted -= HandleRoundStarted;
                RoundManager.OnRoundCompleted -= HandleRoundCompleted;
                RoundManager.OnRoundFailed -= HandleRoundFailed;
                RoundManager.OnGameCompleted -= HandleGameCompleted;
                RoundManager.OnSecondChanceStarted -= HandleSecondChanceStarted;
            }

            if (TiebreakerManager != null)
            {
                TiebreakerManager.OnTiebreakerStarting -= HandleTiebreakerStarting;
                TiebreakerManager.OnTiebreakerEnded -= HandleTiebreakerEnded;
            }
        }

        /// <summary>
        /// 게임 모드 설정
        /// </summary>
        public void SetGameMode(GameMode mode)
        {
            currentMode = mode;
            OnGameModeChanged?.Invoke(currentMode);
        }

        /// <summary>
        /// 난이도 선택 화면으로 이동
        /// </summary>
        public void ShowDifficultySelect()
        {
            ChangeState(GameState.DifficultySelect);
        }

        /// <summary>
        /// 게임 시작 (난이도 선택 후)
        /// </summary>
        public void StartGame(DifficultyLevel difficulty)
        {
            ResetGameState();

            DifficultySystem?.SetDifficulty(difficulty);
            GemSystem?.ResetGemCollection();

            switch (currentMode)
            {
                case GameMode.Classic:
                    StartClassicMode();
                    break;
                case GameMode.TimeAttack:
                    StartTimeAttackMode();
                    break;
                case GameMode.Zen:
                    StartZenMode();
                    break;
                case GameMode.Multiplayer:
                    StartMultiplayerMode();
                    break;
            }
        }

        private void ResetGameState()
        {
            _bonusScore = 0;
            _consecutiveClears = 0;
            OnBonusScoreChanged?.Invoke(_bonusScore);
        }

        private void StartClassicMode()
        {
            RoundManager?.StartNewGame(CurrentDifficulty);
        }

        private void StartTimeAttackMode()
        {
            RoundManager?.StartNewGame(CurrentDifficulty);
        }

        private void StartZenMode()
        {
            enableHints = true;
            RoundManager?.StartNewGame(CurrentDifficulty);
        }

        private void StartMultiplayerMode()
        {
            RoundManager?.StartNewGame(CurrentDifficulty);
        }

        /// <summary>
        /// 퍼즐 해결 완료 처리
        /// </summary>
        public void CompletePuzzle()
        {
            if (_currentState != GameState.Playing) return;

            _consecutiveClears++;
            CalculateBonusScore();

            OnPuzzleSolved?.Invoke();
            RoundManager?.CompleteRound();
        }

        private void CalculateBonusScore()
        {
            float remainingTime = RoundManager?.RemainingTime ?? 0f;
            float scoreMultiplier = DifficultySystem?.GetScoreMultiplier(CurrentDifficulty) ?? 1f;

            int timeBonus = Mathf.RoundToInt(remainingTime * 10 * scoreMultiplier);
            int streakBonus = _consecutiveClears * 50;

            _bonusScore += timeBonus + streakBonus;
            OnBonusScoreChanged?.Invoke(_bonusScore);
        }

        /// <summary>
        /// 게임 일시정지
        /// </summary>
        public void PauseGame()
        {
            if (_currentState == GameState.Playing)
            {
                ChangeState(GameState.Paused);
                RoundManager?.PauseRound();
                Time.timeScale = 0f;
            }
        }

        /// <summary>
        /// 게임 재개
        /// </summary>
        public void ResumeGame()
        {
            if (_currentState == GameState.Paused)
            {
                ChangeState(GameState.Playing);
                RoundManager?.ResumeRound();
                Time.timeScale = 1f;
            }
        }

        /// <summary>
        /// 현재 라운드 재시작
        /// </summary>
        public void RestartRound()
        {
            _consecutiveClears = 0;
            RoundManager?.RestartCurrentRound();
        }

        /// <summary>
        /// 메인 메뉴로 돌아가기
        /// </summary>
        public void ReturnToMenu()
        {
            Time.timeScale = 1f;
            ChangeState(GameState.Menu);
        }

        /// <summary>
        /// 게임 종료
        /// </summary>
        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        /// <summary>
        /// 힌트 활성화/비활성화
        /// </summary>
        public void SetHintsEnabled(bool enabled)
        {
            enableHints = enabled;
        }

        /// <summary>
        /// 게임 결과 요약 조회
        /// </summary>
        public GameResultData GetGameResult()
        {
            var roundSummary = RoundManager?.GetGameResultSummary()
                ?? new GameResultSummary(9, 0, 0f, 0, 'D');
            var gemSummary = GemSystem?.GetCollectionSummary()
                ?? new GemCollectionSummary(0, 0, 0, 0, 0);

            return new GameResultData(
                gameMode: currentMode,
                difficulty: CurrentDifficulty,
                roundSummary: roundSummary,
                gemSummary: gemSummary,
                bonusScore: _bonusScore,
                consecutiveClears: _consecutiveClears
            );
        }

        private void ChangeState(GameState newState)
        {
            if (_currentState == newState) return;

            _currentState = newState;
            OnGameStateChanged?.Invoke(_currentState);
        }

        // Event Handlers
        private void HandleRoundStarting(int round, RoundConfig config)
        {
            ChangeState(GameState.RoundStarting);
        }

        private void HandleRoundStarted(int round)
        {
            ChangeState(GameState.Playing);
        }

        private void HandleRoundCompleted(RoundResult result)
        {
            DifficultySystem?.RecordRoundSuccess();
            ChangeState(GameState.RoundComplete);
        }

        private void HandleRoundFailed(RoundResult result)
        {
            _consecutiveClears = 0;
            DifficultySystem?.RecordRoundFailure();
            ChangeState(GameState.RoundFailed);
        }

        private void HandleGameCompleted(System.Collections.Generic.List<RoundResult> results)
        {
            // 멀티플레이어 모드에서 동점 확인
            if (currentMode == GameMode.Multiplayer)
            {
                CheckForTiebreaker();
            }
            else
            {
                ChangeState(GameState.GameComplete);
            }
        }

        private void HandleSecondChanceStarted()
        {
            ChangeState(GameState.SecondChance);
            OnSecondChanceStarted?.Invoke();
        }

        private void HandleTiebreakerStarting(System.Collections.Generic.List<TiebreakerPlayer> tiedPlayers)
        {
            ChangeState(GameState.Tiebreaker);
        }

        private void HandleTiebreakerEnded(TiebreakerResult result)
        {
            ChangeState(GameState.TiebreakerComplete);
            OnTiebreakerEnded?.Invoke(result);
        }

        /// <summary>
        /// 멀티플레이어 게임 종료 시 동점 확인
        /// </summary>
        private void CheckForTiebreaker()
        {
            // TODO: 실제 멀티플레이어 구현 시 플레이어 결과 수집 필요
            // 현재는 싱글플레이어 전용이므로 바로 게임 완료 처리
            ChangeState(GameState.GameComplete);
        }

        /// <summary>
        /// 멀티플레이어 플레이어 결과로 타이브레이커 확인 및 시작
        /// </summary>
        public void CheckAndStartTiebreaker(System.Collections.Generic.List<(int playerId, string name, int gemPoints, int gemCount)> playerResults)
        {
            if (TiebreakerManager == null)
            {
                ChangeState(GameState.GameComplete);
                return;
            }

            if (TiebreakerManager.CheckForTie(playerResults))
            {
                TiebreakerManager.StartTiebreaker();
            }
            else
            {
                ChangeState(GameState.GameComplete);
            }
        }

        /// <summary>
        /// 타이브레이커 중 퍼즐 완료 처리
        /// </summary>
        public void CompleteTiebreakerPuzzle(int playerId)
        {
            if (_currentState != GameState.Tiebreaker) return;

            TiebreakerManager?.RegisterPlayerCompletion(playerId);
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }
    }

    /// <summary>
    /// 게임 결과 데이터 (불변)
    /// </summary>
    public readonly struct GameResultData
    {
        public GameMode GameMode { get; }
        public DifficultyLevel Difficulty { get; }
        public GameResultSummary RoundSummary { get; }
        public GemCollectionSummary GemSummary { get; }
        public int BonusScore { get; }
        public int ConsecutiveClears { get; }

        public int TotalScore => GemSummary.TotalPoints + BonusScore;

        public GameResultData(
            GameMode gameMode,
            DifficultyLevel difficulty,
            GameResultSummary roundSummary,
            GemCollectionSummary gemSummary,
            int bonusScore,
            int consecutiveClears)
        {
            GameMode = gameMode;
            Difficulty = difficulty;
            RoundSummary = roundSummary;
            GemSummary = gemSummary;
            BonusScore = bonusScore;
            ConsecutiveClears = consecutiveClears;
        }
    }
}
