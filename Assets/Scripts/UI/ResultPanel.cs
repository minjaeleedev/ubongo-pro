using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Ubongo
{
    public enum ResultType
    {
        RoundComplete,
        GameComplete,
        GameOver,
        MultiplayerResult
    }

    [Serializable]
    public class PlayerResult
    {
        public string playerName;
        public int score;
        public int gems;
        public float completionTime;
        public bool isLocalPlayer;
        public int rank;
    }

    public class ResultPanel : MonoBehaviour
    {
        [Header("Panel References")]
        [SerializeField] private CanvasGroup panelCanvasGroup;
        [SerializeField] private RectTransform contentContainer;

        [Header("Round Result Elements")]
        [SerializeField] private GameObject roundResultContainer;
        [SerializeField] private Text roundTitleText;
        [SerializeField] private Text completionTimeText;
        [SerializeField] private Text scoreText;
        [SerializeField] private Text bonusScoreText;
        [SerializeField] private Text totalScoreText;

        [Header("Gem Display")]
        [SerializeField] private GameObject gemDisplayContainer;
        [SerializeField] private Image[] gemIcons;
        [SerializeField] private Text gemCountText;
        [SerializeField] private ParticleSystem gemParticles;

        [Header("Star Rating")]
        [SerializeField] private GameObject starContainer;
        [SerializeField] private Image[] starImages;
        [SerializeField] private Sprite starFilledSprite;
        [SerializeField] private Sprite starEmptySprite;
        [SerializeField] private AudioClip starRevealSound;

        [Header("Final Result Elements")]
        [SerializeField] private GameObject finalResultContainer;
        [SerializeField] private Text finalTitleText;
        [SerializeField] private Text finalScoreText;
        [SerializeField] private Text totalGemsText;
        [SerializeField] private Text roundsCompletedText;
        [SerializeField] private Text bestTimeText;
        [SerializeField] private GameObject newHighScoreIndicator;

        [Header("Multiplayer Result Elements")]
        [SerializeField] private GameObject multiplayerResultContainer;
        [SerializeField] private Transform leaderboardContainer;
        [SerializeField] private GameObject playerResultPrefab;

        [Header("Buttons")]
        [SerializeField] private Button nextRoundButton;
        [SerializeField] private Text nextRoundButtonText;
        [SerializeField] private Button mainMenuButton;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button shareButton;

        [Header("Animation Settings")]
        [SerializeField] private float fadeInDuration = 0.3f;
        [SerializeField] private float elementRevealDelay = 0.2f;
        [SerializeField] private float starRevealDelay = 0.3f;
        [SerializeField] private float gemRevealDelay = 0.15f;
        [SerializeField] private float scoreCountDuration = 1f;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip panelAppearSound;
        [SerializeField] private AudioClip scoreCountSound;
        [SerializeField] private AudioClip gemCollectSound;
        [SerializeField] private AudioClip victoryFanfare;
        [SerializeField] private AudioClip gameOverSound;

        [Header("Visual Effects")]
        [SerializeField] private ParticleSystem confettiParticles;
        [SerializeField] private ParticleSystem sparkleParticles;
        [SerializeField] private Image backgroundOverlay;
        [SerializeField] private Color successBackgroundColor = new Color(0.1f, 0.3f, 0.1f, 0.9f);
        [SerializeField] private Color failureBackgroundColor = new Color(0.3f, 0.1f, 0.1f, 0.9f);

        private GameManager gameManager;
        private UIManager uiManager;
        private ResultType currentResultType;
        private int displayedScore = 0;
        private bool isAnimating = false;

        public event Action OnNextRoundRequested;
        public event Action OnRetryRequested;
        public event Action OnMainMenuRequested;

        private void Start()
        {
            gameManager = GameManager.Instance;
            uiManager = FindObjectOfType<UIManager>();

            InitializeButtons();
            HideAllContainers();

            if (panelCanvasGroup != null)
            {
                panelCanvasGroup.alpha = 0f;
                panelCanvasGroup.interactable = false;
                panelCanvasGroup.blocksRaycasts = false;
            }
        }

        private void InitializeButtons()
        {
            if (nextRoundButton != null)
                nextRoundButton.onClick.AddListener(OnNextRoundClicked);

            if (mainMenuButton != null)
                mainMenuButton.onClick.AddListener(OnMainMenuClicked);

            if (retryButton != null)
                retryButton.onClick.AddListener(OnRetryClicked);

            if (shareButton != null)
                shareButton.onClick.AddListener(OnShareClicked);
        }

        private void HideAllContainers()
        {
            if (roundResultContainer != null)
                roundResultContainer.SetActive(false);

            if (finalResultContainer != null)
                finalResultContainer.SetActive(false);

            if (multiplayerResultContainer != null)
                multiplayerResultContainer.SetActive(false);
        }

        public void ShowRoundResult(int roundNumber, int totalRounds, float completionTime, int roundScore, int bonusScore, int gemsEarned, int starRating)
        {
            currentResultType = ResultType.RoundComplete;
            StartCoroutine(DisplayRoundResult(roundNumber, totalRounds, completionTime, roundScore, bonusScore, gemsEarned, starRating));
        }

        private IEnumerator DisplayRoundResult(int roundNumber, int totalRounds, float completionTime, int roundScore, int bonusScore, int gemsEarned, int starRating)
        {
            isAnimating = true;
            HideAllContainers();

            if (backgroundOverlay != null)
            {
                backgroundOverlay.color = successBackgroundColor;
            }

            yield return StartCoroutine(FadeInPanel());

            if (roundResultContainer != null)
            {
                roundResultContainer.SetActive(true);
            }

            PlaySound(panelAppearSound);

            if (roundTitleText != null)
            {
                roundTitleText.text = $"Round {roundNumber}/{totalRounds} Complete!";
                yield return StartCoroutine(RevealElement(roundTitleText.gameObject));
            }

            yield return new WaitForSeconds(elementRevealDelay);

            if (completionTimeText != null)
            {
                int minutes = Mathf.FloorToInt(completionTime / 60);
                int seconds = Mathf.FloorToInt(completionTime % 60);
                completionTimeText.text = $"Time: {minutes:00}:{seconds:00}";
                yield return StartCoroutine(RevealElement(completionTimeText.gameObject));
            }

            yield return new WaitForSeconds(elementRevealDelay);

            if (scoreText != null)
            {
                yield return StartCoroutine(CountUpScore(scoreText, 0, roundScore));
            }

            if (bonusScoreText != null && bonusScore > 0)
            {
                bonusScoreText.text = $"Time Bonus: +{bonusScore}";
                yield return StartCoroutine(RevealElement(bonusScoreText.gameObject));
            }

            if (totalScoreText != null)
            {
                int total = roundScore + bonusScore;
                totalScoreText.text = $"Total: {total}";
                yield return StartCoroutine(RevealElement(totalScoreText.gameObject));
            }

            yield return new WaitForSeconds(elementRevealDelay);

            yield return StartCoroutine(RevealStars(starRating));

            yield return new WaitForSeconds(elementRevealDelay);

            yield return StartCoroutine(RevealGems(gemsEarned));

            bool isLastRound = roundNumber >= totalRounds;

            if (nextRoundButton != null)
            {
                nextRoundButton.gameObject.SetActive(!isLastRound);
                if (nextRoundButtonText != null)
                {
                    nextRoundButtonText.text = "Next Round";
                }
            }

            if (mainMenuButton != null)
            {
                mainMenuButton.gameObject.SetActive(true);
            }

            EnableInteraction();
            isAnimating = false;
        }

        public void ShowFinalResult(int totalScore, int totalGems, int roundsCompleted, int totalRounds, float bestTime, bool isHighScore)
        {
            currentResultType = ResultType.GameComplete;
            StartCoroutine(DisplayFinalResult(totalScore, totalGems, roundsCompleted, totalRounds, bestTime, isHighScore));
        }

        private IEnumerator DisplayFinalResult(int totalScore, int totalGems, int roundsCompleted, int totalRounds, float bestTime, bool isHighScore)
        {
            isAnimating = true;
            HideAllContainers();

            if (backgroundOverlay != null)
            {
                backgroundOverlay.color = successBackgroundColor;
            }

            yield return StartCoroutine(FadeInPanel());

            if (finalResultContainer != null)
            {
                finalResultContainer.SetActive(true);
            }

            PlaySound(victoryFanfare);

            if (confettiParticles != null)
            {
                confettiParticles.Play();
            }

            if (finalTitleText != null)
            {
                finalTitleText.text = "Game Complete!";
                yield return StartCoroutine(RevealElement(finalTitleText.gameObject));
            }

            yield return new WaitForSeconds(elementRevealDelay);

            if (finalScoreText != null)
            {
                yield return StartCoroutine(CountUpScore(finalScoreText, 0, totalScore));
            }

            if (totalGemsText != null)
            {
                totalGemsText.text = $"Gems Collected: {totalGems}";
                yield return StartCoroutine(RevealElement(totalGemsText.gameObject));
            }

            if (roundsCompletedText != null)
            {
                roundsCompletedText.text = $"Rounds: {roundsCompleted}/{totalRounds}";
                yield return StartCoroutine(RevealElement(roundsCompletedText.gameObject));
            }

            if (bestTimeText != null)
            {
                int minutes = Mathf.FloorToInt(bestTime / 60);
                int seconds = Mathf.FloorToInt(bestTime % 60);
                bestTimeText.text = $"Best Time: {minutes:00}:{seconds:00}";
                yield return StartCoroutine(RevealElement(bestTimeText.gameObject));
            }

            if (isHighScore && newHighScoreIndicator != null)
            {
                newHighScoreIndicator.SetActive(true);
                yield return StartCoroutine(RevealElement(newHighScoreIndicator));
            }

            if (nextRoundButton != null)
            {
                nextRoundButton.gameObject.SetActive(false);
            }

            if (retryButton != null)
            {
                retryButton.gameObject.SetActive(true);
            }

            if (mainMenuButton != null)
            {
                mainMenuButton.gameObject.SetActive(true);
            }

            EnableInteraction();
            isAnimating = false;
        }

        public void ShowGameOver(int finalScore, int totalGems, int roundReached, int totalRounds)
        {
            currentResultType = ResultType.GameOver;
            StartCoroutine(DisplayGameOver(finalScore, totalGems, roundReached, totalRounds));
        }

        private IEnumerator DisplayGameOver(int finalScore, int totalGems, int roundReached, int totalRounds)
        {
            isAnimating = true;
            HideAllContainers();

            if (backgroundOverlay != null)
            {
                backgroundOverlay.color = failureBackgroundColor;
            }

            yield return StartCoroutine(FadeInPanel());

            if (finalResultContainer != null)
            {
                finalResultContainer.SetActive(true);
            }

            PlaySound(gameOverSound);

            if (finalTitleText != null)
            {
                finalTitleText.text = "Time's Up!";
                yield return StartCoroutine(RevealElement(finalTitleText.gameObject));
            }

            yield return new WaitForSeconds(elementRevealDelay);

            if (finalScoreText != null)
            {
                finalScoreText.text = $"Final Score: {finalScore}";
                yield return StartCoroutine(RevealElement(finalScoreText.gameObject));
            }

            if (totalGemsText != null)
            {
                totalGemsText.text = $"Gems Collected: {totalGems}";
                yield return StartCoroutine(RevealElement(totalGemsText.gameObject));
            }

            if (roundsCompletedText != null)
            {
                roundsCompletedText.text = $"Reached Round: {roundReached}/{totalRounds}";
                yield return StartCoroutine(RevealElement(roundsCompletedText.gameObject));
            }

            if (newHighScoreIndicator != null)
            {
                newHighScoreIndicator.SetActive(false);
            }

            if (nextRoundButton != null)
            {
                nextRoundButton.gameObject.SetActive(false);
            }

            if (retryButton != null)
            {
                retryButton.gameObject.SetActive(true);
            }

            if (mainMenuButton != null)
            {
                mainMenuButton.gameObject.SetActive(true);
            }

            EnableInteraction();
            isAnimating = false;
        }

        public void ShowMultiplayerResult(List<PlayerResult> results)
        {
            currentResultType = ResultType.MultiplayerResult;
            StartCoroutine(DisplayMultiplayerResult(results));
        }

        private IEnumerator DisplayMultiplayerResult(List<PlayerResult> results)
        {
            isAnimating = true;
            HideAllContainers();

            if (backgroundOverlay != null)
            {
                backgroundOverlay.color = successBackgroundColor;
            }

            yield return StartCoroutine(FadeInPanel());

            if (multiplayerResultContainer != null)
            {
                multiplayerResultContainer.SetActive(true);
            }

            ClearLeaderboard();

            results.Sort((a, b) => b.score.CompareTo(a.score));

            for (int i = 0; i < results.Count; i++)
            {
                results[i].rank = i + 1;
            }

            for (int i = results.Count - 1; i >= 0; i--)
            {
                yield return new WaitForSeconds(0.5f);
                CreatePlayerResultEntry(results[i]);

                if (i == 0)
                {
                    PlaySound(victoryFanfare);
                    if (confettiParticles != null)
                    {
                        confettiParticles.Play();
                    }
                }
            }

            if (nextRoundButton != null)
            {
                nextRoundButton.gameObject.SetActive(true);
                if (nextRoundButtonText != null)
                {
                    nextRoundButtonText.text = "Next Round";
                }
            }

            if (mainMenuButton != null)
            {
                mainMenuButton.gameObject.SetActive(true);
            }

            EnableInteraction();
            isAnimating = false;
        }

        private void ClearLeaderboard()
        {
            if (leaderboardContainer == null) return;

            foreach (Transform child in leaderboardContainer)
            {
                Destroy(child.gameObject);
            }
        }

        private void CreatePlayerResultEntry(PlayerResult result)
        {
            if (playerResultPrefab == null || leaderboardContainer == null) return;

            GameObject entry = Instantiate(playerResultPrefab, leaderboardContainer);

            Text rankText = entry.transform.Find("RankText")?.GetComponent<Text>();
            if (rankText != null)
            {
                rankText.text = $"{result.rank}";
            }

            Text nameText = entry.transform.Find("NameText")?.GetComponent<Text>();
            if (nameText != null)
            {
                nameText.text = result.playerName;
                if (result.isLocalPlayer)
                {
                    nameText.text += " (You)";
                    nameText.color = Color.yellow;
                }
            }

            Text scoreText = entry.transform.Find("ScoreText")?.GetComponent<Text>();
            if (scoreText != null)
            {
                scoreText.text = $"{result.score}";
            }

            Text timeText = entry.transform.Find("TimeText")?.GetComponent<Text>();
            if (timeText != null)
            {
                if (result.completionTime > 0)
                {
                    int minutes = Mathf.FloorToInt(result.completionTime / 60);
                    int seconds = Mathf.FloorToInt(result.completionTime % 60);
                    timeText.text = $"{minutes:00}:{seconds:00}";
                }
                else
                {
                    timeText.text = "DNF";
                    timeText.color = Color.red;
                }
            }

            Text gemsText = entry.transform.Find("GemsText")?.GetComponent<Text>();
            if (gemsText != null)
            {
                gemsText.text = $"+{result.gems}";
            }

            if (result.rank == 1)
            {
                Image crownIcon = entry.transform.Find("CrownIcon")?.GetComponent<Image>();
                if (crownIcon != null)
                {
                    crownIcon.gameObject.SetActive(true);
                }

                Image background = entry.GetComponent<Image>();
                if (background != null)
                {
                    background.color = new Color(1f, 0.85f, 0f, 0.3f);
                }
            }

            StartCoroutine(RevealElement(entry));
        }

        private IEnumerator FadeInPanel()
        {
            if (panelCanvasGroup == null) yield break;

            panelCanvasGroup.gameObject.SetActive(true);
            float elapsed = 0f;

            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                panelCanvasGroup.alpha = elapsed / fadeInDuration;
                yield return null;
            }

            panelCanvasGroup.alpha = 1f;
        }

        private IEnumerator FadeOutPanel()
        {
            if (panelCanvasGroup == null) yield break;

            float elapsed = 0f;

            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                panelCanvasGroup.alpha = 1f - (elapsed / fadeInDuration);
                yield return null;
            }

            panelCanvasGroup.alpha = 0f;
            panelCanvasGroup.gameObject.SetActive(false);
        }

        private IEnumerator RevealElement(GameObject element)
        {
            if (element == null) yield break;

            element.SetActive(true);

            CanvasGroup canvasGroup = element.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = element.AddComponent<CanvasGroup>();
            }

            canvasGroup.alpha = 0f;

            RectTransform rect = element.GetComponent<RectTransform>();
            Vector3 originalScale = rect != null ? rect.localScale : Vector3.one;
            if (rect != null)
            {
                rect.localScale = originalScale * 0.5f;
            }

            float elapsed = 0f;
            float duration = 0.3f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float smoothT = 1f - Mathf.Pow(1f - t, 3f);

                canvasGroup.alpha = smoothT;

                if (rect != null)
                {
                    rect.localScale = Vector3.Lerp(originalScale * 0.5f, originalScale, smoothT);
                }

                yield return null;
            }

            canvasGroup.alpha = 1f;
            if (rect != null)
            {
                rect.localScale = originalScale;
            }
        }

        private IEnumerator RevealStars(int starCount)
        {
            if (starImages == null || starContainer == null) yield break;

            starContainer.SetActive(true);

            foreach (Image star in starImages)
            {
                if (star != null && starEmptySprite != null)
                {
                    star.sprite = starEmptySprite;
                    star.color = new Color(1f, 1f, 1f, 0.3f);
                }
            }

            yield return new WaitForSeconds(elementRevealDelay);

            for (int i = 0; i < starCount && i < starImages.Length; i++)
            {
                if (starImages[i] != null)
                {
                    yield return new WaitForSeconds(starRevealDelay);

                    PlaySound(starRevealSound);

                    if (starFilledSprite != null)
                    {
                        starImages[i].sprite = starFilledSprite;
                    }
                    starImages[i].color = Color.white;

                    yield return StartCoroutine(PopStar(starImages[i].transform));
                }
            }
        }

        private IEnumerator PopStar(Transform starTransform)
        {
            Vector3 originalScale = starTransform.localScale;
            Vector3 targetScale = originalScale * 1.3f;

            float elapsed = 0f;
            float duration = 0.15f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                starTransform.localScale = Vector3.Lerp(originalScale, targetScale, t);
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                starTransform.localScale = Vector3.Lerp(targetScale, originalScale, t);
                yield return null;
            }

            starTransform.localScale = originalScale;
        }

        private IEnumerator RevealGems(int gemCount)
        {
            if (gemDisplayContainer == null) yield break;

            gemDisplayContainer.SetActive(true);

            if (gemIcons != null)
            {
                foreach (Image gem in gemIcons)
                {
                    if (gem != null)
                    {
                        gem.gameObject.SetActive(false);
                    }
                }

                for (int i = 0; i < gemCount && i < gemIcons.Length; i++)
                {
                    if (gemIcons[i] != null)
                    {
                        yield return new WaitForSeconds(gemRevealDelay);

                        gemIcons[i].gameObject.SetActive(true);
                        PlaySound(gemCollectSound);

                        if (gemParticles != null)
                        {
                            gemParticles.transform.position = gemIcons[i].transform.position;
                            gemParticles.Play();
                        }

                        yield return StartCoroutine(PopStar(gemIcons[i].transform));
                    }
                }
            }

            if (gemCountText != null)
            {
                gemCountText.text = $"+{gemCount}";
                yield return StartCoroutine(RevealElement(gemCountText.gameObject));
            }
        }

        private IEnumerator CountUpScore(Text scoreText, int fromScore, int toScore)
        {
            if (scoreText == null) yield break;

            float elapsed = 0f;
            displayedScore = fromScore;

            while (elapsed < scoreCountDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / scoreCountDuration;
                float smoothT = t * t * (3f - 2f * t);

                displayedScore = Mathf.RoundToInt(Mathf.Lerp(fromScore, toScore, smoothT));
                scoreText.text = $"Score: {displayedScore}";

                if (elapsed % 0.1f < Time.deltaTime)
                {
                    PlaySound(scoreCountSound);
                }

                yield return null;
            }

            displayedScore = toScore;
            scoreText.text = $"Score: {displayedScore}";
        }

        private void EnableInteraction()
        {
            if (panelCanvasGroup != null)
            {
                panelCanvasGroup.interactable = true;
                panelCanvasGroup.blocksRaycasts = true;
            }
        }

        private void DisableInteraction()
        {
            if (panelCanvasGroup != null)
            {
                panelCanvasGroup.interactable = false;
                panelCanvasGroup.blocksRaycasts = false;
            }
        }

        private void PlaySound(AudioClip clip)
        {
            if (audioSource != null && clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }

        public void Hide()
        {
            StartCoroutine(HidePanel());
        }

        private IEnumerator HidePanel()
        {
            DisableInteraction();
            yield return StartCoroutine(FadeOutPanel());
            HideAllContainers();
        }

        private void OnNextRoundClicked()
        {
            if (isAnimating) return;
            OnNextRoundRequested?.Invoke();
            Hide();
        }

        private void OnMainMenuClicked()
        {
            if (isAnimating) return;
            OnMainMenuRequested?.Invoke();
            Hide();
        }

        private void OnRetryClicked()
        {
            if (isAnimating) return;
            OnRetryRequested?.Invoke();
            Hide();
        }

        private void OnShareClicked()
        {
            if (isAnimating) return;

            string shareText = currentResultType switch
            {
                ResultType.GameComplete => $"I completed Ubongo 3D with a score of {displayedScore}!",
                ResultType.RoundComplete => $"I completed a round in Ubongo 3D!",
                _ => "Check out Ubongo 3D!"
            };
        }

        public int CalculateStarRating(float completionTime, float targetTime)
        {
            float ratio = completionTime / targetTime;

            if (ratio <= 0.5f)
            {
                return 3;
            }
            else if (ratio <= 0.75f)
            {
                return 2;
            }
            else if (ratio <= 1f)
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }

        public int CalculateGemsEarned(int starRating)
        {
            return starRating switch
            {
                3 => 3,
                2 => 2,
                1 => 1,
                _ => 0
            };
        }
    }
}
