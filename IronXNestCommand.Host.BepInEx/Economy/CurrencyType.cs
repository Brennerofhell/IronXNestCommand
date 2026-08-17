namespace IronXNestCommand.Host.BepInEx.Economy
{
    public enum CurrencyType
    {
        IntelPoints,
        LogisticsTokens,
        CommandFavor
    }

    public class CurrencyWallet
    {
        public int IntelPoints { get; set; } = 100;
        public int LogisticsTokens { get; set; } = 25;
        public int CommandFavor { get; set; } = 5;
    }
}
