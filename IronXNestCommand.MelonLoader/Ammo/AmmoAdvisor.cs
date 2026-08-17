using System.Collections.Generic;

namespace IronXNestCommand.Ammo
{
    public enum TargetCategory
    {
        InfantrySquad,
        LightVehicle,
        MediumArmor,
        HeavyBunker,
        CounterBatteryArtillery,
        ElectronicCommandCenter
    }

    public class AdvisorRecommendation
    {
        public TargetCategory Category { get; set; }
        public string TargetName { get; set; }
        public string RecommendedShellId { get; set; }
        public string RecommendedShellName { get; set; }
        public int RecommendedPowderCharges { get; set; }
        public string TacticalAdvice { get; set; }
        public string PenetrationRating { get; set; }
    }

    public static class AmmoAdvisor
    {
        private static readonly Dictionary<TargetCategory, AdvisorRecommendation> Database = new();

        static AmmoAdvisor()
        {
            Database[TargetCategory.InfantrySquad] = new AdvisorRecommendation
            {
                Category = TargetCategory.InfantrySquad,
                TargetName = "Infanterie / Weiche Ziele",
                RecommendedShellId = "standard_he",
                RecommendedShellName = "High-Explosive (HE)",
                RecommendedPowderCharges = 1,
                TacticalAdvice = "Flächenwirkung nutzen. Auf Geländesenken zielen.",
                PenetrationRating = "Niedrig"
            };

            Database[TargetCategory.LightVehicle] = new AdvisorRecommendation
            {
                Category = TargetCategory.LightVehicle,
                TargetName = "Leichte Aufklärungsfahrzeuge",
                RecommendedShellId = "standard_he",
                RecommendedShellName = "Standard HE / Schrapnell",
                RecommendedPowderCharges = 2,
                TacticalAdvice = "Kurze Vorhaltezeit einplanen.",
                PenetrationRating = "Mittel"
            };

            Database[TargetCategory.MediumArmor] = new AdvisorRecommendation
            {
                Category = TargetCategory.MediumArmor,
                TargetName = "Mittlerer Kampfpanzer",
                RecommendedShellId = "shell_ap_hv",
                RecommendedShellName = "HV-AP Shell (High-Velocity)",
                RecommendedPowderCharges = 3,
                TacticalAdvice = "Flache Flugbahn für maximale kinetische Wucht an Turm/Wanne.",
                PenetrationRating = "Sehr Hoch"
            };

            Database[TargetCategory.HeavyBunker] = new AdvisorRecommendation
            {
                Category = TargetCategory.HeavyBunker,
                TargetName = "Schwerer Beton-Bunker",
                RecommendedShellId = "shell_ap_hv",
                RecommendedShellName = "HV-AP Shell / Bunkerbrecher",
                RecommendedPowderCharges = 4,
                TacticalAdvice = "Maximale Treibladung. Auf Scharten oder Dachpartie zielen.",
                PenetrationRating = "Extrem"
            };

            Database[TargetCategory.CounterBatteryArtillery] = new AdvisorRecommendation
            {
                Category = TargetCategory.CounterBatteryArtillery,
                TargetName = "Feindliche Artilleriestellung",
                RecommendedShellId = "shell_emp_mk1",
                RecommendedShellName = "EMP Shell Mk I / Weitreichende HE",
                RecommendedPowderCharges = 3,
                TacticalAdvice = "Erstschlag mit EMP unterbricht Zielführung der feindlichen Batterie.",
                PenetrationRating = "Spezial"
            };

            Database[TargetCategory.ElectronicCommandCenter] = new AdvisorRecommendation
            {
                Category = TargetCategory.ElectronicCommandCenter,
                TargetName = "Radar- & Funkstation",
                RecommendedShellId = "shell_emp_mk1",
                RecommendedShellName = "EMP Shell Mk I",
                RecommendedPowderCharges = 2,
                TacticalAdvice = "Schaltet Ziel-Erfassung und feindliche Luftunterstützung aus.",
                PenetrationRating = "Elektro-Magnetisch"
            };
        }

        public static AdvisorRecommendation GetRecommendation(TargetCategory category)
        {
            if (Database.TryGetValue(category, out var rec))
                return rec;

            return Database[TargetCategory.MediumArmor];
        }

        public static IEnumerable<AdvisorRecommendation> GetAllCategories() => Database.Values;
    }
}
