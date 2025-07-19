//Assets/Data/SaveLoad/EncryptedHabitStorage.cs

using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using System.Collections.Generic;
using PlexiPark.Core.SharedEnums;

public static class EncryptedHabitStorage
{
    private static readonly string FilePath = Path.Combine(Application.persistentDataPath, "habit_data.dat");
    private static readonly string EncryptionKey = "plexipark-habit-key!"; // ✅ Replace in production

    public static void SaveHabits(List<HabitData> habits)
    {
        string json = JsonUtility.ToJson(new HabitListWrapper { habits = habits });
        byte[] encrypted = EncryptStringToBytes(json, EncryptionKey);
        File.WriteAllBytes(FilePath, encrypted);
        Debug.Log($"✅ Habits saved to: {FilePath}");
    }

    public static List<HabitData> LoadHabits()
    {
        try
        {
            if (!File.Exists(FilePath))
                return new List<HabitData>();

            byte[] encrypted = File.ReadAllBytes(FilePath);
            string json = DecryptStringFromBytes(encrypted, EncryptionKey);
            HabitListWrapper wrapper = JsonUtility.FromJson<HabitListWrapper>(json);
            return wrapper?.habits ?? new List<HabitData>();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"🛑 Failed to load habits: {ex.Message}");
            return new List<HabitData>();
        }
    }


    // --- Encryption Methods ---
    private static byte[] EncryptStringToBytes(string plainText, string key)
    {
        using var aes = Aes.Create();
        aes.Key = GetKeyBytes(key);
        aes.GenerateIV();
        var encryptor = aes.CreateEncryptor();

        using var ms = new MemoryStream();
        ms.Write(aes.IV, 0, aes.IV.Length); // Prepend IV

        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
        using (var sw = new StreamWriter(cs))
        {
            sw.Write(plainText);
        }

        return ms.ToArray();
    }

    private static string DecryptStringFromBytes(byte[] cipherData, string key)
    {
        using var aes = Aes.Create();
        aes.Key = GetKeyBytes(key);

        byte[] iv = new byte[aes.BlockSize / 8];
        Array.Copy(cipherData, iv, iv.Length);
        aes.IV = iv;

        using var ms = new MemoryStream(cipherData, iv.Length, cipherData.Length - iv.Length);
        using var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read);
        using var sr = new StreamReader(cs);
        return sr.ReadToEnd();
    }

    private static byte[] GetKeyBytes(string key)
    {
        using var sha = SHA256.Create();
        return sha.ComputeHash(Encoding.UTF8.GetBytes(key));
    }

    [Serializable]
    private class HabitListWrapper
    {
        public List<HabitData> habits;
    }
}
