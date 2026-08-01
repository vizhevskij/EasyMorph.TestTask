using EasyMorph.TestTask.Common;
using System.Text;
using System.Xml;

namespace EasyMorph.TestTask.DataParser
{
    /// <summary>
    /// Reads all <c>*.in.xml</c> files from the specified directory and generates summary XML files in the same directory.
    /// </summary>
    public class XmlParser(string workDir)
    {
        public void Run()
        {
            var reportLines = ReadFiles();
            if (reportLines.Count > 0)
            {
                reportLines.Sort();
                WriteResults(reportLines);
            }
        }

        private List<ReportLine> ReadFiles()
        {
            using var logger = new Logger(workDir);
            var lines = new List<ReportLine>();
            var files = Directory.GetFiles(workDir, "*_in.xml");
            using var pb = new ProgressBar(files.Length, "Read files");
            foreach (var file in files)
            {
                ReadXml(file, lines, logger);
                pb.Increment();
            }
            return lines;
        }

        private void ReadXml(string file, List<ReportLine> lines, Logger logger)
        {
            ReportLine line = null;
            // Tracks the parser state while traversing the XML document
            bool inStore = false, inPeriod = false, inProduct = false, hasProductName = false;
            int? totalAmountCents = null;
            string storeName = null;

            try
            {
                // using interned strings reading mode
                using XmlReader reader = XmlNameParserComparer.CreateReader(file);
                while (reader.Read())
                {
                    switch (reader.NodeType)
                    {
                        case XmlNodeType.Element:
                            switch (reader)
                            {
                                case var _ when reader.IsTotalAmount():
                                    if (inStore && inProduct && inPeriod)
                                    {
                                        if (reader.TryGetElementContent(out var strValue))
                                        {
                                            if (ConvertHelper.TryParseCents(strValue, out var value))
                                            {
                                                totalAmountCents = value;
                                            }
                                            else
                                            {
                                                totalAmountCents = null;
                                                logger.LogError($"{Path.GetFileName(file)}: Incorrect '{strValue}' value for TotalAmount");
                                            }
                                        }
                                    }
                                    break;
                                case var _ when reader.IsProductName():
                                    hasProductName = inStore && inPeriod && inProduct;
                                    break;
                                case var _ when reader.IsProduct():
                                    inProduct = inStore && inPeriod;
                                    break;
                                case var _ when reader.IsPeriod():
                                    if (inStore && reader.TryGetAttr(XmlNameParserComparer.IsDateAttr, out var strDate))
                                    {
                                        if (strDate.TryParseDate(out var date))
                                        {
                                            // Handle an unexpected duplicate store/date combination by reusingthe existing line entry instead of creating a new one
                                            line = lines.FirstOrDefault(l => l.Store == storeName && l.Date == date);
                                            line ??= new ReportLine(storeName, date);
                                            inPeriod = true;
                                        }
                                        else
                                        {
                                            logger.LogError($"{Path.GetFileName(file)}: Incorrect '{strDate}' value for Date");
                                        }
                                    }
                                    break;
                                case var _ when reader.IsStore():
                                    if (reader.TryGetAttr(XmlNameParserComparer.IsNameAttr, out string name) && !string.IsNullOrWhiteSpace(name))
                                    {
                                        storeName = name;
                                        inStore = true;
                                    }
                                    break;
                            }
                            break;
                        case XmlNodeType.EndElement:
                            switch (reader)
                            {
                                case var _ when reader.IsProduct():
                                    // Save the sale only if ProductName was successfully read
                                    if (hasProductName && totalAmountCents != null && inStore && inPeriod && inProduct)
                                    {
                                        line.AddSale(totalAmountCents.Value);
                                    }
                                    inProduct = hasProductName = false;
                                    totalAmountCents = null;
                                    break;
                                case var _ when reader.IsPeriod():
                                    if (inStore && inPeriod)
                                    {
                                        lines.Add(line);
                                    }
                                    inPeriod = false;
                                    line = null;
                                    break;
                                case var _ when reader.IsStore():
                                    inStore = false;
                                    storeName = null;
                                    line = null;
                                    break;
                            }
                            break;
                    }
                }
            }
            catch (XmlException ex)
            {
                logger.LogError($"XML error in '{Path.GetFileName(file)}' " + $"(line {ex.LineNumber}, position {ex.LinePosition}): {ex.Message}");
            }
            catch (UnauthorizedAccessException ex)
            {
                logger.LogError($"Access denied to '{Path.GetFileName(file)}': {ex.Message}");
            }
            catch (IOException ex)
            {
                logger.LogError($"I/O error while reading '{Path.GetFileName(file)}': {ex.Message}");
            }
        }

        /// <summary>
        /// Writes daily summary files from the specified report lines. Missing store/date combinations are filled with <c>0.00</c>.
        /// <para>The input collection must be sorted by date and store name.</para>
        /// </summary>
        private void WriteResults(List<ReportLine> lines)
        {
            var storeNames = GetAllUniqeStoreNames(lines);
            var index = 0;
            var minDate = lines.First().Date;
            var lastDate = lines.Last().Date;
            var writeSettings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "    ",
                Encoding = Encoding.UTF8
            };
            using var pb = new ProgressBar(lastDate.DayNumber - minDate.DayNumber + 1, "Write results");
            for (var date = minDate; date <= lastDate; date = date.AddDays(1))
            {
                var filePath = Path.Combine(workDir, date.ToString("yyyy_MM_dd") + "_out.xml");
                using var sw = File.Create(filePath);
                using var writer = XmlWriter.Create(sw, writeSettings);
                writer.WriteStartDocument();
                writer.WriteStartElement("Period");
                writer.WriteAttributeString("Date", date.ToString("yyyy-MM-dd"));
                foreach (string name in storeNames)
                {
                    long totalAmount = 0;
                    if (index < lines.Count)
                    {
                        var line = lines[index];
                        if (line.Date == date && line.Store == name)
                        {
                            totalAmount = line.TotalAmount;
                            index++;
                        }
                    }
                    writer.WriteStartElement("Store");
                    writer.WriteAttributeString("Name", name);
                    writer.WriteElementString("TotalAmount", totalAmount.ToMoneyString());
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();
                writer.WriteEndDocument();
                pb.Increment();
            }
        }

        private List<string> GetAllUniqeStoreNames(List<ReportLine> lines)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var names = new List<string>();
            foreach (var line in lines)
            {
                if (seen.Add(line.Store))
                {
                    names.Add(line.Store);
                }
            }
            return names;
        }

        /// <summary>
        /// Represents aggregated sales data for a store on a specific day.
        /// </summary>
        class ReportLine(string store, DateOnly date) : IComparable<ReportLine>
        {
            public string Store { get; } = store;
            public DateOnly Date { get; } = date;
            public long TotalAmount { get; private set; } = 0;

            public void AddSale(int sale)
            {
                TotalAmount += sale;
            }

            public int CompareTo(ReportLine other)
            {
                if (other is null) return 1;
                var r = Date.CompareTo(other.Date);
                return r == 0 ? StringComparer.Ordinal.Compare(Store, other.Store) : r;
            }
        }
    }
}