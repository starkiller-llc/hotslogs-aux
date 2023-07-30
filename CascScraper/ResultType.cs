using System.Xml;

namespace CascScraper {
    class ResultType {
        public bool IsDecimal { get; set; }
        public decimal Value { get; set; }

        public XmlNode Node { get; set; }

        public ResultType(decimal value) {
            IsDecimal = true;
            Value = value;
        }

        public ResultType(XmlNode obj) {
            Node = obj;
        }
    }
}
