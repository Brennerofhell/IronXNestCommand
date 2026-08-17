using MelonLoader;
using IronXNestCommand.Core;

namespace IronXNestCommand.Economy
{
    public enum CurrencyType
    {
        IntelPoints,
        LogisticsTokens,
        CommandFavor
    }

    /// <summary>
    /// Verwaltet das Hinzufügen und Ausgeben von Währungen und kommuniziert mit dem SaveManager.
    /// </summary>
    public static class CurrencyManager
    {
        public static CurrencyData CurrentBalances { get; private set; }

        public static void Initialize()
        {
            MelonLogger.Msg("[CurrencyManager] Initialisiere Währungssystem...");
            CurrentBalances = SaveManager.LoadCurrencyData();
            MelonLogger.Msg($"[CurrencyManager] Geladene Stände - Intel: {CurrentBalances.IntelPoints}, Logistics: {CurrentBalances.LogisticsTokens}, Favor: {CurrentBalances.CommandFavor}");
        }

        public static void AddCurrency(CurrencyType type, int amount)
        {
            if (amount < 0) return;

            // FairnessGuard: eigene Währungen sind laut Mod-Plan nur im Singleplayer voll aktiv,
            // damit im Co-op niemand einen wirtschaftlichen Vorteil gegenüber Mitspielern hat.
            // Rang/XP zählen laut Design weiterhin (ProgressionManager.AddXP ist NICHT gegated).
            if (FairnessGuard.IsMultiplayerActive)
            {
                MelonLogger.Msg($"[CurrencyManager] Multiplayer aktiv — {amount} {type} NICHT gutgeschrieben (Fairness).");
                return;
            }

            switch (type)
            {
                case CurrencyType.IntelPoints:
                    CurrentBalances.IntelPoints += amount;
                    break;
                case CurrencyType.LogisticsTokens:
                    CurrentBalances.LogisticsTokens += amount;
                    break;
                case CurrencyType.CommandFavor:
                    CurrentBalances.CommandFavor += amount;
                    break;
            }

            SaveManager.SaveCurrencyData(CurrentBalances);
            MelonLogger.Msg($"[CurrencyManager] +{amount} {type} hinzugefügt.");
        }

        public static bool SpendCurrency(CurrencyType type, int amount)
        {
            if (amount < 0) return false;
            
            // Im Multiplayer keine Mod-Währungen ausgeben (verhindert unfaire Vorteile)
            if (FairnessGuard.IsMultiplayerActive)
            {
                MelonLogger.Warning("[CurrencyManager] Käufe im Multiplayer blockiert durch FairnessGuard!");
                return false;
            }

            if (!HasEnough(type, amount))
            {
                MelonLogger.Msg($"[CurrencyManager] Nicht genug {type} (Benötigt: {amount}).");
                return false;
            }

            switch (type)
            {
                case CurrencyType.IntelPoints:
                    CurrentBalances.IntelPoints -= amount;
                    break;
                case CurrencyType.LogisticsTokens:
                    CurrentBalances.LogisticsTokens -= amount;
                    break;
                case CurrencyType.CommandFavor:
                    CurrentBalances.CommandFavor -= amount;
                    break;
            }

            SaveManager.SaveCurrencyData(CurrentBalances);
            MelonLogger.Msg($"[CurrencyManager] -{amount} {type} ausgegeben.");
            return true;
        }

        public static bool HasEnough(CurrencyType type, int amount)
        {
            switch (type)
            {
                case CurrencyType.IntelPoints:
                    return CurrentBalances.IntelPoints >= amount;
                case CurrencyType.LogisticsTokens:
                    return CurrentBalances.LogisticsTokens >= amount;
                case CurrencyType.CommandFavor:
                    return CurrentBalances.CommandFavor >= amount;
                default:
                    return false;
            }
        }
    }
}
