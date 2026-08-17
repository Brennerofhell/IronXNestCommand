using System.IO;
using IronXNestCommand.Core.Logging;
using IronXNestCommand.Core.Paths;
using IronXNestCommand.Host.BepInEx.Core;

namespace IronXNestCommand.Host.BepInEx.Economy
{
    public static class CurrencyManager
    {
        private static CurrencyWallet _wallet;
        private static string WalletFilePath => Path.Combine(ModPaths.DataRoot, "economy.json");

        public static CurrencyWallet CurrentBalances => _wallet ??= new CurrencyWallet();

        public static void Initialize()
        {
            _wallet = SaveManager.LoadJson(WalletFilePath, new CurrencyWallet());
            ModLogger.Info($"[CurrencyManager] Geladen: {_wallet.IntelPoints} Intel, {_wallet.LogisticsTokens} Tokens, {_wallet.CommandFavor} Favor.");
        }

        public static void AddCurrency(CurrencyType type, int amount)
        {
            if (amount <= 0) return;

            // FairnessGuard: eigene Währungen sind laut Mod-Plan nur im Singleplayer voll aktiv,
            // damit im Co-op niemand einen wirtschaftlichen Vorteil gegenüber Mitspielern hat.
            // Rang/XP zählen laut Design weiterhin (ProgressionManager.AddXP ist NICHT gegated).
            if (FairnessGuard.IsMultiplayerActive)
            {
                ModLogger.Info($"[CurrencyManager] Multiplayer aktiv — {amount} {type} NICHT gutgeschrieben (Fairness).");
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
            SaveManager.SaveJson(WalletFilePath, _wallet);
        }

        public static bool SpendCurrency(CurrencyType type, int amount)
        {
            if (amount <= 0) return true;
            bool success = false;
            switch (type)
            {
                case CurrencyType.IntelPoints:
                    if (CurrentBalances.IntelPoints >= amount)
                    {
                        CurrentBalances.IntelPoints -= amount;
                        success = true;
                    }
                    break;
                case CurrencyType.LogisticsTokens:
                    if (CurrentBalances.LogisticsTokens >= amount)
                    {
                        CurrentBalances.LogisticsTokens -= amount;
                        success = true;
                    }
                    break;
                case CurrencyType.CommandFavor:
                    if (CurrentBalances.CommandFavor >= amount)
                    {
                        CurrentBalances.CommandFavor -= amount;
                        success = true;
                    }
                    break;
            }

            if (success)
            {
                SaveManager.SaveJson(WalletFilePath, _wallet);
            }
            return success;
        }
    }
}
