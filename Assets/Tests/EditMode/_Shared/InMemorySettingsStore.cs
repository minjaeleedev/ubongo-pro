using Ubongo.Infrastructure.Settings;

namespace Ubongo.Tests.EditMode.Shared
{
    public sealed class InMemorySettingsStore : ISettingsStore
    {
        public bool StoredValue { get; private set; }
        public int SaveCallCount { get; private set; }
        private bool hasStoredValue;

        public bool GetBool(string key, bool defaultValue)
        {
            return hasStoredValue ? StoredValue : defaultValue;
        }

        public void SetBool(string key, bool value)
        {
            StoredValue = value;
            hasStoredValue = true;
        }

        public void Save()
        {
            SaveCallCount++;
        }
    }
}
