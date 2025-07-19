// File: Data/SaveLoad/EncryptedJsonUtility.cs
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace PlexiPark.Data.SaveLoad
{
    public static class EncryptedJsonUtility
    {
        // Key and IV can be generated and stored securely later. These are just placeholders for now.
        private static readonly byte[] Key = Encoding.UTF8.GetBytes("ppark_AES_key_128b").PadRight(16); // 16 bytes
        private static readonly byte[] IV = Encoding.UTF8.GetBytes("ppark_AES_iv__128b").PadRight(16);   // 16 bytes

        public static void Save<T>(string path, T data)
        {
            try
            {
                string json = JsonUtility.ToJson(data, true);
                byte[] encrypted = EncryptStringToBytes_Aes(json, Key, IV);
                File.WriteAllBytes(path, encrypted);
                Debug.Log($"🔐 Encrypted save written to: {path}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Failed to save encrypted JSON: {ex.Message}");
            }
        }

        public static T Load<T>(string path)
        {
            try
            {
                if (!File.Exists(path)) return default;
                byte[] encrypted = File.ReadAllBytes(path);
                string json = DecryptStringFromBytes_Aes(encrypted, Key, IV);
                return JsonUtility.FromJson<T>(json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Failed to load encrypted JSON: {ex.Message}");
                return default;
            }
        }

        // AES helpers
        private static byte[] EncryptStringToBytes_Aes(string plainText, byte[] key, byte[] iv)
        {
            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;

            using var ms = new MemoryStream();
            using var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write);
            using var sw = new StreamWriter(cs);
            sw.Write(plainText);

            return ms.ToArray();
        }

        private static string DecryptStringFromBytes_Aes(byte[] cipherText, byte[] key, byte[] iv)
        {
            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;

            using var ms = new MemoryStream(cipherText);
            using var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read);
            using var sr = new StreamReader(cs);
            return sr.ReadToEnd();
        }

        private static byte[] PadRight(this byte[] input, int length)
        {
            if (input.Length >= length) return input;
            var result = new byte[length];
            Array.Copy(input, result, input.Length);
            return result;
        }
    }
}
