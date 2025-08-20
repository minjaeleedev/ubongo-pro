using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace Ubongo
{
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
        [SerializeField] private Button pauseButton;
        
        [Header("Menu UI Elements")]
        [SerializeField] private Button startButton;
        [SerializeField] private Button quitButton;
        
        [Header("Pause UI Elements")]
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button menuButton;
        
        [Header("Game Over UI Elements")]
        [SerializeField] private Text finalScoreText;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button mainMenuButton;
        
        [Header("Level Complete UI Elements")]
        [SerializeField] private Text levelCompleteScoreText;
        [SerializeField] private Button nextLevelButton;
        
        private GameManager gameManager;
        
        private void Start()
        {
            gameManager = GameManager.Instance;
            InitializeUI();
            SubscribeToEvents();
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
                    break;
                    
                case GameState.Playing:
                    ShowPanel(gamePanel);
                    UpdateLevel();
                    break;
                    
                case GameState.Paused:
                    ShowPanel(pausePanel);
                    ShowPanel(gamePanel);
                    break;
                    
                case GameState.GameOver:
                    ShowPanel(gameOverPanel);
                    if (finalScoreText != null)
                        finalScoreText.text = $"Final Score: {gameManager.Score}";
                    break;
                    
                case GameState.LevelComplete:
                    ShowPanel(levelCompletePanel);
                    if (levelCompleteScoreText != null)
                        levelCompleteScoreText.text = $"Score: {gameManager.Score}";
                    break;
            }
        }
        
        private void UpdateTimer(float time)
        {
            if (timerText != null)
            {
                int minutes = Mathf.FloorToInt(time / 60);
                int seconds = Mathf.FloorToInt(time % 60);
                timerText.text = $"{minutes:00}:{seconds:00}";
                
                if (time < 10f)
                {
                    timerText.color = Color.red;
                    StartCoroutine(FlashTimer());
                }
                else if (time < 30f)
                {
                    timerText.color = Color.yellow;
                }
                else
                {
                    timerText.color = Color.white;
                }
            }
        }
        
        private IEnumerator FlashTimer()
        {
            if (timerText != null)
            {
                timerText.enabled = false;
                yield return new WaitForSeconds(0.2f);
                timerText.enabled = true;
            }
        }
        
        private void UpdateScore(int score)
        {
            if (scoreText != null)
            {
                scoreText.text = $"Score: {score}";
                StartCoroutine(AnimateScoreChange());
            }
        }
        
        private IEnumerator AnimateScoreChange()
        {
            if (scoreText != null)
            {
                Vector3 originalScale = scoreText.transform.localScale;
                scoreText.transform.localScale = originalScale * 1.2f;
                yield return new WaitForSeconds(0.2f);
                scoreText.transform.localScale = originalScale;
            }
        }
        
        private void UpdateLevel()
        {
            if (levelText != null)
            {
                levelText.text = $"Level {gameManager.CurrentLevel}";
            }
        }
        
        private void OnLevelCompleted()
        {
            StartCoroutine(ShowLevelCompleteAnimation());
        }
        
        private IEnumerator ShowLevelCompleteAnimation()
        {
            yield return new WaitForSeconds(0.5f);
        }
        
        private void HideAllPanels()
        {
            if (menuPanel != null) menuPanel.SetActive(false);
            if (gamePanel != null) gamePanel.SetActive(false);
            if (pausePanel != null) pausePanel.SetActive(false);
            if (gameOverPanel != null) gameOverPanel.SetActive(false);
            if (levelCompletePanel != null) levelCompletePanel.SetActive(false);
        }
        
        private void ShowPanel(GameObject panel)
        {
            if (panel != null)
                panel.SetActive(true);
        }
        
        private void OnStartGame()
        {
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
            gameManager.StartGame();
        }
        
        private void OnReturnToMenu()
        {
            Time.timeScale = 1f;
            gameManager.ChangeState(GameState.Menu);
        }
        
        private void OnNextLevel()
        {
            gameManager.NextLevel();
        }
        
        private void OnQuitGame()
        {
            gameManager.QuitGame();
        }
    }
}
