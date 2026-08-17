namespace IronXNestCommand.Core
{
    public class ModConfig
    {
        public string ToggleKey { get; set; } = "F8";
        public bool StartVisible { get; set; } = true;
        public bool AutoAdvisorEnabled { get; set; } = true;
        public bool DisableInMultiplayer { get; set; } = true;
        public bool SoundFeedbackEnabled { get; set; } = true;
        public float OverlayScale { get; set; } = 1.0f;
    }
}
