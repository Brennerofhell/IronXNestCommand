namespace IronXNestCommand.Economy
{
    /// <summary>
    /// Speicherbare Datenstruktur für alle Kontostände des Spielers.
    /// </summary>
    public class CurrencyData
    {
        public int IntelPoints { get; set; } = 0;
        public int LogisticsTokens { get; set; } = 0;
        public int CommandFavor { get; set; } = 0;

        // Requisition Credits werden meist vom Basisspiel verwaltet, aber wir könnten sie hier tracken, falls nötig.
        // public int RequisitionCredits { get; set; } = 0;
    }
}
