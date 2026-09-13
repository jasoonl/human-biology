using HumanBodyExplorer.Core;
using NUnit.Framework;

namespace HumanBodyExplorer.Tests
{
    public class SecurePrefsTests
    {
        private const string TestKey = "HBE_Test_SecurePrefs_Key";

        [TearDown]
        public void TearDown()
        {
            SecurePrefs.DeleteKey(TestKey);
        }

        [Test]
        public void SetString_ThenGetString_RoundTrips()
        {
            SecurePrefs.SetString(TestKey, "sensitive-value-123");
            string result = SecurePrefs.GetString(TestKey);
            Assert.AreEqual("sensitive-value-123", result);
        }

        [Test]
        public void SetFloat_ThenGetFloat_RoundTrips()
        {
            SecurePrefs.SetFloat(TestKey, 42.5f);
            float result = SecurePrefs.GetFloat(TestKey);
            Assert.AreEqual(42.5f, result, 0.0001f);
        }

        [Test]
        public void GetString_MissingKey_ReturnsDefault()
        {
            string result = SecurePrefs.GetString("HBE_Nonexistent_Key", "fallback");
            Assert.AreEqual("fallback", result);
        }
    }
}
