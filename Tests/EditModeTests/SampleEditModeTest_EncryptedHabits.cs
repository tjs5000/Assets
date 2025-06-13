using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using PlexiPark.Data;

public class SampleEditModeTest_EncryptedHabits
{
    private readonly string testHabitID = "habit_test_123";
    string filePath = Path.Combine(Application.persistentDataPath, "habit_data.dat");
    [Test]
    public void EncryptedHabitStorage_RoundTripIntegrity_IsPreserved()
    {
        if (File.Exists(filePath))
            File.Delete(filePath);

        // Arrange
        List<HabitData> originalList = new List<HabitData>
        {
            new HabitData
            {
                HabitID = testHabitID,
                Name = "Test Habit",
                Description = "A habit for testing",
                CurrentStreak = 3,
                TotalCompletions = 5,
                TotalFailures = 1
            }
        };

        // Act
        EncryptedHabitStorage.SaveHabits(originalList);
        List<HabitData> loadedList = EncryptedHabitStorage.LoadHabits();

        // Assert
        Assert.IsNotNull(loadedList);
        Assert.AreEqual(1, loadedList.Count);

        HabitData loaded = loadedList[0];
        Assert.AreEqual(originalList[0].HabitID, loaded.HabitID);
        Assert.AreEqual(originalList[0].Name, loaded.Name);
        Assert.AreEqual(originalList[0].Description, loaded.Description);
        Assert.AreEqual(originalList[0].CurrentStreak, loaded.CurrentStreak);
        Assert.AreEqual(originalList[0].TotalCompletions, loaded.TotalCompletions);
        Assert.AreEqual(originalList[0].TotalFailures, loaded.TotalFailures);
    }

    [Test]
    public void EncryptedHabitStorage_File_IsNotPlainTextJSON()
    {
        if (File.Exists(filePath))
            File.Delete(filePath);

        // Arrange – ensure a file exists
        List<HabitData> dummyList = new List<HabitData>
    {
        new HabitData
        {
            HabitID = "dummy123",
            Name = "Dummy",
            Description = "Encrypt me",
            CurrentStreak = 1,
            TotalCompletions = 1,
            TotalFailures = 0
        }
    };
        EncryptedHabitStorage.SaveHabits(dummyList);

        // Act
        filePath = Path.Combine(Application.persistentDataPath, "habit_data.dat");
        byte[] fileBytes = File.ReadAllBytes(filePath);
        string fileText = System.Text.Encoding.UTF8.GetString(fileBytes);

        // Assert – check that sensitive strings are not visible
        Assert.IsFalse(fileText.Contains("dummy123"), "⚠️ Habit ID is readable – encryption failed.");
        Assert.IsFalse(fileText.Contains("Encrypt me"), "⚠️ Description is readable – encryption failed.");
        Assert.IsTrue(fileBytes.Length > 0, "⚠️ File was not written.");
    }
    [TearDown]
    public void CleanUpFile()
    {
        if (File.Exists(filePath))
            File.Delete(filePath);
    }
}
