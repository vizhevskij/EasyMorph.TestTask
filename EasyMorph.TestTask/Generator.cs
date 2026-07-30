using System.IO.Compression;
using System.Reflection;
using System.Text;
using System.Xml;

namespace EasyMorph.TestTask
{
    public class Generator
    {
        private readonly List<string> _stores;
        private readonly List<Product> _products;
        private readonly Random _random = new();
        private readonly int _generatedProductAverageXmlSize;

        public Generator()
        {
            _stores = LoadStoreNames();
            _products = LoadProducts();
            _generatedProductAverageXmlSize = (int)Math.Round(_products.Average(p => p.Name.Length + p.MaxPrice.ToString().Length + 1 + p.Styles.Average(p => p.Length))) + 114;
        }

        public void Run(string workDirectory, int storeCount, int approximateStoreSize)
        {
            var pb = new ProgressBar(storeCount);
            if (!Directory.Exists(workDirectory))
            {
                Directory.CreateDirectory(workDirectory);
            }
            var productCountPerPeriod = (int)(approximateStoreSize * 1024d / 30 / _generatedProductAverageXmlSize);
            var startDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-30));
            var settings = new XmlWriterSettings { Indent = true, IndentChars = "    ", Encoding = Encoding.UTF8 };
            var storeNumbers = new HashSet<int>();
            while (storeNumbers.Count < storeCount)
            {
                storeNumbers.Add(Random.Shared.Next(0, _stores.Count));
            }
            var storeNames = storeNumbers.Select(n => _stores[n]).ToArray();
            foreach (var storeName in storeNames)
            {
                var shortStoreNameName = new string(storeName.Where(c => !Path.GetInvalidFileNameChars().Contains(c)).Take(25).ToArray());
                var filePath = Path.Combine(workDirectory, shortStoreNameName + "_in.xml");
                using var sw = File.Create(filePath);
                using var writer = XmlWriter.Create(sw, settings);
                writer.WriteStartDocument();
                writer.WriteStartElement("Store");
                writer.WriteAttributeString("Name", storeName);
                for (var day = 0; day < 30; day++)
                {
                    writer.WriteStartElement("Period");
                    writer.WriteAttributeString("Date", startDate.AddDays(day).ToString("yyyy-MM-dd"));
                    for (var counter = 0; counter < productCountPerPeriod; counter++)
                    {
                        var product = GetRandomProduct();
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
            pb.Finish();
        }

        private List<string> LoadStoreNames()
        {
            var filePath = GetType().Namespace + ".US_Canada_100_Stores.xml.gz";
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(filePath);
            using var gzip = new GZipStream(stream, CompressionMode.Decompress);
            var settings = new XmlReaderSettings { IgnoreComments = true, IgnoreWhitespace = true };
            using var reader = XmlReader.Create(gzip, settings);
            var storeElementName = reader.NameTable.Add("Store");
            var nameAttrName = reader.NameTable.Add("name");
            var result = new List<string>();
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element && ReferenceEquals(reader.Name, storeElementName) && reader.HasAttributes)
                {
                    while (reader.MoveToNextAttribute())
                    {
                        if (ReferenceEquals(reader.Name, nameAttrName))
                        {
                            result.Add(reader.Value);
                            break;
                        }
                    }
                    reader.MoveToElement();
                }
            }
            return result;
        }

        private (string Name, string Total) GetRandomProduct()
        {
            var product = _products[_random.Next(0, _products.Count)];
            var total = _random.Next(product.MinPrice, product.MaxPrice).CentToDollar();
            var style = product.Styles[_random.Next(0, product.Styles.Length)];
            return (Name: string.Format($"{style} {product.Name}"), Total: total);
        }

        private List<Product> LoadProducts()
        {
            var result = new List<Product>();
            var filePath = GetType().Namespace + ".US_Canada_1000_Products.xml.gz";
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(filePath);
            using GZipStream gzip = new(stream, CompressionMode.Decompress);
            var settings = new XmlReaderSettings { IgnoreComments = true, IgnoreWhitespace = true };
            using XmlReader reader = XmlReader.Create(gzip, settings);
            var productElement = reader.NameTable.Add("Product");
            var styleElement = reader.NameTable.Add("Style");
            var nameAttr = reader.NameTable.Add("name");
            var minPriceAttr = reader.NameTable.Add("minPrice");
            var maxPriceAttr = reader.NameTable.Add("maxPrice");

            Product current = null;
            List<string> styles = null;
            while (reader.Read())
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        if (ReferenceEquals(reader.Name, productElement))
                        {
                            if (reader.HasAttributes)
                            {
                                string nameStr = null, minPriceStr = null, maxPriceStr = null;
                                while (reader.MoveToNextAttribute())
                                {
                                    if (ReferenceEquals(reader.Name, nameAttr))
                                    {
                                        nameStr = reader.Value;
                                    }
                                    else if (ReferenceEquals(reader.Name, minPriceAttr))
                                    {
                                        minPriceStr = reader.Value;
                                    }
                                    else if (ReferenceEquals(reader.Name, maxPriceAttr))
                                    {
                                        maxPriceStr = reader.Value;
                                    }
                                }

                                current = (!string.IsNullOrWhiteSpace(nameStr) && MoneyHelper.TryToCent(minPriceStr, out var min) && MoneyHelper.TryToCent(maxPriceStr, out var max)) ? new Product(nameStr, min, max) : null;
                                styles = [];
                                reader.MoveToElement();
                            }
                        }
                        else if (ReferenceEquals(reader.Name, styleElement) && reader.HasAttributes)
                        {
                            string nameStr = null;
                            while (reader.MoveToNextAttribute())
                            {
                                if (ReferenceEquals(reader.Name, nameAttr))
                                {
                                    nameStr = reader.Value;
                                    break;
                                }
                            }
                            if (!string.IsNullOrWhiteSpace(nameStr))
                            {
                                styles.Add(nameStr);
                            }
                            reader.MoveToElement();
                        }
                        break;
                    case XmlNodeType.EndElement:
                        if (ReferenceEquals(reader.Name, productElement))
                        {
                            if (current != null)
                            {
                                current.Styles = styles.ToArray();
                                result.Add(current);
                            }
                            current = null;
                            styles = null;
                        }
                        break;
                }
            }

            return result;
        }

        internal class Product(string name, int minPrice, int maxPrice)
        {
            public string Name { get; } = name;
            public int MinPrice { get; set; } = minPrice;
            public int MaxPrice { get; set; } = maxPrice;
            public string[] Styles { get; set; }
        }
    }
}
