using System;
using UnityEngine;

namespace PlexiPark.Data
{
    [Serializable]
    public class HabitData
    {
        public string HabitID;
        public string Name;
        public string Description;
        public int CurrentStreak;
        public int TotalCompletions;
        public int TotalFailures;
    }
}
