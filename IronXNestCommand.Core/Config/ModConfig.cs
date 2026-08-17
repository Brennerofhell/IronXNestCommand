namespace IronXNestCommand.Core.Config;

public sealed class ModConfig
{
    public bool Enabled { get; set; } = true;

    public string ToggleKey { get; set; } = "F8";

    public bool StartVisible { get; set; } = true;

    public bool DisableGameplayAlteringFeaturesInMultiplayer { get; set; } = true;

    public bool AwardXpInChallenge { get; set; }
}
