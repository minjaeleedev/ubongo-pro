using UnityEngine;
using System;
using System.Collections;
using Ubongo.Systems;
using Ubongo.Core;
using Ubongo.Infrastructure.Settings;
using DifficultyLevelSystem = Ubongo.Systems.DifficultyLevel;

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
        private static readonly Quaternion IsometricRotation = Quaternion.Euler(35.264f, 45f, 0f);
        private const string RevealSolutionOptionKey = "RevealSolutionOnTimeout";

        private static GameManager _instance;
        public static GameManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<GameManager>();
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
        [SerializeField] private bool autoStartInEditor = false;
        [SerializeField] private bool showSolutionOnTimeout = false;
        [SerializeField, Min(1f)] private float solutionRevealSeconds = 5f;

        [Header("Solution Preview")]
        [SerializeField] private Material solutionPreviewMaterial;
        [SerializeField] private float solutionBlockHeight = 0.45f;
        [SerializeField] private float solutionLayerStepY = 0.6f;
        [SerializeField] private float solutionBlockScaleRatio = 0.88f;

        [Header("Camera Settings")]
        [SerializeField] private float defaultCameraDistance = 12f;
        [SerializeField] private float minOrthographicSize = 5f;
        [SerializeField] private float maxOrthographicSize = 16f;
        [SerializeField] private float viewPadding = 1.25f;

        [Header("References")]
        [SerializeField] private GemSystem gemSystem;
        [SerializeField] private RoundManager roundManager;
        [SerializeField] private DifficultySystem difficultySystem;
        [SerializeField] private TiebreakerManager tiebreakerManager;
        [SerializeField] private LevelGenerator levelGenerator;
        [SerializeField] private GameBoard gameBoard;

        private GameState _currentState = GameState.Menu;
        private int _bonusScore = 0;
        private int _consecutiveClears = 0;
        private GameObject solutionPreviewContainer;
        private Coroutine solutionPreviewCoroutine;
        private ISettingsStore settingsStore;

        // Events
        public event Action<GameState> OnGameStateChanged;
        public event Action<int> OnBonusScoreChanged;
        public event Action<GameMode> OnGameModeChanged;
        public event Action OnPuzzleSolved;
        public event Action OnSecondChanceStarted;
        public event Action<TiebreakerResult> OnTiebreakerEnded;

        // Legacy UI compatibility events
        public event Action<int> OnScoreChanged;
        public event Action<float> OnTimeChanged;
        public event Action OnLevelComplete;

        // Properties
        public GameState CurrentState => _currentState;
        public GameMode CurrentMode => currentMode;
        public bool EnableHints => enableHints;
        public int BonusScore => _bonusScore;
        public int ConsecutiveClears => _consecutiveClears;

        // Legacy UI compatibility properties
        public int Score => TotalGemPoints + _bonusScore;
        public int CurrentLevel => CurrentRound;

        // System References (단일 인스턴스 참조만 사용)
        public GemSystem GemSystem => gemSystem;
        public RoundManager RoundManager => roundManager;
        public DifficultySystem DifficultySystem => difficultySystem;
        public TiebreakerManager TiebreakerManager => tiebreakerManager;
        public LevelGenerator LevelGenerator => levelGenerator;
        public GameBoard GameBoard => gameBoard;

        // Computed Properties
        public int CurrentRound => RoundManager?.CurrentRound ?? 0;
        public int TotalRounds => RoundManager?.TotalRounds ?? 9;
        public float RemainingTime => RoundManager?.RemainingTime ?? 0f;
        public int TotalGemPoints => GemSystem?.TotalPoints ?? 0;
        public DifficultyLevelSystem CurrentDifficulty => DifficultySystem?.CurrentDifficulty ?? DifficultyLevelSystem.Easy;

        public void Initialize(ISettingsStore injectedSettingsStore)
        {
            if (injectedSettingsStore == null)
            {
                return;
            }

            settingsStore = injectedSettingsStore;
            LoadSolutionRevealOption();
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            EnsureSettingsStore();
            LoadSolutionRevealOption();
            InitializeSystemReferences();
        }

        private void Start()
        {
            SetupCamera();
            SubscribeToEvents();
#if UNITY_EDITOR
            if (autoStartInEditor)
            {
                StartGame(DifficultyLevelSystem.Easy);
            }
#endif
        }

        private void SetupCamera()
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            cam.transform.rotation = IsometricRotation;
            cam.transform.position = new Vector3(-8f, 8f, -8f);
            cam.orthographic = true;
            cam.orthographicSize = minOrthographicSize;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 200f;
            cam.backgroundColor = new Color(0.1f, 0.1f, 0.15f);
            cam.clearFlags = CameraClearFlags.SolidColor;
        }

        private void InitializeSystemReferences()
        {
            // 항상 싱글톤 Instance를 사용하여 일관성 보장
            // Inspector 할당값보다 Instance를 우선하여 이벤트 구독 불일치 방지
            gemSystem = GemSystem.Instance;
            roundManager = RoundManager.Instance;
            difficultySystem = DifficultySystem.Instance;
            tiebreakerManager = TiebreakerManager.Instance;

            // LevelGenerator와 GameBoard는 씬에서 찾기
            levelGenerator = FindAnyObjectByType<LevelGenerator>();
            gameBoard = FindAnyObjectByType<GameBoard>();

            // GameBoard가 없으면 동적 생성
            if (gameBoard == null)
            {
                var boardObject = new GameObject("GameBoard");
                gameBoard = boardObject.AddComponent<GameBoard>();
            }

            // InputManager가 없으면 동적 생성
            if (InputManager.Instance == null)
            {
                var inputManagerObject = new GameObject("InputManager");
                inputManagerObject.AddComponent<InputManager>();
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
                RoundManager.OnRoundTimeUpdated += HandleTimeUpdated;
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
                RoundManager.OnRoundTimeUpdated -= HandleTimeUpdated;
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
        public void StartGame(DifficultyLevelSystem difficulty)
        {
            // 이벤트 구독이 올바른 인스턴스에 되어있는지 확인
            EnsureEventSubscriptions();

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

        /// <summary>
        /// 게임 시작 (기본 난이도 Easy)
        /// </summary>
        public void StartGame()
        {
            StartGame(DifficultyLevelSystem.Easy);
        }

        private void EnsureEventSubscriptions()
        {
            // 기존 구독 해제
            UnsubscribeFromEvents();

            // 참조 갱신 (싱글톤 인스턴스가 늦게 생성된 경우 대비)
            if (roundManager == null) roundManager = RoundManager.Instance;
            if (gemSystem == null) gemSystem = GemSystem.Instance;
            if (difficultySystem == null) difficultySystem = DifficultySystem.Instance;
            if (tiebreakerManager == null) tiebreakerManager = TiebreakerManager.Instance;

            // 재구독
            SubscribeToEvents();
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
        /// 현재 레벨 재시작 (RestartRound의 별칭)
        /// </summary>
        public void RestartLevel()
        {
            RestartRound();
        }

        /// <summary>
        /// 다음 레벨로 진행
        /// </summary>
        public void NextLevel()
        {
            RoundManager?.StartNextRound();
        }

        /// <summary>
        /// 레벨 완료 처리 (CompletePuzzle의 별칭)
        /// </summary>
        public void CompleteLevel()
        {
            CompletePuzzle();
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
            OnScoreChanged?.Invoke(Score);
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
            UnityEngine.Application.Quit();
#endif
        }

        /// <summary>
        /// 힌트 활성화/비활성화
        /// </summary>
        public void SetHintsEnabled(bool enabled)
        {
            enableHints = enabled;
        }

        public void SetShowSolutionOnTimeout(bool enabled)
        {
            showSolutionOnTimeout = enabled;
            ISettingsStore store = EnsureSettingsStore();
            store.SetBool(RevealSolutionOptionKey, enabled);
            store.Save();
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
            ClearSolutionPreview();

            // 퍼즐 생성
            if (levelGenerator != null)
            {
                var levelData = levelGenerator.GenerateLevelData(round);

                if (gameBoard != null)
                {
                    gameBoard.InitializeGrid(levelData.BoardSize);
                    gameBoard.SetTargetArea(levelData.TargetArea);
                }

                ConfigureGameplayView();
                levelGenerator.SpawnFromLevelData(levelData);
                ConfigureGameplayView();
            }

            ChangeState(GameState.Playing);
        }

        private void ConfigureGameplayView()
        {
            Camera cam = Camera.main;
            if (cam == null || gameBoard == null)
            {
                return;
            }

            Bounds focusBounds = gameBoard.GetWorldBounds();
            if (levelGenerator != null && levelGenerator.TryGetSpawnedPiecesBounds(out Bounds pieceBounds))
            {
                focusBounds.Encapsulate(pieceBounds);
            }
            Vector3 focusCenter = focusBounds.center;
            Vector3 cameraOffset = new Vector3(-1f, 1f, -1f).normalized * defaultCameraDistance;

            cam.transform.rotation = IsometricRotation;
            cam.transform.position = focusCenter + cameraOffset;
            cam.transform.LookAt(focusCenter);
            cam.orthographic = true;

            float targetSize = CalculateOrthographicSizeFromBounds(
                focusBounds,
                cam.transform.rotation,
                cam.aspect,
                viewPadding
            );

            cam.orthographicSize = Mathf.Clamp(targetSize, minOrthographicSize, maxOrthographicSize);
        }

        public static float CalculateOrthographicSizeFromBounds(
            Bounds bounds,
            Quaternion cameraRotation,
            float aspect,
            float padding)
        {
            Vector3[] corners = GetBoundsCorners(bounds);
            float minX = float.MaxValue;
            float maxX = float.MinValue;
            float minY = float.MaxValue;
            float maxY = float.MinValue;
            Quaternion inverseRotation = Quaternion.Inverse(cameraRotation);

            foreach (Vector3 corner in corners)
            {
                Vector3 viewSpace = inverseRotation * corner;
                minX = Mathf.Min(minX, viewSpace.x);
                maxX = Mathf.Max(maxX, viewSpace.x);
                minY = Mathf.Min(minY, viewSpace.y);
                maxY = Mathf.Max(maxY, viewSpace.y);
            }

            float verticalHalfSize = (maxY - minY) * 0.5f + padding;
            float horizontalHalfSize = ((maxX - minX) * 0.5f / Mathf.Max(0.01f, aspect)) + padding;

            return Mathf.Max(verticalHalfSize, horizontalHalfSize);
        }

        private static Vector3[] GetBoundsCorners(Bounds bounds)
        {
            Vector3 center = bounds.center;
            Vector3 extents = bounds.extents;

            return new[]
            {
                center + new Vector3(-extents.x, -extents.y, -extents.z),
                center + new Vector3(-extents.x, -extents.y, extents.z),
                center + new Vector3(-extents.x, extents.y, -extents.z),
                center + new Vector3(-extents.x, extents.y, extents.z),
                center + new Vector3(extents.x, -extents.y, -extents.z),
                center + new Vector3(extents.x, -extents.y, extents.z),
                center + new Vector3(extents.x, extents.y, -extents.z),
                center + new Vector3(extents.x, extents.y, extents.z)
            };
        }

        private void HandleRoundCompleted(RoundResult result)
        {
            DifficultySystem?.RecordRoundSuccess();
            ChangeState(GameState.RoundComplete);
            OnLevelComplete?.Invoke();
        }

        private void HandleTimeUpdated(float remainingTime)
        {
            OnTimeChanged?.Invoke(remainingTime);
        }

        private void HandleRoundFailed(RoundResult result)
        {
            _consecutiveClears = 0;
            DifficultySystem?.RecordRoundFailure();
            ChangeState(GameState.RoundFailed);

            TryShowSolutionOnTimeout(result);
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
            ClearSolutionPreview();
        }

        private void LoadSolutionRevealOption()
        {
            ISettingsStore store = EnsureSettingsStore();
            showSolutionOnTimeout = store.GetBool(RevealSolutionOptionKey, showSolutionOnTimeout);
        }

        private ISettingsStore EnsureSettingsStore()
        {
            if (settingsStore != null)
            {
                return settingsStore;
            }

            settingsStore = new PlayerPrefsSettingsStore();
            return settingsStore;
        }

        private void TryShowSolutionOnTimeout(RoundResult result)
        {
            if (!showSolutionOnTimeout || levelGenerator == null || gameBoard == null)
            {
                return;
            }

            if (result.Completed || result.TimeSpent < (result.TimeLimit - 0.01f))
            {
                return;
            }

            LevelData levelData = levelGenerator.CurrentLevelData;
            if (levelData == null || levelData.Pieces == null || levelData.SolutionPlacements == null || levelData.SolutionPlacements.Count == 0)
            {
                return;
            }

            BuildSolutionPreview(levelData);
            RoundManager?.SetNextTransitionDelayOverride(solutionRevealSeconds);

            if (solutionPreviewCoroutine != null)
            {
                StopCoroutine(solutionPreviewCoroutine);
            }
            solutionPreviewCoroutine = StartCoroutine(ClearSolutionPreviewAfterDelay(solutionRevealSeconds));
        }

        private void BuildSolutionPreview(LevelData levelData)
        {
            ClearSolutionPreview();
            solutionPreviewContainer = new GameObject("SolutionPreview");
            solutionPreviewContainer.transform.SetParent(gameBoard.transform, false);

            float blockScaleXZ = Mathf.Max(0.1f, gameBoard.CellSize * solutionBlockScaleRatio);
            float blockHalfHeight = solutionBlockHeight * 0.5f;

            for (int i = 0; i < levelData.SolutionPlacements.Count; i++)
            {
                SolutionPlacement placement = levelData.SolutionPlacements[i];
                if (placement.PieceIndex < 0 || placement.PieceIndex >= levelData.Pieces.Count)
                {
                    continue;
                }

                PieceDefinition piece = levelData.Pieces[placement.PieceIndex];
                Vector3Int[] rotatedBlocks = RotationUtil.RotatePiece(piece.Blocks, placement.RotationIndex);

                foreach (Vector3Int block in rotatedBlocks)
                {
                    Vector3Int cell = placement.Position + block;
                    Vector3 world = gameBoard.GridToWorld(cell.x, 0, cell.z);
                    world.y = blockHalfHeight + (cell.y * solutionLayerStepY);

                    GameObject ghostBlock = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    ghostBlock.name = $"Solution_{piece.Name}_{cell.x}_{cell.y}_{cell.z}";
                    ghostBlock.transform.SetParent(solutionPreviewContainer.transform, true);
                    ghostBlock.transform.position = world;
                    ghostBlock.transform.localScale = new Vector3(blockScaleXZ, solutionBlockHeight, blockScaleXZ);

                    Renderer renderer = ghostBlock.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        if (solutionPreviewMaterial != null)
                        {
                            renderer.material = new Material(solutionPreviewMaterial);
                        }
                        renderer.material.color = piece.DefaultColor * 0.9f;
                    }

                    Collider blockCollider = ghostBlock.GetComponent<Collider>();
                    if (blockCollider != null)
                    {
                        Destroy(blockCollider);
                    }
                }
            }
        }

        private IEnumerator ClearSolutionPreviewAfterDelay(float delaySeconds)
        {
            yield return new WaitForSeconds(Mathf.Max(0.1f, delaySeconds));
            ClearSolutionPreview();
        }

        private void ClearSolutionPreview()
        {
            if (solutionPreviewCoroutine != null)
            {
                StopCoroutine(solutionPreviewCoroutine);
                solutionPreviewCoroutine = null;
            }

            if (solutionPreviewContainer != null)
            {
                Destroy(solutionPreviewContainer);
                solutionPreviewContainer = null;
            }
        }
    }

    /// <summary>
    /// 게임 결과 데이터 (불변)
    /// </summary>
    public readonly struct GameResultData
    {
        public GameMode GameMode { get; }
        public DifficultyLevelSystem Difficulty { get; }
        public GameResultSummary RoundSummary { get; }
        public GemCollectionSummary GemSummary { get; }
        public int BonusScore { get; }
        public int ConsecutiveClears { get; }

        public int TotalScore => GemSummary.TotalPoints + BonusScore;

        public GameResultData(
            GameMode gameMode,
            DifficultyLevelSystem difficulty,
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
