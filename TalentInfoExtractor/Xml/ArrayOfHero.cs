using System.Collections.Generic;
using System.Xml.Serialization;

namespace TalentInfoExtractor.Xml {
    public class ArrayOfHero {
        [XmlElement("Hero")]
        public List<Hero> Heros { get; set; }
    }
}
