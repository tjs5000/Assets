//Assets/Managers/PlayerDataManager.cs
/// <summary>
/// Manages all persistent player data: profile, habits, park state, visitor history, placed objects, and achievements.
/// Implements JSON save/load per TDD §4.1.
/// </summary>

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using PlexiPark.Data;
using PlexiPark.Data.SaveLoad;
using PlexiPark.Core.SharedEnums;

namespace PlexiPark.Managers
{
    public class PlayerDataManager : MonoBehaviour
    {
        public static PlayerDataManager Instance { get; private set; }

        // --- Player Profile ---
        public string PlayerID { get; private set; }
        public string PlayerName { get; private set; }
        public string CurrentParkID { get; private set; }
        public int ReputationScore { get; private set; }

        // --- Habit Tracking ---
        public List<HabitData> Habits { get; private set; } = new List<HabitData>();

        // --- Park Save Data ---
        public string ParkID;
        public string ParkName;
        public ParkType CurrentParkType;
        public int CurrentBalance;
        public int MonthlyIncome;
        public int MonthlyExpenses;
        public float OverallParkRating;

        // Visitor counts by month (serialized as list of entries)
        private Dictionary<string, int> visitorCountsByMonth = new Dictionary<string, int>();
        public IReadOnlyDictionary<string, int> VisitorCountsByMonth => visitorCountsByMonth;

        public List<PlacedObject> PlacedObjects { get; private set; } = new List<PlacedObject>();
        public List<AchievementData> Achievements { get; private set; } = new List<AchievementData>();

        private string path = Application.persistentDataPath + "/playerdata.secure";

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                LoadPlayerData();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Call to save all player data out to disk as JSON.
        /// </summary>
        public void SavePlayerData()
        {
            var container = new PlayerDataContainer
            {
                PlayerID = PlayerID,
                PlayerName = PlayerName,
                CurrentParkID = CurrentParkID,
                ReputationScore = ReputationScore,
                Habits = Habits,
                ParkID = ParkID,
                ParkName = ParkName,
                CurrentParkType = CurrentParkType,
                CurrentBalance = CurrentBalance,
                MonthlyIncome = MonthlyIncome,
                MonthlyExpenses = MonthlyExpenses,
                OverallParkRating = OverallParkRating,
                VisitorCounts = ConvertVisitorDictToList(),
                PlacedObjects = PlacedObjects,
                Achievements = Achievements
            };

            EncryptedJsonUtility.Save(path, container);
            Debug.Log($"Player data saved to {path}");
        }

        /// <summary>
        /// Loads player data from disk if present; otherwise initializes defaults.
        /// </summary>
        public void LoadPlayerData()
        {
            if (File.Exists(path))
            {
                PlayerDataContainer container = EncryptedJsonUtility.Load<PlayerDataContainer>(path);
                if (container != null)
                {
                    PlayerID = container.PlayerID;
                    PlayerName = container.PlayerName;
                    CurrentParkID = container.CurrentParkID;
                    ReputationScore = container.ReputationScore;
                    Habits = container.Habits ?? new List<HabitData>();
                    ParkID = container.ParkID;
                    ParkName = container.ParkName;
                    CurrentParkType = container.CurrentParkType;
                    CurrentBalance = container.CurrentBalance;
                    MonthlyIncome = container.MonthlyIncome;
                    MonthlyExpenses = container.MonthlyExpenses;
                    OverallParkRating = container.OverallParkRating;
                    visitorCountsByMonth = ConvertVisitorListToDict(container.VisitorCounts);
                    PlacedObjects = container.PlacedObjects ?? new List<PlacedObject>();
                    Achievements = container.Achievements ?? new List<AchievementData>();

                    Debug.Log("Player data loaded.");
                }
                else
                {
                    InitializeNewPlayer();
                    Debug.Log("No save file found; initialized new player data.");
                }
            }
        }
        private void InitializeNewPlayer()
        {
            PlayerID = Guid.NewGuid().ToString();
            PlayerName = "NewPlayer";
            CurrentParkID = "";
            ReputationScore = 0;
            // Leave other collections default
        }

        #region Visitor Count Helpers
        [Serializable]
        private struct VisitorCountEntry
        {
            public string MonthKey;
            public int Count;
        }

        private List<VisitorCountEntry> ConvertVisitorDictToList()
        {
            var list = new List<VisitorCountEntry>();
            foreach (var kv in visitorCountsByMonth)
                list.Add(new VisitorCountEntry { MonthKey = kv.Key, Count = kv.Value });
            return list;
        }

        private Dictionary<string, int> ConvertVisitorListToDict(List<VisitorCountEntry> list)
        {
            var dict = new Dictionary<string, int>();
            if (list == null) return dict;
            foreach (var entry in list)
                dict[entry.MonthKey] = entry.Count;
            return dict;
        }
        #endregion

        #region Data Container
        [Serializable]
        private class PlayerDataContainer
        {
            public string PlayerID;
            public string PlayerName;
            public string CurrentParkID;
            public int ReputationScore;
            public List<HabitData> Habits;

            public string ParkID;
            public string ParkName;
            public ParkType CurrentParkType;
            public int CurrentBalance;
            public int MonthlyIncome;
            public int MonthlyExpenses;
            public float OverallParkRating;

            public List<VisitorCountEntry> VisitorCounts;
            public List<PlacedObject> PlacedObjects;
            public List<AchievementData> Achievements;
        }
        #endregion
    }
}