namespace IronXNestCommand.Core.Config
{
    public sealed class ModConfig
    {
        public bool Enabled { get; set; } = true;
        public string ToggleKey { get; set; } = "F8";
        public string MiniHudToggleKey { get; set; } = "F7";
        public bool StartVisible { get; set; } = true;
        public bool ShowMiniHud { get; set; } = true;
        public bool AutoAdvisorEnabled { get; set; } = true;
        public bool DisableInMultiplayer { get; set; } = true;
        public bool SoundFeedbackEnabled { get; set; } = true;
        public bool PreventEnemyDespawn { get; set; } = true;
        public float OverlayScale { get; set; } = 1.0f;
    }
}
