using UnityEngine;
using System;
using Ubongo.Infrastructure.Settings;
using Ubongo.Systems;

namespace Ubongo.Application.Bootstrap
{
    /// <summary>
    /// Lightweight runtime composition root for wiring root-level services.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class GameCompositionRoot : MonoBehaviour
    {
        private static GameCompositionRoot _instance;

        [SerializeField] private GameManager gameManager;
        [SerializeField] private RoundManager roundManager;
        [SerializeField] private GemSystem gemSystem;
        [SerializeField] private DifficultySystem difficultySystem;
        [SerializeField] private TiebreakerManager tiebreakerManager;
        [SerializeField] private InputManager inputManager;
        [SerializeField] private LevelGenerator levelGenerator;
        [SerializeField] private UIManager uiManager;
        [SerializeField] private GameBoard gameBoard;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning($"[{nameof(GameCompositionRoot)}] Duplicate root detected. Destroying '{name}'.");
                Destroy(gameObject);
                return;
            }

            _instance = this;

            ResolveRuntimeGraphOrThrow();
            EnsureGameBoardConstructedOrThrow();
            WireRuntimeDependencies();

            ISettingsStore settingsStore = new PlayerPrefsSettingsStore();
            gameManager.Initialize(settingsStore);
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        private void ResolveRuntimeGraphOrThrow()
        {
            if (!TryResolveRequiredComponentsInOrder())
            {
                throw new InvalidOperationException($"[{nameof(GameCompositionRoot)}] Runtime graph validation failed.");
            }
        }

        private bool TryResolveRequiredComponentsInOrder()
        {
            Func<bool>[] resolvers =
            {
                () => TryResolveRequiredComponent(ref gameManager),
                () => TryResolveRequiredComponent(ref roundManager),
                () => TryResolveRequiredComponent(ref gemSystem),
                () => TryResolveRequiredComponent(ref difficultySystem),
                () => TryResolveRequiredComponent(ref tiebreakerManager),
                () => TryResolveRequiredComponent(ref inputManager),
                () => TryResolveRequiredComponent(ref levelGenerator),
                () => TryResolveRequiredComponent(ref uiManager),
                () => TryResolveRequiredComponent(ref gameBoard)
            };

            for (int i = 0; i < resolvers.Length; i++)
            {
                if (!resolvers[i]())
                {
                    return false;
                }
            }

            return true;
        }

        private bool TryResolveRequiredComponent<T>(ref T component) where T : Component
        {
            T[] matches = UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (matches.Length != 1)
            {
                Debug.LogError($"[{nameof(GameCompositionRoot)}] Expected exactly one {typeof(T).Name} in scene, but found {matches.Length}.");
                return false;
            }

            component = matches[0];
            return true;
        }

        private void EnsureGameBoardConstructedOrThrow()
        {
            if (gameBoard == null)
            {
                throw new InvalidOperationException($"[{nameof(GameCompositionRoot)}] GameBoard dependency is missing.");
            }

            if (gameBoard.IsConstructed)
            {
                return;
            }

            try
            {
                gameBoard.Construct(BoardRuntimeServices.CreateDefault());
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException(
                    $"[{nameof(GameCompositionRoot)}] Failed to construct GameBoard: {exception.Message}",
                    exception);
            }
        }

        private void WireRuntimeDependencies()
        {
            roundManager.ConfigureRuntimeDependencies(difficultySystem, gemSystem);
            gameManager.ConfigureRuntimeDependencies(
                gemSystem,
                roundManager,
                difficultySystem,
                tiebreakerManager,
                levelGenerator,
                gameBoard,
                inputManager);

            uiManager.ConfigureRuntimeDependencies(gameManager);
            ConfigureOptionalUiDependencies();
        }

        private void ConfigureOptionalUiDependencies()
        {
            GameHUD[] huds = UnityEngine.Object.FindObjectsByType<GameHUD>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < huds.Length; i++)
            {
                GameHUD hud = huds[i];
                if (hud == null)
                {
                    continue;
                }

                hud.ConfigureRuntimeDependencies(gameManager, uiManager);
            }

            ResultPanel[] resultPanels = UnityEngine.Object.FindObjectsByType<ResultPanel>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < resultPanels.Length; i++)
            {
                ResultPanel resultPanel = resultPanels[i];
                if (resultPanel == null)
                {
                    continue;
                }

                resultPanel.ConfigureRuntimeDependencies(gameManager);
            }

            DebugPanel[] debugPanels = UnityEngine.Object.FindObjectsByType<DebugPanel>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < debugPanels.Length; i++)
            {
                DebugPanel debugPanel = debugPanels[i];
                if (debugPanel == null)
                {
                    continue;
                }

                debugPanel.ConfigureRuntimeDependencies(inputManager);
            }
        }
    }
}
