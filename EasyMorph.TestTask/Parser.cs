using System.Globalization;
using System.Text;
using System.Xml;

namespace EasyMorph.TestTask
{
    public class Parser(string workDirectory)
    {
        private readonly XmlReaderSettings _readSettings = new() { IgnoreComments = true, IgnoreWhitespace = true };
        private readonly XmlWriterSettings _writeSettings = new() { Indent = true, IndentChars = "    ", Encoding = Encoding.UTF8 };

        private StreamWriter _errorWriter;
        private string _errorFile;

        public void Run()
        {
            var reportLines = ReadFiles(workDirectory);
            if (reportLines.Count > 0)
            {
                WriteResults(reportLines);
            }
        }

        private List<ReportLine> ReadFiles(string workDirectory)
        {
            _errorFile = Path.Combine(workDirectory, "errors.txt");
            try
            {
                File.Delete(_errorFile);
            }
            catch (UnauthorizedAccessException)
            {
            }
            catch (IOException)
            {
            }

            Console.Write("Read files");
            try
            {
                var result = new List<ReportLine>();
                var files = Directory.GetFiles(workDirectory, "*_in.xml");
                var pb = new ProgressBar(files.Length);
                foreach (var file in files)
                {
                    try
                    {
                        using XmlReader reader = XmlReader.Create(file, _readSettings);
                        var storeElement = reader.NameTable.Add("Store");
                        var periodElement = reader.NameTable.Add("Period");
                        var productElement = reader.NameTable.Add("Product");
                        var productNameElement = reader.NameTable.Add("ProductName");
                        var totalAmountElement = reader.NameTable.Add("TotalAmount");
                        var nameAttr = reader.NameTable.Add("Name");
                        var dateAttr = reader.NameTable.Add("Date");

                        ReportLine line = null;

                        bool inStore = false, inPeriod = false, inProduct = false, hasProductName = false;
                        int? cents = null;
                        string storeName = null;

                        while (reader.Read())
                        {
                            switch (reader.NodeType)
                            {
                                case XmlNodeType.Element:
                                    if (ReferenceEquals(reader.Name, totalAmountElement))
                                    {
                                        if (inStore && inProduct && inPeriod)
                                        {
                                            if (reader.Read() && reader.NodeType == XmlNodeType.Text)
                                            {
                                                if (MoneyHelper.TryToCent(reader.Value, out var value))
                                                {
                                                    cents = value;
                                                }
                                                else
                                                {
                                                    cents = null;
                                                    LogError($"{Path.GetFileName(file)}: Incorrect '{reader.Value}' value for TotalAmount");
                                                }
                                            }
                                        }
                                    }
                                    else if (ReferenceEquals(reader.Name, productNameElement))
                                    {
                                        if (inPeriod && inStore && inProduct)
                                        {
                                            hasProductName = true;
                                        }
                                    }
                                    else if (ReferenceEquals(reader.Name, productElement))
                                    {
                                        if (inPeriod && inStore)
                                        {
                                            inProduct = true;
                                        }
                                    }
                                    else if (ReferenceEquals(reader.Name, periodElement))
                                    {
                                        if (inStore && reader.HasAttributes)
                                        {
                                            while (reader.MoveToNextAttribute())
                                            {
                                                if (ReferenceEquals(reader.Name, dateAttr))
                                                {
                                                    if (DateOnly.TryParseExact(reader.Value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                                                    {
                                                        line = result.FirstOrDefault(l => l.Store == storeName && l.Date == date);
                                                        if (line == null)
                                                        {
                                                            line = new ReportLine(storeName, date);
                                                        }
                                                        inPeriod = true;
                                                    }
                                                    else
                                                    {
                                                        LogError($"{Path.GetFileName(file)}: Incorrect '{reader.Value}' value for Date");
                                                    }
                                                    break;
                                                }
                                            }
                                            reader.MoveToElement();
                                        }
                                    }
                                    else if (ReferenceEquals(reader.Name, storeElement) && reader.HasAttributes)
                                    {
                                        while (reader.MoveToNextAttribute())
                                        {
                                            if (ReferenceEquals(reader.Name, nameAttr) && !string.IsNullOrWhiteSpace(reader.Value))
                                            {
                                                storeName = reader.Value;
                                                inStore = true;
                                                break;
                                            }
                                        }
                                        reader.MoveToElement();
                                    }
                                    break;
                                case XmlNodeType.EndElement:
                                    if (ReferenceEquals(reader.Name, productElement) && inStore && inPeriod)
                                    {
                                        if (hasProductName && cents != null)
                                        {
                                            line.TotalAmount += cents.Value;
                                        }
                                        inProduct = hasProductName = false;
                                        cents = null;
                                    }
                                    else if (ReferenceEquals(reader.Name, periodElement) && inStore && inPeriod)
                                    {
                                        result.Add(line);
                                        inPeriod = false;
                                        line = null;
                                    }
                                    else if (ReferenceEquals(reader.Name, storeElement))
                                    {
                                        inStore = false;
                                        storeName = null;
                                        line = null;
                                    }
                                    break;
                            }
                        }
                    }
                    catch (XmlException ex)
                    {
                        LogError($"XML error in '{Path.GetFileName(file)}' " + $"(line {ex.LineNumber}, position {ex.LinePosition}): {ex.Message}");
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        LogError($"Access denied to '{Path.GetFileName(file)}': {ex.Message}");
                    }
                    catch (IOException ex)
                    {
                        LogError($"I/O error while reading '{Path.GetFileName(file)}': {ex.Message}");
                    }

                    pb.Increment();
                }
                result.Sort(static (x, y) =>
                {
                    var r = x.Date.CompareTo(y.Date);
                    return r != 0 ? r : StringComparer.Ordinal.Compare(x.Store, y.Store);
                });
                pb.Finish();
                return result;
            }
            finally
            {
                _errorWriter?.Dispose();
                _errorWriter = null;
            }
        }

        private void WriteResults(List<ReportLine> reportLines)
        {
            Console.Write("Write results");
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var storeNames = new List<string>();

            foreach (var line in reportLines)
            {
                if (seen.Add(line.Store))
                {
                    storeNames.Add(line.Store);
                }
            }

            var index = 0;
            var minDate = reportLines.First().Date;
            var lastDate = reportLines.Last().Date;

            var pb = new ProgressBar(lastDate.DayNumber - minDate.DayNumber + 1);
            for (var date = minDate; date <= lastDate; date = date.AddDays(1))
            {
                var filePath = Path.Combine(workDirectory, date.ToString("yyyy_MM_dd") + "_out.xml");
                using var sw = File.Create(filePath);
                using var writer = XmlWriter.Create(sw, _writeSettings);
                writer.WriteStartDocument();
                writer.WriteStartElement("Period");
                writer.WriteAttributeString("Date", date.ToString("yyyy-MM-dd"));
                foreach (string store in storeNames)
                {
                    if (index < reportLines.Count)
                    {
                        var line = reportLines[index];
                        if (line.Date == date && line.Store == store)
                        {
                            writer.WriteStartElement("Store");
                            writer.WriteAttributeString("Name", line.Store);
                            writer.WriteElementString("TotalAmount", line.TotalAmount.CentToDollar());
                            writer.WriteEndElement();
                            index++;
                        }
                    }
                }
                writer.WriteEndElement();
                writer.WriteEndDocument();
                pb.Increment();
            }
            pb.Finish();
        }

        private void LogError(string message)
        {
            try
            {
                _errorWriter ??= new StreamWriter(_errorFile, false, Encoding.UTF8);
                _errorWriter.WriteLine(message);
            }
            catch (IOException)
            {
                Console.WriteLine(message);
            }
            catch (UnauthorizedAccessException)
            {
                Console.WriteLine(message);
            }
        }

        private class ReportLine(string store, DateOnly date)
        {
            public string Store { get; } = store;
            public DateOnly Date { get; } = date;
            public long TotalAmount { get; set; }
        }
    }
}