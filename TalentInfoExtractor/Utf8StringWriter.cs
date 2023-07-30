using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace TalentInfoExtractor {
    public class Utf8StringWriter : StringWriter {
        public override Encoding Encoding => Encoding.UTF8;
    }
}
