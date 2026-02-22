namespace Ubongo.Infrastructure.Settings
{
    /// <summary>
    /// Minimal key-value settings abstraction used by runtime services.
    /// </summary>
    public interface ISettingsStore
    {
        bool GetBool(string key, bool defaultValue);
        void SetBool(string key, bool value);
        void Save();
    }
}
