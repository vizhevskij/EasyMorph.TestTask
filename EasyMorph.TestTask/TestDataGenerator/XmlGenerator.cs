using EasyMorph.TestTask.Common;
using System.IO.Compression;
using System.Reflection;
using System.Text;
using System.Xml;

namespace EasyMorph.TestTask.TestDataGenerator
{
    /// <summary>
    /// Generates sample XML files using randomly selected data from dictionaries embedded as assembly resources.
    /// </summary>
    public class XmlGenerator
    {
        private readonly List<string> _stores;
        private readonly List<Product> _products;
        private readonly XmlWriterSettings _writerSettings;

        public XmlGenerator()
        {
            _stores = LoadFromEmbeddedResource(ReadStoreNameXml, "US_Canada_100_Stores.xml.gz");
            _products = LoadFromEmbeddedResource(ReadProductXml, "US_Canada_1000_Products.xml.gz");
            _writerSettings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "    ",
                Encoding = Encoding.UTF8
            };
        }

        /// <summary>
        /// Generates sample XML files in the specified directory.
        /// <para>The generated data spans the last 30 days relative to the current date.</para>
        /// </summary>
        /// <param name="workDirectory"> Destination directory. If it does not exist, it is created. </param>
        /// <param name="storeCount"> Number of store XML files to generate. </param>
        /// <param name="approximateFileSizeKb"> Approximate size of each generated XML file in KB. </param>
        public void Run(string workDirectory, int storeCount, int approximateFileSizeKb)
        {
            if (!Directory.Exists(workDirectory))
            {
                Directory.CreateDirectory(workDirectory);
            }

            var productCountPerPeriod = EstimateProductsPerPeriod(approximateFileSizeKb);
            var random = new Random();
            var startDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-30));
            var storeNames = GetRandomStoreNames(storeCount);
            using var pb = new ProgressBar(storeCount, "Generation");
            foreach (var storeName in storeNames)
            {
                var shortStoreNameName = new string(storeName.Where(c => !Path.GetInvalidFileNameChars().Contains(c)).Take(25).ToArray());
                var filePath = Path.Combine(workDirectory, shortStoreNameName + "_in.xml");
                using var sw = File.Create(filePath);
                using var writer = XmlWriter.Create(sw, _writerSettings);
                writer.WriteStartDocument();
                writer.WriteStartElement("Store");
                writer.WriteAttributeString("Name", storeName);
                for (var day = 0; day < 30; day++)
                {
                    writer.WriteStartElement("Period");
                    writer.WriteAttributeString("Date", startDate.AddDays(day).ToString("yyyy-MM-dd"));
                    for (var counter = 0; counter < productCountPerPeriod; counter++)
                    {
                        var product = BuildRandomProduct(random);
                        writer.WriteStartElement("Product");
                        writer.WriteElementString("ProductName", product.Name);
                        writer.WriteElementString("TotalAmount", product.Total);
                        writer.WriteEndElement();
                    }
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();
                writer.WriteEndDocument();
                pb.Increment();
            }
        }

        /// <summary>
        /// Loads objects from a GZip-compressed XML resource embedded in the assembly.
        /// </summary>
        private List<T> LoadFromEmbeddedResource<T>(Func<XmlReader, IEnumerable<T>> selector, string resourceName)
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(GetType().Namespace + "." + resourceName);
            using var gzip = new GZipStream(stream, CompressionMode.Decompress);
            var settings = new XmlReaderSettings { IgnoreComments = true, IgnoreWhitespace = true };
            using var reader = XmlNameGeneratorComparer.CreateReader(gzip);
            return selector(reader).ToList();
        }

        private IEnumerable<string> ReadStoreNameXml(XmlReader reader)
        {
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element && reader.IsStore() && reader.TryGetAttr(XmlNameGeneratorComparer.IsNameAttr, out var value))
                {
                    yield return value;
                }
            }
        }

        private IEnumerable<Product> ReadProductXml(XmlReader reader)
        {
            Product product = null;
            List<string> styles = [];
            while (reader.Read())
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        if (reader.HasAttributes)
                        {
                            if (reader.IsProduct())
                            {
                                string nameStr = null, minPriceStr = null, maxPriceStr = null;
                                while (reader.MoveToNextAttribute())
                                {
                                    switch (reader)
                                    {
                                        case var _ when reader.IsNameAttr():
                                            nameStr = reader.Value;
                                            break;
                                        case var _ when reader.IsMinPriceAttr():
                                            minPriceStr = reader.Value;
                                            break;
                                        case var _ when reader.IsMaxPriceAttr():
                                            maxPriceStr = reader.Value;
                                            break;
                                    }
                                }
                                product = (!string.IsNullOrWhiteSpace(nameStr) && ConvertHelper.TryParseCents(minPriceStr, out var min) && ConvertHelper.TryParseCents(maxPriceStr, out var max)) ? new Product(nameStr, min, max) : null;
                                styles = [];
                                reader.MoveToElement();
                            }
                            else if (reader.IsStyle() && reader.TryGetAttr(XmlNameGeneratorComparer.IsNameAttr, out var nameStr) && !string.IsNullOrWhiteSpace(nameStr))
                            {
                                styles.Add(nameStr);
                            }
                        }
                        break;
                    case XmlNodeType.EndElement:
                        if (reader.IsProduct())
                        {
                            if (product != null)
                            {
                                product.Styles = styles.ToArray();
                                yield return product;
                            }
                            product = null;
                            styles = [];
                        }
                        break;
                }
            }
        }

        private string[] GetRandomStoreNames(int storeCount)
        {
            var storeNumbers = new HashSet<int>();
            while (storeNumbers.Count < storeCount)
            {
                storeNumbers.Add(Random.Shared.Next(0, _stores.Count));
            }
            return storeNumbers.Select(n => _stores[n]).ToArray();
        }

        /// <summary>
        /// Creates a random product using the embedded product dictionary. The product name is generated by combining a random style with a product name, and the total amount is selected randomly within the product's price range.
        /// </summary>
        private (string Name, string Total) BuildRandomProduct(Random random)
        {
            var product = _products[random.Next(0, _products.Count)];
            var total = random.Next(product.MinPrice, product.MaxPrice).ToMoneyString();
            var style = product.Styles[random.Next(0, product.Styles.Length)];
            return (Name: string.Format($"{style} {product.Name}"), Total: total);
        }

        // Shows size of string <Product><ProductName></ProductName><Tota...
        private const int ProductXmlMarkupSize = 114;

        private int EstimateProductsPerPeriod(int approximateFileSizeKb)
        {
            return (int)(approximateFileSizeKb * 1024d / 30 /
                (_products.Average(p => p.Name.Length + p.MaxPrice.ToString().Length + 1 + p.Styles.Average(p => p.Length)) + ProductXmlMarkupSize));
        }

        /// <summary>
        /// Represents a product loaded from an embedded resource.
        /// </summary>
        private class Product(string name, int minPrice, int maxPrice)
        {
            public string Name { get; } = name;
            public int MinPrice { get; set; } = minPrice;
            public int MaxPrice { get; set; } = maxPrice;
            public string[] Styles { get; set; }
        }
    }
}