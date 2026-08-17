using MelonLoader;

namespace IronXNestCommand.Core
{
    public static class FairnessGuard
    {
        public static bool IsMultiplayerActive { get; private set; } = false;

        /// <summary>
        /// Schaltet den Multiplayer-Sicherheitsmodus ein oder aus.
        /// </summary>
        public static void SetMultiplayerState(bool inMultiplayer)
        {
            IsMultiplayerActive = inMultiplayer;

            if (inMultiplayer)
            {
                MelonLogger.Warning("[FairnessGuard] Multiplayer-Sitzung erkannt! Unfaire Mod-Boni wurden deaktiviert.");
            }
            else
            {
                MelonLogger.Msg("[FairnessGuard] Einzelspieler-Modus aktiv. Alle QoL- und Assistenz-Funktionen aktiv.");
            }
        }

        /// <summary>
        /// Prüft, ob ein Gameplay-Bonus aktuell erlaubt ist.
        /// </summary>
        public static bool CanApplyGameplayBonus()
        {
            // Im Multiplayer niemals unfaire Werte manipulieren
            return !IsMultiplayerActive;
        }
    }
}
