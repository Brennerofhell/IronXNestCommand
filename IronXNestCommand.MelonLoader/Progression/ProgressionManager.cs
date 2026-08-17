using System;
using System.Collections.Generic;
using MelonLoader;
using IronXNestCommand.Core;
using IronXNestCommand.Economy;

namespace IronXNestCommand.Progression
{
    public static class ProgressionManager
    {
        public static ProgressionData Data { get; private set; }
        private static readonly List<RankDefinition> Ranks = new List<RankDefinition>();

        public static void Initialize()
        {
            MelonLogger.Msg("[ProgressionManager] Initialisiere Rang- und Erfahrungssystem...");
            SetupRanks();
            Data = SaveManager.LoadProgressionData();
            UpdateRankStatus();
            MelonLogger.Msg($"[ProgressionManager] Geladen: {GetCurrentRank().Title} ({Data.TotalXP} XP)");
        }

        private static void SetupRanks()
        {
            Ranks.Clear();
            Ranks.Add(new RankDefinition(1, "Recruit Operator", 0, "Grundausbildung abgeschlossen.", "1 Loadout-Slot", "Basis Ammo Advisor"));
            Ranks.Add(new RankDefinition(2, "Junior Gunner", 500, "Erste Kampfeinsätze erfolgreich überstanden.", "2 Loadout-Slots", "+5% Treffergenauigkeits-Anzeige"));
            Ranks.Add(new RankDefinition(3, "Qualified Operator", 1500, "Erfahrener Turret-Bediener.", "3 Loadout-Slots", "Erweiterte Ballistik-Prognosen"));
            Ranks.Add(new RankDefinition(4, "Senior Operator", 3500, "Spezialist für schwere Artillerie.", "4 Loadout-Slots", "Schnell-Nachschub Freigabe"));
            Ranks.Add(new RankDefinition(5, "Master Gunner", 7000, "Meisterhafte Präzision auf maximale Distanz.", "5 Loadout-Slots", "Freischaltung von HV-AP Shells"));
            Ranks.Add(new RankDefinition(6, "Nest Commander", 12000, "Befehlshaber über die Verteidigungsstellungen.", "6 Loadout-Slots", "Freischaltung von EMP Shells", "Erhöhtes Intel-Einkommen"));
            Ranks.Add(new RankDefinition(7, "High Command Liaison", 20000, "Direkte Verbindung zum Oberkommando.", "Unbegrenzte Loadouts", "Command Favor Rabatt"));
        }

        public static RankDefinition GetCurrentRank()
        {
            RankDefinition current = Ranks[0];
            foreach (var rank in Ranks)
            {
                if (Data.TotalXP >= rank.RequiredXP)
                {
                    current = rank;
                }
                else
                {
                    break;
                }
            }
            return current;
        }

        public static RankDefinition GetNextRank()
        {
            var current = GetCurrentRank();
            int nextIdx = Ranks.IndexOf(current) + 1;
            if (nextIdx < Ranks.Count)
                return Ranks[nextIdx];
            return null; // Maximaler Rang erreicht
        }

        public static float GetProgressToNextRank()
        {
            var current = GetCurrentRank();
            var next = GetNextRank();
            if (next == null) return 1f;

            int xpInCurrentRank = Data.TotalXP - current.RequiredXP;
            int xpRequiredForNext = next.RequiredXP - current.RequiredXP;
            if (xpRequiredForNext <= 0) return 1f;

            return Math.Clamp((float)xpInCurrentRank / xpRequiredForNext, 0f, 1f);
        }

        public static void AddXP(int amount, string reason = "")
        {
            if (amount <= 0) return;

            var oldRank = GetCurrentRank();
            Data.TotalXP += amount;
            
            var newRank = GetCurrentRank();
            if (newRank.Level > oldRank.Level)
            {
                MelonLogger.Msg($"[ProgressionManager] *** RANGAUFSTIEG! *** Neuer Rang: {newRank.Title}!");
                // Rangaufstiegs-Belohnung in Form von Favor und Intel Points
                CurrencyManager.AddCurrency(CurrencyType.CommandFavor, 1);
                CurrencyManager.AddCurrency(CurrencyType.IntelPoints, 50);
            }
            else
            {
                MelonLogger.Msg($"[ProgressionManager] +{amount} XP erhalten ({reason}). Gesamt: {Data.TotalXP} XP");
            }

            SaveManager.SaveProgressionData(Data);
        }

        public static void RecordMissionFinished(bool victory, int shots, int hits, int counterBattery)
        {
            Data.MissionsCompleted++;
            Data.ShellsFired += shots;
            Data.DirectHits += hits;
            Data.CounterBatteryKills += counterBattery;

            int earnedXP = (victory ? 200 : 50) + (hits * 15) + (counterBattery * 100);
            int earnedIntel = (hits * 2) + (counterBattery * 10);
            int earnedTokens = victory ? 5 : 1;

            MelonLogger.Msg($"[ProgressionManager] Mission beendet. Trefferquote: {Data.AccuracyPercentage:F1}%");
            AddXP(earnedXP, "Missionsabschluss");
            CurrencyManager.AddCurrency(CurrencyType.IntelPoints, earnedIntel);
            CurrencyManager.AddCurrency(CurrencyType.LogisticsTokens, earnedTokens);

            if (hits >= 10 && (float)hits / Math.Max(1, shots) >= 0.7f)
            {
                CurrencyManager.AddCurrency(CurrencyType.CommandFavor, 1);
            }

            SaveManager.SaveProgressionData(Data);
        }

        private static void UpdateRankStatus()
        {
            Data.CurrentRankLevel = GetCurrentRank().Level;
        }

        public static IReadOnlyList<RankDefinition> GetAllRanks() => Ranks;
    }
}
