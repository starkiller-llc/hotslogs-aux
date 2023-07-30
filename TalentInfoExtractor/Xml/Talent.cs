namespace TalentInfoExtractor.Xml {
    public class Talent {
        public string TalentImage { get; set; }
        public int Tier { get; set; }
        public string Name { get; set; }
        public int? Cooldown { get; set; }
        public bool ShouldSerializeCooldown() {
            return Cooldown.HasValue;
        }
    }
}
