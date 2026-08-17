using System;
using System.Collections.Generic;
using IronXNestCommand.Core.Logging;
using IronXNestCommand.Core.Paths;
using IronXNestCommand.Host.BepInEx.Core;
using IronXNestCommand.Host.BepInEx.Economy;
using IronXNestCommand.Host.BepInEx.Overlay;

namespace IronXNestCommand.Host.BepInEx.Progression
{
    public static class ProgressionManager
    {
        private static ProgressionData _data;
        private static readonly List<RankDefinition> Ranks = new();

        public static ProgressionData Data => _data ??= new ProgressionData();

        public static void Initialize()
        {
            RegisterRanks();
            _data = SaveManager.LoadJson(ModPaths.ProgressFile, new ProgressionData());
            ModLogger.Info($"[ProgressionManager] Geladen: {Data.TotalXP} XP · Aktueller Rang: {GetCurrentRank().Title} (Lvl {GetCurrentRank().Level})");
        }

        private static void RegisterRanks()
        {
            Ranks.Clear();
            Ranks.Add(new RankDefinition(1, "Rekrut (Trainee Gunner)", 0, "Frisch eingezogener Geschützbediener.", "Standard Munition"));
            Ranks.Add(new RankDefinition(2, "Kanonier (Gunner 3rd Class)", 250, "Erste Gefechtserfahrung gesammelt.", "Schnellere Munitions-Zuteilung"));
            Ranks.Add(new RankDefinition(3, "Oberkanonier (Senior Gunner)", 750, "Zuverlässiger Schütze mit ballistischer Präzision.", "Freischaltung HV-AP Shells"));
            Ranks.Add(new RankDefinition(4, "Feuerleitmeister (Fire Controller)", 1500, "Experte für Richt- und Feuerleitberechnungen.", "Advisor Erweiterte Zielprofile"));
            Ranks.Add(new RankDefinition(5, "Batterieführer (Battery Commander)", 3000, "Befehlshaber über schwere Artillerieeinheiten.", "Freischaltung EMP Disruptor Shells"));
            Ranks.Add(new RankDefinition(6, "Artillerie-Inspektor (Inspector General)", 5500, "Höchste taktische Instanz der Heeresartillerie.", "Vergünstigter Eil-Nachschub"));
            Ranks.Add(new RankDefinition(7, "Festungskommandant (Iron Fortress Master)", 10000, "Legende der Festungs-Artillerie.", "Alle Boni & Munitionseffekte maximiert"));
        }

        public static RankDefinition GetCurrentRank()
        {
            RankDefinition current = Ranks[0];
            foreach (var r in Ranks)
            {
                if (Data.TotalXP >= r.RequiredXP)
                    current = r;
                else
                    break;
            }
            return current;
        }

        public static RankDefinition GetNextRank()
        {
            foreach (var r in Ranks)
            {
                if (Data.TotalXP < r.RequiredXP)
                    return r;
            }
            return null;
        }

        public static float GetProgressToNextRank()
        {
            var cur = GetCurrentRank();
            var next = GetNextRank();
            if (next == null) return 1f;

            float span = next.RequiredXP - cur.RequiredXP;
            if (span <= 0) return 1f;

            float currentInTier = Data.TotalXP - cur.RequiredXP;
            return Math.Clamp(currentInTier / span, 0f, 1f);
        }

        public static void AddXP(int amount, string reason = "")
        {
            if (amount <= 0) return;
            var oldRank = GetCurrentRank();
            Data.TotalXP += amount;
            SaveManager.SaveJson(ModPaths.ProgressFile, Data);

            var newRank = GetCurrentRank();
            if (newRank.Level > oldRank.Level)
            {
                AudioFeedback.PlayLevelUp();
                CommandOverlay.ShowNotification($"★ BEFÖRDERUNG! Neuer Dienstgrad: {newRank.Title} ★", 4.0f);
                ModLogger.Info($"★ BEFÖRDERUNG! Neuer Dienstgrad: {newRank.Title} (Lvl {newRank.Level}) ★");
            }
        }

        public static void RecordMissionFinished(bool victory, int shellsFired, int directHits, int counterBatteryKills)
        {
            Data.MissionsCompleted++;
            Data.ShellsFired += shellsFired;
            Data.DirectHits += directHits;
            Data.CounterBatteryKills += counterBatteryKills;

            int xpEarned = (victory ? 250 : 50) + (directHits * 25) + (counterBatteryKills * 75);
            AddXP(xpEarned, "Missionsabschluss");

            CurrencyManager.AddCurrency(CurrencyType.LogisticsTokens, victory ? 10 : 3);
            CurrencyManager.AddCurrency(CurrencyType.IntelPoints, directHits * 5 + counterBatteryKills * 15);
        }
    }
}
