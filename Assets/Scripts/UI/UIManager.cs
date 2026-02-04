using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;

namespace Ubongo
{
    public enum Difficulty
    {
        Junior,     // 3 blocks
        Senior,     // 4 blocks
        Master      // 5+ blocks
    }

    public class UIManager : MonoBehaviour
    {
        [Header("UI Panels")]
        [SerializeField] private GameObject menuPanel;
        [SerializeField] private GameObject gamePanel;
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private GameObject levelCompletePanel;

        [Header("Game UI Elements")]
        [SerializeField] private Text timerText;
        [SerializeField] private Text scoreText;
        [SerializeField] private Text levelText;
        [SerializeField] private Text roundText;
        [SerializeField] private Text gemCounterText;
        [SerializeField] private Text difficultyText;
        [SerializeField] private Button pauseButton;

        [Header("Dice UI Elements")]
        [SerializeField] private GameObject dicePanel;
        [SerializeField] private Text diceResultText;
        [SerializeField] private Image diceImage;
        [SerializeField] private Button rollDiceButton;

        [Header("Menu UI Elements")]
        [SerializeField] private Button startButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private Text totalGemsText;

        [Header("Pause UI Elements")]
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button menuButton;

        [Header("Game Over UI Elements")]
        [SerializeField] private Text finalScoreText;
        [SerializeField] private Text finalGemsText;
        [SerializeField] private Text reachedRoundText;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button mainMenuButton;

        [Header("Level Complete UI Elements")]
        [SerializeField] private Text levelCompleteScoreText;
        [SerializeField] private Text roundGemsText;
        [SerializeField] private Text bonusScoreText;
        [SerializeField] private Button nextLevelButton;

        [Header("Game Settings")]
        [SerializeField] private int totalRounds = 9;
        [SerializeField] private Difficulty currentDifficulty = Difficulty.Junior;

        [Header("Visual Settings")]
        [SerializeField] private Color timerNormalColor = Color.white;
        [SerializeField] private Color timerWarningColor = Color.yellow;
        [SerializeField] private Color timerDangerColor = Color.red;

        private GameManager gameManager;
        private int currentGems = 0;
        private int totalGems = 0;
        private int currentRound = 1;
        private int lastDiceResult = 0;
        private bool isTimerFlashing = false;

        public event Action<int> OnDiceRolled;
        public event Action<int> OnGemsChanged;

        public int CurrentGems => currentGems;
        public int TotalGems => totalGems;
        public int CurrentRound => currentRound;
        public Difficulty CurrentDifficulty => currentDifficulty;

        private void Start()
        {
            gameManager = GameManager.Instance;
            InitializeUI();
            SubscribeToEvents();
            LoadTotalGems();
        }

        private void InitializeUI()
        {
            ShowPanel(menuPanel);

            if (startButton != null)
                startButton.onClick.AddListener(OnStartGame);

            if (quitButton != null)
                quitButton.onClick.AddListener(OnQuitGame);

            if (pauseButton != null)
                pauseButton.onClick.AddListener(OnPauseGame);

            if (resumeButton != null)
                resumeButton.onClick.AddListener(OnResumeGame);

            if (restartButton != null)
                restartButton.onClick.AddListener(OnRestartLevel);

            if (menuButton != null)
                menuButton.onClick.AddListener(OnReturnToMenu);

            if (retryButton != null)
                retryButton.onClick.AddListener(OnRetryGame);

            if (mainMenuButton != null)
                mainMenuButton.onClick.AddListener(OnReturnToMenu);

            if (nextLevelButton != null)
                nextLevelButton.onClick.AddListener(OnNextLevel);

            if (rollDiceButton != null)
                rollDiceButton.onClick.AddListener(OnRollDice);

            UpdateGemDisplay();
            UpdateDifficultyDisplay();
        }

        private void SubscribeToEvents()
        {
            if (gameManager != null)
            {
                gameManager.OnGameStateChanged += OnGameStateChanged;
                gameManager.OnScoreChanged += UpdateScore;
                gameManager.OnTimeChanged += UpdateTimer;
                gameManager.OnLevelComplete += OnLevelCompleted;
            }
        }

        private void OnDestroy()
        {
            if (gameManager != null)
            {
                gameManager.OnGameStateChanged -= OnGameStateChanged;
                gameManager.OnScoreChanged -= UpdateScore;
                gameManager.OnTimeChanged -= UpdateTimer;
                gameManager.OnLevelComplete -= OnLevelCompleted;
            }
        }

        private void OnGameStateChanged(GameState newState)
        {
            HideAllPanels();

            switch (newState)
            {
                case GameState.Menu:
                    ShowPanel(menuPanel);
                    UpdateTotalGemsDisplay();
                    break;

                case GameState.Playing:
                    ShowPanel(gamePanel);
                    UpdateLevel();
                    UpdateRoundDisplay();
                    UpdateGemDisplay();
                    UpdateDifficultyDisplay();
                    isTimerFlashing = false;
                    break;

                case GameState.Paused:
                    ShowPanel(pausePanel);
                    ShowPanel(gamePanel);
                    break;

                case GameState.GameOver:
                    ShowPanel(gameOverPanel);
                    DisplayGameOverResults();
                    break;

                case GameState.RoundComplete:
                    ShowPanel(levelCompletePanel);
                    DisplayLevelCompleteResults();
                    break;
            }
        }

        private void UpdateTimer(float time)
        {
            if (timerText == null) return;

            int minutes = Mathf.FloorToInt(time / 60);
            int seconds = Mathf.FloorToInt(time % 60);
            timerText.text = $"{minutes:00}:{seconds:00}";

            if (time < 10f)
            {
                timerText.color = timerDangerColor;
                if (!isTimerFlashing)
                {
                    StartCoroutine(FlashTimerContinuous());
                }
            }
            else if (time < 30f)
            {
                timerText.color = timerWarningColor;
                isTimerFlashing = false;
            }
            else
            {
                timerText.color = timerNormalColor;
                isTimerFlashing = false;
            }
        }

        private IEnumerator FlashTimerContinuous()
        {
            isTimerFlashing = true;

            while (isTimerFlashing && timerText != null)
            {
                timerText.enabled = false;
                yield return new WaitForSeconds(0.2f);

                if (!isTimerFlashing) break;

                timerText.enabled = true;
                yield return new WaitForSeconds(0.2f);
            }

            if (timerText != null)
            {
                timerText.enabled = true;
            }
        }

        private void UpdateScore(int score)
        {
            if (scoreText == null) return;

            scoreText.text = $"Score: {score}";
            StartCoroutine(AnimateScoreChange());
        }

        private IEnumerator AnimateScoreChange()
        {
            if (scoreText == null) yield break;

            Vector3 originalScale = scoreText.transform.localScale;
            scoreText.transform.localScale = originalScale * 1.2f;
            yield return new WaitForSeconds(0.15f);
            scoreText.transform.localScale = originalScale;
        }

        private void UpdateLevel()
        {
            if (levelText != null)
            {
                levelText.text = $"Level {gameManager.CurrentLevel}";
            }
        }

        private void UpdateRoundDisplay()
        {
            if (roundText != null)
            {
                roundText.text = $"Round {currentRound}/{totalRounds}";
            }
        }

        private void UpdateGemDisplay()
        {
            if (gemCounterText != null)
            {
                gemCounterText.text = $"{currentGems}";
            }
        }

        private void UpdateTotalGemsDisplay()
        {
            if (totalGemsText != null)
            {
                totalGemsText.text = $"Gems: {totalGems}";
            }
        }

        private void UpdateDifficultyDisplay()
        {
            if (difficultyText == null) return;

            string difficultyName = currentDifficulty switch
            {
                Difficulty.Junior => "Junior (3 blocks)",
                Difficulty.Senior => "Senior (4 blocks)",
                Difficulty.Master => "Master (5+ blocks)",
                _ => "Unknown"
            };

            difficultyText.text = difficultyName;
        }

        public void SetDifficulty(Difficulty difficulty)
        {
            currentDifficulty = difficulty;
            UpdateDifficultyDisplay();
        }

        public void AddGems(int amount)
        {
            currentGems += amount;
            totalGems += amount;
            UpdateGemDisplay();
            SaveTotalGems();
            OnGemsChanged?.Invoke(currentGems);
            StartCoroutine(AnimateGemGain(amount));
        }

        private IEnumerator AnimateGemGain(int amount)
        {
            if (gemCounterText == null) yield break;

            Vector3 originalScale = gemCounterText.transform.localScale;
            Color originalColor = gemCounterText.color;

            gemCounterText.color = Color.yellow;
            gemCounterText.transform.localScale = originalScale * 1.3f;

            yield return new WaitForSeconds(0.2f);

            gemCounterText.transform.localScale = originalScale;
            gemCounterText.color = originalColor;
        }

        public void UseGems(int amount)
        {
            if (currentGems >= amount)
            {
                currentGems -= amount;
                totalGems -= amount;
                UpdateGemDisplay();
                SaveTotalGems();
                OnGemsChanged?.Invoke(currentGems);
            }
        }

        private void OnRollDice()
        {
            StartCoroutine(RollDiceAnimation());
        }

        private IEnumerator RollDiceAnimation()
        {
            if (rollDiceButton != null)
            {
                rollDiceButton.interactable = false;
            }

            for (int i = 0; i < 10; i++)
            {
                int randomValue = UnityEngine.Random.Range(1, 7);
                if (diceResultText != null)
                {
                    diceResultText.text = randomValue.ToString();
                }
                yield return new WaitForSeconds(0.05f + (i * 0.02f));
            }

            lastDiceResult = UnityEngine.Random.Range(1, 7);

            if (diceResultText != null)
            {
                diceResultText.text = lastDiceResult.ToString();
            }

            OnDiceRolled?.Invoke(lastDiceResult);

            if (rollDiceButton != null)
            {
                rollDiceButton.interactable = true;
            }
        }

        public void ShowDiceResult(int result)
        {
            lastDiceResult = result;

            if (dicePanel != null)
            {
                dicePanel.SetActive(true);
            }

            if (diceResultText != null)
            {
                diceResultText.text = result.ToString();
            }
        }

        public void HideDicePanel()
        {
            if (dicePanel != null)
            {
                dicePanel.SetActive(false);
            }
        }

        public void NextRound()
        {
            if (currentRound < totalRounds)
            {
                currentRound++;
                UpdateRoundDisplay();
            }
        }

        public void ResetRounds()
        {
            currentRound = 1;
            currentGems = 0;
            UpdateRoundDisplay();
            UpdateGemDisplay();
        }

        public bool IsLastRound()
        {
            return currentRound >= totalRounds;
        }

        private void OnLevelCompleted()
        {
            int gemsEarned = CalculateGemsForRound();
            AddGems(gemsEarned);

            StartCoroutine(ShowLevelCompleteAnimation());
        }

        private int CalculateGemsForRound()
        {
            float remainingTime = gameManager.RemainingTime;

            if (remainingTime > 30f)
            {
                return 3;
            }
            else if (remainingTime > 15f)
            {
                return 2;
            }
            else
            {
                return 1;
            }
        }

        private IEnumerator ShowLevelCompleteAnimation()
        {
            yield return new WaitForSeconds(0.5f);
        }

        private void DisplayLevelCompleteResults()
        {
            if (levelCompleteScoreText != null)
            {
                levelCompleteScoreText.text = $"Score: {gameManager.Score}";
            }

            int gemsEarned = CalculateGemsForRound();
            if (roundGemsText != null)
            {
                roundGemsText.text = $"Gems Earned: {gemsEarned}";
            }

            int bonusScore = Mathf.RoundToInt(gameManager.RemainingTime * 10);
            if (bonusScoreText != null)
            {
                bonusScoreText.text = $"Time Bonus: +{bonusScore}";
            }
        }

        private void DisplayGameOverResults()
        {
            if (finalScoreText != null)
            {
                finalScoreText.text = $"Final Score: {gameManager.Score}";
            }

            if (finalGemsText != null)
            {
                finalGemsText.text = $"Gems Collected: {currentGems}";
            }

            if (reachedRoundText != null)
            {
                reachedRoundText.text = $"Reached Round: {currentRound}/{totalRounds}";
            }
        }

        private void HideAllPanels()
        {
            if (menuPanel != null) menuPanel.SetActive(false);
            if (gamePanel != null) gamePanel.SetActive(false);
            if (pausePanel != null) pausePanel.SetActive(false);
            if (gameOverPanel != null) gameOverPanel.SetActive(false);
            if (levelCompletePanel != null) levelCompletePanel.SetActive(false);
            if (dicePanel != null) dicePanel.SetActive(false);
        }

        private void ShowPanel(GameObject panel)
        {
            if (panel != null)
                panel.SetActive(true);
        }

        private void OnStartGame()
        {
            ResetRounds();
            gameManager.StartGame();
        }

        private void OnPauseGame()
        {
            gameManager.PauseGame();
        }

        private void OnResumeGame()
        {
            gameManager.ResumeGame();
        }

        private void OnRestartLevel()
        {
            gameManager.RestartLevel();
        }

        private void OnRetryGame()
        {
            ResetRounds();
            gameManager.StartGame();
        }

        private void OnReturnToMenu()
        {
            Time.timeScale = 1f;
            gameManager.ReturnToMenu();
        }

        private void OnNextLevel()
        {
            NextRound();
            gameManager.NextLevel();
        }

        private void OnQuitGame()
        {
            gameManager.QuitGame();
        }

        private void LoadTotalGems()
        {
            totalGems = PlayerPrefs.GetInt("TotalGems", 0);
            UpdateTotalGemsDisplay();
        }

        private void SaveTotalGems()
        {
            PlayerPrefs.SetInt("TotalGems", totalGems);
            PlayerPrefs.Save();
        }
    }
}
