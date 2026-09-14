using HumanBodyExplorer.Core;
using NUnit.Framework;

namespace HumanBodyExplorer.Tests
{
    public class SecurityManagerTests
    {
        [Test]
        public void Encrypt_ThenDecrypt_RoundTrips()
        {
            var key = SecurityManager.DeriveKeyFromHardwareId();
            var security = new SecurityManager(key);

            string plainText = "{\"event\":\"quiz_completed\",\"score\":95}";
            string encrypted = security.Encrypt(plainText);
            string decrypted = security.Decrypt(encrypted);

            Assert.AreEqual(plainText, decrypted);
            Assert.AreNotEqual(plainText, encrypted);
        }

        [Test]
        public void Encrypt_SamePlaintextTwice_ProducesDifferentCiphertext()
        {
            var key = SecurityManager.DeriveKeyFromHardwareId();
            var security = new SecurityManager(key);

            string a = security.Encrypt("same text");
            string b = security.Encrypt("same text");

            Assert.AreNotEqual(a, b, "Fresh IV per call should make repeated encryptions of the same plaintext differ.");
        }

        [Test]
        public void Constructor_WrongKeyLength_Throws()
        {
            Assert.Throws<System.ArgumentException>(() => new SecurityManager(new byte[16]));
        }
    }
}
