using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;

namespace Ubongo
{
    public class GameHUD : MonoBehaviour
    {
        [Header("Timer Warning Effects")]
        [SerializeField] private Text timerText;
        [SerializeField] private Image timerBackgroundImage;
        [SerializeField] private Image screenBorderGlow;
        [SerializeField] private AudioSource warningAudioSource;
        [SerializeField] private AudioClip countdownTickSound;
        [SerializeField] private AudioClip urgentWarningSound;

        [Header("Timer Warning Settings")]
        [SerializeField] private float warningThreshold = 30f;
        [SerializeField] private float dangerThreshold = 10f;
        [SerializeField] private float criticalThreshold = 5f;
        [SerializeField] private float timerEnlargeScale = 1.5f;

        [Header("Timer Colors")]
        [SerializeField] private Color normalTimerColor = Color.white;
        [SerializeField] private Color warningTimerColor = new Color(1f, 0.85f, 0f);
        [SerializeField] private Color dangerTimerColor = new Color(1f, 0.3f, 0.3f);
        [SerializeField] private Color borderGlowColor = new Color(1f, 0f, 0f, 0.5f);

        [Header("Gem Animation")]
        [SerializeField] private RectTransform gemIconTransform;
        [SerializeField] private Text gemCountText;
        [SerializeField] private GameObject gemAcquisitionPrefab;
        [SerializeField] private Transform gemAnimationTarget;
        [SerializeField] private ParticleSystem gemSparkleParticles;
        [SerializeField] private AudioClip gemCollectSound;

        [Header("Round Transition")]
        [SerializeField] private CanvasGroup roundTransitionPanel;
        [SerializeField] private Text roundTransitionText;
        [SerializeField] private Text roundSubtitleText;
        [SerializeField] private float transitionFadeDuration = 0.5f;
        [SerializeField] private float transitionDisplayDuration = 1.5f;

        [Header("Toast Messages")]
        [SerializeField] private CanvasGroup toastPanel;
        [SerializeField] private Text toastText;
        [SerializeField] private float toastDuration = 2f;

        [Header("Control Hints")]
        [SerializeField] private GameObject controlHintsPanel;
        [SerializeField] private Text rotateYHintText;
        [SerializeField] private Text rotateXHintText;
        [SerializeField] private Text rotateZHintText;

        private GameManager gameManager;
        private UIManager uiManager;
        private Vector3 originalTimerScale;
        private bool isHeartbeatActive = false;
        private bool hasShown30SecWarning = false;
        private bool hasShown10SecWarning = false;
        private Coroutine currentTimerAnimation;

        public event Action OnRoundTransitionComplete;

        private void Awake()
        {
            if (timerText != null)
            {
                originalTimerScale = timerText.transform.localScale;
            }
        }

        private void Start()
        {
            gameManager = GameManager.Instance;
            uiManager = FindObjectOfType<UIManager>();

            if (gameManager != null)
            {
                gameManager.OnTimeChanged += OnTimeChanged;
                gameManager.OnGameStateChanged += OnGameStateChanged;
            }

            if (uiManager != null)
            {
                uiManager.OnGemsChanged += OnGemsChanged;
            }

            InitializeHUD();
        }

        private void OnDestroy()
        {
            if (gameManager != null)
            {
                gameManager.OnTimeChanged -= OnTimeChanged;
                gameManager.OnGameStateChanged -= OnGameStateChanged;
            }

            if (uiManager != null)
            {
                uiManager.OnGemsChanged -= OnGemsChanged;
            }
        }

        private void InitializeHUD()
        {
            if (screenBorderGlow != null)
            {
                screenBorderGlow.gameObject.SetActive(false);
            }

            if (roundTransitionPanel != null)
            {
                roundTransitionPanel.alpha = 0f;
                roundTransitionPanel.gameObject.SetActive(false);
            }

            if (toastPanel != null)
            {
                toastPanel.alpha = 0f;
                toastPanel.gameObject.SetActive(false);
            }

            UpdateControlHints();
        }

        private void OnGameStateChanged(GameState newState)
        {
            if (newState == GameState.Playing)
            {
                ResetTimerWarnings();
            }
        }

        private void ResetTimerWarnings()
        {
            hasShown30SecWarning = false;
            hasShown10SecWarning = false;
            isHeartbeatActive = false;

            if (timerText != null)
            {
                timerText.transform.localScale = originalTimerScale;
                timerText.color = normalTimerColor;
            }

            if (screenBorderGlow != null)
            {
                screenBorderGlow.gameObject.SetActive(false);
            }
        }

        private void OnTimeChanged(float remainingTime)
        {
            UpdateTimerWarningEffects(remainingTime);
        }

        private void UpdateTimerWarningEffects(float remainingTime)
        {
            if (remainingTime <= 30f && remainingTime > 10f && !hasShown30SecWarning)
            {
                hasShown30SecWarning = true;
                ShowToast("30 seconds remaining!");
                SetTimerColor(warningTimerColor);
            }
            else if (remainingTime <= 10f && remainingTime > 5f && !hasShown10SecWarning)
            {
                hasShown10SecWarning = true;
                ShowToast("Time is running out!");
                SetTimerColor(dangerTimerColor);
                ActivateBorderGlow();
            }
            else if (remainingTime <= 5f && remainingTime > 0f)
            {
                if (!isHeartbeatActive)
                {
                    StartHeartbeatAnimation();
                }

                PlayCountdownTick(remainingTime);
            }

            if (timerBackgroundImage != null && remainingTime <= dangerThreshold)
            {
                float pulseValue = Mathf.PingPong(Time.time * 4f, 1f);
                Color bgColor = Color.Lerp(Color.clear, dangerTimerColor * 0.3f, pulseValue);
                timerBackgroundImage.color = bgColor;
            }
        }

        private void SetTimerColor(Color color)
        {
            if (timerText != null)
            {
                timerText.color = color;
            }
        }

        private void ActivateBorderGlow()
        {
            if (screenBorderGlow == null) return;

            screenBorderGlow.gameObject.SetActive(true);
            screenBorderGlow.color = borderGlowColor;
            StartCoroutine(PulseBorderGlow());
        }

        private IEnumerator PulseBorderGlow()
        {
            while (screenBorderGlow != null && screenBorderGlow.gameObject.activeSelf)
            {
                float pulse = Mathf.PingPong(Time.time * 2f, 1f);
                Color glowColor = borderGlowColor;
                glowColor.a = pulse * 0.6f;
                screenBorderGlow.color = glowColor;
                yield return null;
            }
        }

        private void StartHeartbeatAnimation()
        {
            isHeartbeatActive = true;

            if (currentTimerAnimation != null)
            {
                StopCoroutine(currentTimerAnimation);
            }

            currentTimerAnimation = StartCoroutine(HeartbeatTimerAnimation());
        }

        private IEnumerator HeartbeatTimerAnimation()
        {
            while (isHeartbeatActive && timerText != null)
            {
                float beatDuration = 0.15f;

                timerText.transform.localScale = originalTimerScale * timerEnlargeScale;
                yield return new WaitForSeconds(beatDuration);

                timerText.transform.localScale = originalTimerScale;
                yield return new WaitForSeconds(beatDuration);

                timerText.transform.localScale = originalTimerScale * (timerEnlargeScale * 0.8f);
                yield return new WaitForSeconds(beatDuration * 0.5f);

                timerText.transform.localScale = originalTimerScale;
                yield return new WaitForSeconds(0.3f);
            }
        }

        private void PlayCountdownTick(float remainingTime)
        {
            if (warningAudioSource == null || countdownTickSound == null) return;

            int currentSecond = Mathf.CeilToInt(remainingTime);
            int previousSecond = Mathf.CeilToInt(remainingTime + Time.deltaTime);

            if (currentSecond != previousSecond && currentSecond <= 5 && currentSecond > 0)
            {
                warningAudioSource.PlayOneShot(countdownTickSound);
            }
        }

        private void OnGemsChanged(int newGemCount)
        {
            StartCoroutine(AnimateGemAcquisition());
        }

        public void PlayGemAcquisitionAnimation(int gemAmount, Vector3 worldStartPosition)
        {
            StartCoroutine(GemFlyAnimation(gemAmount, worldStartPosition));
        }

        private IEnumerator AnimateGemAcquisition()
        {
            if (gemIconTransform == null) yield break;

            Vector3 originalScale = gemIconTransform.localScale;

            gemIconTransform.localScale = originalScale * 1.5f;

            if (gemSparkleParticles != null)
            {
                gemSparkleParticles.Play();
            }

            if (warningAudioSource != null && gemCollectSound != null)
            {
                warningAudioSource.PlayOneShot(gemCollectSound);
            }

            float elapsed = 0f;
            float duration = 0.3f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float bounceT = 1f - Mathf.Pow(1f - t, 3f);
                gemIconTransform.localScale = Vector3.Lerp(originalScale * 1.5f, originalScale, bounceT);
                yield return null;
            }

            gemIconTransform.localScale = originalScale;
        }

        private IEnumerator GemFlyAnimation(int gemAmount, Vector3 worldStartPosition)
        {
            if (gemAcquisitionPrefab == null || gemAnimationTarget == null) yield break;

            for (int i = 0; i < Mathf.Min(gemAmount, 5); i++)
            {
                GameObject gemObj = Instantiate(gemAcquisitionPrefab, transform);
                RectTransform gemRect = gemObj.GetComponent<RectTransform>();

                if (gemRect != null)
                {
                    Vector2 screenPos = Camera.main.WorldToScreenPoint(worldStartPosition);
                    gemRect.position = screenPos;

                    Vector3 randomOffset = new Vector3(
                        UnityEngine.Random.Range(-50f, 50f),
                        UnityEngine.Random.Range(-50f, 50f),
                        0f
                    );

                    StartCoroutine(FlyGemToTarget(gemRect, gemAnimationTarget.position + randomOffset));
                }

                yield return new WaitForSeconds(0.1f);
            }
        }

        private IEnumerator FlyGemToTarget(RectTransform gemRect, Vector3 targetPosition)
        {
            Vector3 startPos = gemRect.position;
            float elapsed = 0f;
            float duration = 0.6f;

            Vector3 controlPoint = (startPos + targetPosition) / 2f + Vector3.up * 100f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                float smoothT = t * t * (3f - 2f * t);

                Vector3 p0 = Vector3.Lerp(startPos, controlPoint, smoothT);
                Vector3 p1 = Vector3.Lerp(controlPoint, targetPosition, smoothT);
                gemRect.position = Vector3.Lerp(p0, p1, smoothT);

                float scale = 1f - (smoothT * 0.5f);
                gemRect.localScale = Vector3.one * scale;

                yield return null;
            }

            Destroy(gemRect.gameObject);
            StartCoroutine(AnimateGemAcquisition());
        }

        public void ShowRoundTransition(int roundNumber, int totalRounds, string subtitle = "")
        {
            StartCoroutine(PlayRoundTransition(roundNumber, totalRounds, subtitle));
        }

        private IEnumerator PlayRoundTransition(int roundNumber, int totalRounds, string subtitle)
        {
            if (roundTransitionPanel == null) yield break;

            roundTransitionPanel.gameObject.SetActive(true);

            if (roundTransitionText != null)
            {
                roundTransitionText.text = $"Round {roundNumber}/{totalRounds}";
            }

            if (roundSubtitleText != null)
            {
                roundSubtitleText.text = subtitle;
            }

            float elapsed = 0f;
            while (elapsed < transitionFadeDuration)
            {
                elapsed += Time.deltaTime;
                roundTransitionPanel.alpha = elapsed / transitionFadeDuration;
                yield return null;
            }
            roundTransitionPanel.alpha = 1f;

            if (roundTransitionText != null)
            {
                StartCoroutine(ScaleTextPop(roundTransitionText.transform));
            }

            yield return new WaitForSeconds(transitionDisplayDuration);

            elapsed = 0f;
            while (elapsed < transitionFadeDuration)
            {
                elapsed += Time.deltaTime;
                roundTransitionPanel.alpha = 1f - (elapsed / transitionFadeDuration);
                yield return null;
            }
            roundTransitionPanel.alpha = 0f;

            roundTransitionPanel.gameObject.SetActive(false);
            OnRoundTransitionComplete?.Invoke();
        }

        private IEnumerator ScaleTextPop(Transform textTransform)
        {
            Vector3 originalScale = textTransform.localScale;
            Vector3 targetScale = originalScale * 1.2f;

            float elapsed = 0f;
            float duration = 0.2f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                textTransform.localScale = Vector3.Lerp(originalScale, targetScale, t);
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                textTransform.localScale = Vector3.Lerp(targetScale, originalScale, t);
                yield return null;
            }

            textTransform.localScale = originalScale;
        }

        public void ShowToast(string message)
        {
            StartCoroutine(DisplayToast(message));
        }

        private IEnumerator DisplayToast(string message)
        {
            if (toastPanel == null || toastText == null) yield break;

            toastText.text = message;
            toastPanel.gameObject.SetActive(true);

            float elapsed = 0f;
            float fadeDuration = 0.3f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                toastPanel.alpha = elapsed / fadeDuration;
                yield return null;
            }
            toastPanel.alpha = 1f;

            yield return new WaitForSeconds(toastDuration);

            elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                toastPanel.alpha = 1f - (elapsed / fadeDuration);
                yield return null;
            }
            toastPanel.alpha = 0f;

            toastPanel.gameObject.SetActive(false);
        }

        public void ShowControlHints(bool show)
        {
            if (controlHintsPanel != null)
            {
                controlHintsPanel.SetActive(show);
            }
        }

        private void UpdateControlHints()
        {
            if (rotateYHintText != null)
            {
                rotateYHintText.text = "[Q/E] Y-Axis Rotation";
            }

            if (rotateXHintText != null)
            {
                rotateXHintText.text = "[R] X-Axis Rotation";
            }

            if (rotateZHintText != null)
            {
                rotateZHintText.text = "[F] Z-Axis Rotation";
            }
        }

        public void PlayPuzzleCompleteEffect()
        {
            StartCoroutine(PuzzleCompleteSequence());
        }

        private IEnumerator PuzzleCompleteSequence()
        {
            ShowToast("Puzzle Complete!");

            if (gemSparkleParticles != null)
            {
                gemSparkleParticles.Play();
            }

            yield return new WaitForSeconds(0.5f);
        }
    }
}
