using System.Xml;

namespace EasyMorph.TestTask.DataParser
{
    /// <summary>
    /// Stores XML names as interned strings to enable fast reference comparison
    /// during XML parsing, improving performance and reducing memory usage.
    /// <para>
    /// Always obtain an instance through <see cref="CreateReader(XmlReader)"/>,
    /// as it initializes the interned names for the associated <see cref="XmlReader"/>.
    /// </para>
    /// </summary>
    public static class XmlNameParserComparer
    {
        private static readonly XmlReaderSettings _readSettings;
        private static readonly string _storeElement;
        private static readonly string _periodElement;
        private static readonly string _productElement;
        private static readonly string _productNameElement;
        private static readonly string _totalAmountElement;
        private static readonly string _nameAttr;
        private static readonly string _dateAttr;

        static XmlNameParserComparer()
        {
            var nameTable = new NameTable();
            _storeElement = nameTable.Add("Store");
            _periodElement = nameTable.Add("Period");
            _productElement = nameTable.Add("Product");
            _productNameElement = nameTable.Add("ProductName");
            _totalAmountElement = nameTable.Add("TotalAmount");
            _nameAttr = nameTable.Add("Name");
            _dateAttr = nameTable.Add("Date");
            _readSettings = new XmlReaderSettings 
            {
                IgnoreComments = true,
                IgnoreWhitespace = true,
                NameTable = nameTable 
            };
        }

        public static XmlReader CreateReader(string file)
        {
            return XmlReader.Create(file, _readSettings);
        }

        public static bool IsStore(this XmlReader reader)
        {
            return ReferenceEquals(reader.Name, _storeElement);
        }

        public static bool IsPeriod(this XmlReader reader)
        {
            return ReferenceEquals(reader.Name, _periodElement);
        }

        public static bool IsProduct(this XmlReader reader)
        {
            return ReferenceEquals(reader.Name, _productElement);
        }

        public static bool IsProductName(this XmlReader reader)
        {
            return ReferenceEquals(reader.Name, _productNameElement);
        }

        public static bool IsTotalAmount(this XmlReader reader)
        {
            return ReferenceEquals(reader.Name, _totalAmountElement);
        }

        public static bool IsNameAttr(this XmlReader reader)
        {
            return ReferenceEquals(reader.Name, _nameAttr);
        }

        public static bool IsDateAttr(this XmlReader reader)
        {
            return ReferenceEquals(reader.Name, _dateAttr);
        }
    }
}