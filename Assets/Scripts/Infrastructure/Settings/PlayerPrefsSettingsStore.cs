using UnityEngine;

namespace Ubongo.Infrastructure.Settings
{
    /// <summary>
    /// Unity PlayerPrefs-backed runtime settings store.
    /// </summary>
    public sealed class PlayerPrefsSettingsStore : ISettingsStore
    {
        public bool GetBool(string key, bool defaultValue)
        {
            int fallback = defaultValue ? 1 : 0;
            return PlayerPrefs.GetInt(key, fallback) == 1;
        }

        public void SetBool(string key, bool value)
        {
            PlayerPrefs.SetInt(key, value ? 1 : 0);
        }

        public void Save()
        {
            PlayerPrefs.Save();
        }
    }
}
