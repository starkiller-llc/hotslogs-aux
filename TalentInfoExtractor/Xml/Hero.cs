using System.Collections.Generic;

namespace TalentInfoExtractor.Xml {
    public class Hero {
        public string Name { get; set; }
        public string Image { get; set; }
        public List<Talent> Talents { get; set; }
        public List<Skill> Skills { get; set; }
    }
}
