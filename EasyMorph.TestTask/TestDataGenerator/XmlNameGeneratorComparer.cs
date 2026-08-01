using System.Xml;

namespace EasyMorph.TestTask.TestDataGenerator
{
    /// <summary>
    /// Stores XML names as interned strings to enable fast reference comparison during XML parsing, improving performance and reducing memory usage.
    /// <para> Always obtain an instance through <see cref="CreateReader(XmlReader)"/>, as it initializes the interned names for the associated <see cref="XmlReader"/>. </para>
    /// </summary>
    public static class XmlNameGeneratorComparer
    {
        private static readonly XmlReaderSettings _readSettings;

        private static readonly string _storeElement;
        private static readonly string _productElement;
        private static readonly string _styleElement;
        private static readonly string _nameAttr;
        private static readonly string _minPriceAttr;
        private static readonly string _maxPriceAttr;

        static XmlNameGeneratorComparer()
        {
            var nameTable = new NameTable();
            _storeElement = nameTable.Add("Store");
            _productElement = nameTable.Add("Product");
            _styleElement = nameTable.Add("Style");
            _nameAttr = nameTable.Add("name");
            _minPriceAttr = nameTable.Add("minPrice");
            _maxPriceAttr = nameTable.Add("maxPrice");
            _readSettings = new XmlReaderSettings
            {
                IgnoreComments = true,
                IgnoreWhitespace = true,
                NameTable = nameTable
            };
        }

        public static XmlReader CreateReader(Stream stream)
        {
            return XmlReader.Create(stream, _readSettings);
        }

        public static bool IsStore(this XmlReader reader)
        {
            return ReferenceEquals(reader.Name, _storeElement);
        }

        public static bool IsProduct(this XmlReader reader)
        {
            return ReferenceEquals(reader.Name, _productElement);
        }

        public static bool IsStyle(this XmlReader reader)
        {
            return ReferenceEquals(reader.Name, _styleElement);
        }

        public static bool IsNameAttr(this XmlReader reader)
        {
            return ReferenceEquals(reader.Name, _nameAttr);
        }

        public static bool IsMinPriceAttr(this XmlReader reader)
        {
            return ReferenceEquals(reader.Name, _minPriceAttr);
        }

        public static bool IsMaxPriceAttr(this XmlReader reader)
        {
            return ReferenceEquals(reader.Name, _maxPriceAttr);
        }
    }
}