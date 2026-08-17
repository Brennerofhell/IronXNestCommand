namespace IronXNestCommand.Ammo
{
    /// <summary>
    /// Stellt die Daten für eine benutzerdefinierte Munitionsart (Shell) dar.
    /// </summary>
    public class ShellDefinition
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        // Schadenswerte
        public float KineticDamage { get; set; }
        public float ExplosiveDamage { get; set; }
        public float ArmorPenetration { get; set; }
        public float BlastRadius { get; set; }

        // Wirtschaft
        public int RequisitionCost { get; set; }
        public int CommandFavorCost { get; set; }

        public ShellDefinition(string id, string name)
        {
            Id = id;
            Name = name;
            Description = "Eine Custom Shell.";
            KineticDamage = 100f;
            ExplosiveDamage = 0f;
            ArmorPenetration = 10f;
            BlastRadius = 0f;
            RequisitionCost = 10;
            CommandFavorCost = 0;
        }
    }
}
