using System.IO;
using NUnit.Framework;
using UnityEngine;
using PlexiPark.Managers;
using PlexiPark.Data;    

public class PlayerDataManagerTests
{
    private string saveFilePath;

    [SetUp]
    public void SetUp()
    {
        // Ensure no lingering save file
        saveFilePath = Path.Combine(Application.persistentDataPath, "playerdata.json");
        if (File.Exists(saveFilePath))
            File.Delete(saveFilePath);
    }

    [Test]
    public void LoadPlayerData_WhenNoSaveFile_InitializesDefaults()
    {
        // Arrange: fresh GameObject
        var go = new GameObject("PDM");
        var pdm = go.AddComponent<PlayerDataManager>();

        // Act: Awake() should have run in AddComponent
        // Assert default values
        Assert.AreEqual("NewPlayer", pdm.PlayerName, "Default PlayerName mismatch");        // :contentReference[oaicite:0]{index=0}
        Assert.IsEmpty(pdm.CurrentParkID, "CurrentParkID should start empty");
        Assert.AreEqual(0, pdm.ReputationScore, "ReputationScore should start at 0");
        Assert.IsEmpty(pdm.Habits, "Habits list should start empty");
        Assert.IsEmpty(pdm.PlacedObjects, "PlacedObjects list should start empty");
        Assert.IsEmpty(pdm.Achievements, "Achievements list should start empty");

        Object.DestroyImmediate(go);
    }

    [Test]
    public void SaveThenLoad_PersistsCollectionsCorrectly()
    {
        // Arrange: create and populate
        var go1 = new GameObject("PDM1");
        var pdm1 = go1.AddComponent<PlayerDataManager>();

        // Add one of each
        pdm1.Habits.Add(new HabitData { HabitID = "H1" });
        pdm1.PlacedObjects.Add(new PlacedObject { ObjectID = "O1", GridPosition = new Vector2Int(2,3), Rotation = Quaternion.Euler(0,45,0) });
        pdm1.Achievements.Add(new AchievementData { AchievementID = "A1", Title = "Test", TargetValue = 1f });

        pdm1.SavePlayerData();
        Object.DestroyImmediate(go1);

        // Act: new instance should load that file
        var go2 = new GameObject("PDM2");
        var pdm2 = go2.AddComponent<PlayerDataManager>();

        // Assert that our entries survived serialization
        Assert.AreEqual(1, pdm2.Habits.Count, "Habit count mismatch after load");               // :contentReference[oaicite:1]{index=1}
        Assert.AreEqual("H1", pdm2.Habits[0].HabitID);

        Assert.AreEqual(1, pdm2.PlacedObjects.Count, "PlacedObject count mismatch after load");  // :contentReference[oaicite:2]{index=2}
        Assert.AreEqual("O1", pdm2.PlacedObjects[0].ObjectID);

        Assert.AreEqual(1, pdm2.Achievements.Count, "Achievement count mismatch after load");    // :contentReference[oaicite:3]{index=3}
        Assert.AreEqual("A1", pdm2.Achievements[0].AchievementID);

        Object.DestroyImmediate(go2);
    }
}
