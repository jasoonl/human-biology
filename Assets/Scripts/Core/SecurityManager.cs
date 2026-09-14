using System;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace HumanBodyExplorer.Core
{
    /// <summary>
    /// Phase 86: AES-256 encryption for local analytics/telemetry payloads, using
    /// a fresh IV per call (concatenated as [IV][Ciphertext]) to prevent block
    /// analysis attacks across repeated encryptions of similar plaintext.
    /// </summary>
    public class SecurityManager
    {
        private readonly byte[] _key;

        public SecurityManager(byte[] key)
        {
            if (key.Length != 32)
            {
                throw new ArgumentException("AES-256 requires a 32-byte key.", nameof(key));
            }
            _key = key;
        }

        public static byte[] DeriveKeyFromHardwareId()
        {
            using var sha256 = SHA256.Create();
            return sha256.ComputeHash(Encoding.UTF8.GetBytes(SystemInfo.deviceUniqueIdentifier + "HBE.Telemetry.v1"));
        }

        public string Encrypt(string plainText)
        {
            using var aes = Aes.Create();
            aes.Key = _key;
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor();
            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
            byte[] cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

            byte[] payload = new byte[aes.IV.Length + cipherBytes.Length];
            Buffer.BlockCopy(aes.IV, 0, payload, 0, aes.IV.Length);
            Buffer.BlockCopy(cipherBytes, 0, payload, aes.IV.Length, cipherBytes.Length);

            return Convert.ToBase64String(payload);
        }

        public string Decrypt(string base64Payload)
        {
            byte[] payload = Convert.FromBase64String(base64Payload);

            using var aes = Aes.Create();
            aes.Key = _key;

            byte[] iv = new byte[16];
            Buffer.BlockCopy(payload, 0, iv, 0, iv.Length);
            aes.IV = iv;

            byte[] cipherBytes = new byte[payload.Length - iv.Length];
            Buffer.BlockCopy(payload, iv.Length, cipherBytes, 0, cipherBytes.Length);

            using var decryptor = aes.CreateDecryptor();
            byte[] plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
            return Encoding.UTF8.GetString(plainBytes);
        }
    }
}
