//Assets/Managers/HabitIntegrationManager.cs

using UnityEngine;
using System.Collections.Generic;
using PlexiPark.Core.SharedEnums;
using System;

namespace PlexiPark.Managers
{
    public class HabitIntegrationManager : MonoBehaviour
    {
        public static HabitIntegrationManager Instance { get; private set; }

        private List<HabitData> currentHabits = new();

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            currentHabits = EncryptedHabitStorage.LoadHabits();
        }

        public IReadOnlyList<HabitData> GetHabits()
        {
            return currentHabits.AsReadOnly();
        }

        public void AddHabit(HabitData newHabit)
        {
            currentHabits.Add(newHabit);
            SaveHabits();
        }

        public void MarkHabitCompleted(string habitID)
        {
            HabitData habit = currentHabits.Find(h => h.HabitID == habitID);
            if (habit == null)
            {
                Debug.LogWarning($"⚠️ Habit '{habitID}' not found.");
                return;
            }

            habit.CurrentStreak++;
            habit.TotalCompletions++;

            // Trigger rewards (Phase 1 rewards only)
            //   FinanceManager.Instance?.AddRealWorldBonus(100); // Example value
            //   ReputationManager.Instance?.IncreaseReputation(1);
            //   VisitorManager.Instance?.TemporarilyBoostVisitorType(VisitorType.Walker, 10f);

            SaveHabits();
            Debug.Log($"✅ Habit '{habit.Name}' completed.");
        }

        public void MarkHabitFailed(string habitID)
        {
            HabitData habit = currentHabits.Find(h => h.HabitID == habitID);
            if (habit == null)
            {
                Debug.LogWarning($"⚠️ Habit '{habitID}' not found.");
                return;
            }

            habit.CurrentStreak = 0;
            habit.TotalFailures++;

            //   FinanceManager.Instance?.ApplyRealWorldPenalty(50); // Example value
            //   ReputationManager.Instance?.DecreaseReputation(1);

            SaveHabits();
            Debug.Log($"❌ Habit '{habit.Name}' failed.");
        }

        public void SaveHabits()
        {
            EncryptedHabitStorage.SaveHabits(currentHabits);
        }

        public void LoadHabits()
        {
            currentHabits = EncryptedHabitStorage.LoadHabits();
        }

        // 🔄 Phase 2: Called by HealthConnectManager.cs
        // public void ProcessAutomatedHabit(HealthData data)
        // {
        //     // Example: if step count crosses a threshold
        //     if (data.steps >= 1000)
        //     {
        //         FinanceManager.Instance?.AddRealWorldBonus(150);
        //         VisitorManager.Instance?.TemporarilyBoostVisitorType(VisitorType.Hiker, 10f);
        //         ReputationManager.Instance?.IncreaseReputation(2);
        //         Debug.Log("📲 Health data reward triggered via HealthConnectManager.");
        //     }
        // }
    }
}