using System;

namespace IronXNestCommand.Progression
{
    /// <summary>
    /// Speicherbare Progression- und Statistik-Daten des Spielers.
    /// </summary>
    public class ProgressionData
    {
        public int TotalXP { get; set; } = 0;
        public int CurrentRankLevel { get; set; } = 1;
        public int MissionsCompleted { get; set; } = 0;
        public int ShellsFired { get; set; } = 0;
        public int DirectHits { get; set; } = 0;
        public int CounterBatteryKills { get; set; } = 0;

        public float AccuracyPercentage => ShellsFired > 0 ? ((float)DirectHits / ShellsFired) * 100f : 0f;
    }
}
