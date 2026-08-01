using EasyMorph.TestTask;
using EasyMorph.TestTask.DataParser;
using System.Xml;

namespace EasyMorph.UnitTests
{
    public class UnitTests
    {
        [Theory]
        [InlineData("TotalAmount\\First", "2026_07_26_out.xml", "/Period[@Date='2026-07-26']/Store[@Name='ABC']/TotalAmount", "666.66")]
        [InlineData("TotalAmount\\UnclosedTag", null, null, null, "The 'Product' start tag on line 4 position 4 does not match the end tag")]
        [InlineData("TotalAmount\\SeveralFiles", "2026_07_26_out.xml", "/Period[@Date='2026-07-26']/Store[@Name='ABC']/TotalAmount", "1718.19")]
        [InlineData("TotalAmount\\InvalidTotalAmounts", "2026_07_26_out.xml", "/Period[@Date='2026-07-26']/Store[@Name='ABC']/TotalAmount", "543.21", "123.457")]
        [InlineData("TotalAmount\\InvalidTotalAmounts", "2026_07_26_out.xml", "/Period[@Date='2026-07-26']/Store[@Name='ABC']/TotalAmount", "543.21", "123,45")]
        [InlineData("TotalAmount\\InvalidTotalAmounts", "2026_07_26_out.xml", "/Period[@Date='2026-07-26']/Store[@Name='ABC']/TotalAmount", "543.21", "-123.45")]
        [InlineData("TotalAmount\\InvalidTotalAmounts", "2026_07_26_out.xml", "/Period[@Date='2026-07-26']/Store[@Name='ABC']/TotalAmount", "543.21", "aaaa.bbb")]
        [InlineData("TotalAmount\\InvalidPeriods", null, null, null, "2026-26-07")]

        public void CheckTotalAmount(string workDir, string resFile, string xPath, string expected, string expectedError = null)
        {
            var startDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, workDir);
            var parser = new XmlParser(startDir);
            parser.Run();
            if (resFile != null && xPath != null && expected != null)
            {
                var resDoc = new XmlDocument();
                resDoc.Load(Path.Combine(startDir, resFile));
                var expectedNode = resDoc.DocumentElement.SelectSingleNode(xPath);
                Assert.Equal(expected, expectedNode.InnerText);
            }
            if (!string.IsNullOrEmpty(expectedError))
            {
                var errors = File.ReadAllText(Path.Combine(startDir, "errors.txt"));
                Assert.Contains(expectedError, errors);
            }
        }
    }
}