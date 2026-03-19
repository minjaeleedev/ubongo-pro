using NUnit.Framework;
using UnityEngine;
using Ubongo.Infrastructure.Settings;

namespace Ubongo.Tests.EditMode.Infrastructure.Settings
{
    public class PlayerPrefsSettingsStoreTests
    {
        private const string TestSettingsKey = "Ubongo.Tests.EditMode.SettingsStore.BoolKey";

        [SetUp]
        public void SetUp()
        {
            PlayerPrefs.DeleteKey(TestSettingsKey);
        }

        [TearDown]
        public void TearDown()
        {
            PlayerPrefs.DeleteKey(TestSettingsKey);
        }

        [Test]
        public void PlayerPrefsSettingsStore_GetSetBool_Works()
        {
            ISettingsStore store = new PlayerPrefsSettingsStore();

            Assert.IsFalse(store.GetBool(TestSettingsKey, false));

            store.SetBool(TestSettingsKey, true);
            store.Save();
            Assert.IsTrue(store.GetBool(TestSettingsKey, false));

            store.SetBool(TestSettingsKey, false);
            store.Save();
            Assert.IsFalse(store.GetBool(TestSettingsKey, true));
        }
    }
}
