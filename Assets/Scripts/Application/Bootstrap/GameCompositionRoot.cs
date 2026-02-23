using UnityEngine;
using System;
using Ubongo.Infrastructure.Settings;

namespace Ubongo.Application.Bootstrap
{
    public static class GameBoardFactory
    {
        public static GameBoard ResolveOrCreate(GameBoard existingBoard = null)
        {
            GameBoard board = existingBoard;
            if (board == null)
            {
                GameObject boardObject = new GameObject("GameBoard");
                board = boardObject.AddComponent<GameBoard>();
            }

            EnsureConstructed(board);
            return board;
        }

        public static void EnsureConstructed(GameBoard board)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            if (board.IsConstructed)
            {
                return;
            }

            board.Construct(BoardRuntimeServices.CreateDefault());
        }
    }

    /// <summary>
    /// Lightweight runtime composition root for wiring root-level services.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class GameCompositionRoot : MonoBehaviour
    {
        private static GameCompositionRoot _instance;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning($"[{nameof(GameCompositionRoot)}] Duplicate root detected. Destroying '{name}'.");
                Destroy(gameObject);
                return;
            }

            _instance = this;

            ISettingsStore settingsStore = new PlayerPrefsSettingsStore();
            GameManager.Instance.Initialize(settingsStore);

            GameBoard existingBoard = FindAnyObjectByType<GameBoard>();
            GameBoardFactory.ResolveOrCreate(existingBoard);
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
    }
}
