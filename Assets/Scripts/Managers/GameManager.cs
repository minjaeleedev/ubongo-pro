using UnityEngine;
using System;
using System.Collections;

namespace Ubongo
{
    public enum GameState
    {
        Menu,
        Playing,
        Paused,
        LevelComplete,
        GameOver
    }

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
                        GameObject go = new GameObject("GameManager");
                        _instance = go.AddComponent<GameManager>();
                    }
                }
                return _instance;
            }
        }

        [Header("Game Settings")]
        [SerializeField] private int currentLevel = 1;
        [SerializeField] private float timeLimit = 60f;
        [SerializeField] private int score = 0;
        
        [Header("Game State")]
        [SerializeField] private GameState currentState = GameState.Menu;
        private float remainingTime;
        
        public event Action<GameState> OnGameStateChanged;
        public event Action<int> OnScoreChanged;
        public event Action<float> OnTimeChanged;
        public event Action OnLevelComplete;
        
        public GameState CurrentState => currentState;
        public int CurrentLevel => currentLevel;
        public int Score => score;
        public float RemainingTime => remainingTime;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            InitializeGame();
        }

        private void InitializeGame()
        {
            remainingTime = timeLimit;
            score = 0;
            ChangeState(GameState.Menu);
        }

        public void StartGame()
        {
            currentLevel = 1;
            score = 0;
            remainingTime = timeLimit;
            ChangeState(GameState.Playing);
            StartCoroutine(GameTimer());
        }

        public void PauseGame()
        {
            if (currentState == GameState.Playing)
            {
                ChangeState(GameState.Paused);
                Time.timeScale = 0f;
            }
        }

        public void ResumeGame()
        {
            if (currentState == GameState.Paused)
            {
                ChangeState(GameState.Playing);
                Time.timeScale = 1f;
            }
        }

        private IEnumerator GameTimer()
        {
            while (currentState == GameState.Playing && remainingTime > 0)
            {
                remainingTime -= Time.deltaTime;
                OnTimeChanged?.Invoke(remainingTime);
                
                if (remainingTime <= 0)
                {
                    GameOver();
                }
                
                yield return null;
            }
        }

        public void CompleteLevel()
        {
            if (currentState != GameState.Playing) return;
            
            int bonusScore = Mathf.RoundToInt(remainingTime * 10);
            AddScore(bonusScore);
            
            ChangeState(GameState.LevelComplete);
            OnLevelComplete?.Invoke();
            
            StartCoroutine(LoadNextLevelAfterDelay());
        }

        private IEnumerator LoadNextLevelAfterDelay()
        {
            yield return new WaitForSeconds(2f);
            NextLevel();
        }

        public void NextLevel()
        {
            currentLevel++;
            remainingTime = timeLimit + (currentLevel * 5);
            ChangeState(GameState.Playing);
            StartCoroutine(GameTimer());
        }

        public void AddScore(int points)
        {
            score += points;
            OnScoreChanged?.Invoke(score);
        }

        private void GameOver()
        {
            ChangeState(GameState.GameOver);
            Time.timeScale = 1f;
        }

        private void ChangeState(GameState newState)
        {
            currentState = newState;
            OnGameStateChanged?.Invoke(currentState);
        }

        public void RestartLevel()
        {
            remainingTime = timeLimit + (currentLevel * 5);
            ChangeState(GameState.Playing);
            StartCoroutine(GameTimer());
        }

        public void QuitGame()
        {
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }
    }
}