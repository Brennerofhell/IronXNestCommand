using System.Collections.Generic;

namespace IronXNestCommand.Host.BepInEx.Progression
{
    public class RankDefinition
    {
        public int Level { get; set; }
        public string Title { get; set; }
        public int RequiredXP { get; set; }
        public string Description { get; set; }
        public List<string> UnlockedPerks { get; set; } = new();

        public RankDefinition(int level, string title, int requiredXp, string description, params string[] perks)
        {
            Level = level;
            Title = title;
            RequiredXP = requiredXp;
            Description = description;
            if (perks != null)
                UnlockedPerks.AddRange(perks);
        }
    }
}
