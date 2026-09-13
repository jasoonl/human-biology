using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace HumanBodyExplorer.Core
{
    /// <summary>
    /// Encrypts values with AES-256 (keyed off a machine-specific identifier) before
    /// writing them to PlayerPrefs, so local save data/settings can't be trivially
    /// edited by hand.
    /// </summary>
    public static class SecurePrefs
    {
        private static byte[] DeriveKey()
        {
            string seed = SystemInfo.deviceUniqueIdentifier + "HumanBodyExplorer.v1";
            using var sha256 = SHA256.Create();
            return sha256.ComputeHash(Encoding.UTF8.GetBytes(seed));
        }

        private static string Encrypt(string plainText)
        {
            using var aes = Aes.Create();
            aes.Key = DeriveKey();
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor();
            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
            byte[] cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

            byte[] payload = new byte[aes.IV.Length + cipherBytes.Length];
            Buffer.BlockCopy(aes.IV, 0, payload, 0, aes.IV.Length);
            Buffer.BlockCopy(cipherBytes, 0, payload, aes.IV.Length, cipherBytes.Length);

            return Convert.ToBase64String(payload);
        }

        private static string Decrypt(string base64Payload)
        {
            byte[] payload = Convert.FromBase64String(base64Payload);

            using var aes = Aes.Create();
            aes.Key = DeriveKey();

            byte[] iv = new byte[16];
            Buffer.BlockCopy(payload, 0, iv, 0, iv.Length);
            aes.IV = iv;

            byte[] cipherBytes = new byte[payload.Length - iv.Length];
            Buffer.BlockCopy(payload, iv.Length, cipherBytes, 0, cipherBytes.Length);

            using var decryptor = aes.CreateDecryptor();
            byte[] plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
            return Encoding.UTF8.GetString(plainBytes);
        }

        public static void SetString(string key, string value)
        {
            PlayerPrefs.SetString(key, Encrypt(value));
        }

        public static string GetString(string key, string defaultValue = "")
        {
            if (!PlayerPrefs.HasKey(key)) return defaultValue;

            try
            {
                return Decrypt(PlayerPrefs.GetString(key));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SecurePrefs] Failed to decrypt key '{key}': {e.Message}");
                return defaultValue;
            }
        }

        public static void SetFloat(string key, float value)
        {
            SetString(key, value.ToString("R", System.Globalization.CultureInfo.InvariantCulture));
        }

        public static float GetFloat(string key, float defaultValue = 0f)
        {
            string raw = GetString(key, null);
            if (raw == null) return defaultValue;

            return float.TryParse(raw, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out float result)
                ? result
                : defaultValue;
        }

        public static bool HasKey(string key) => PlayerPrefs.HasKey(key);

        public static void DeleteKey(string key) => PlayerPrefs.DeleteKey(key);
    }
}
