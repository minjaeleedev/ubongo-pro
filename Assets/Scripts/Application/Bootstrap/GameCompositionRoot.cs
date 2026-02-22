using UnityEngine;
using Ubongo.Infrastructure.Settings;

namespace Ubongo.Application.Bootstrap
{
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
