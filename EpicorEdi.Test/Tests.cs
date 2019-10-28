using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Linq;
using System.Text;

namespace EpicorEdi.Test
{
    [TestClass]
    public class Tests
    {
        [TestMethod]
        public void TestReadWrite()
        {
            var originalBytes = File.ReadAllBytes(@"TestData\Normal.app");
            var reader = new StreamReader(new MemoryStream(originalBytes));
            var report = ReportParser.Parse(reader, ReportParser.BySchemaIndex);
            var outputBytes = Encoding.UTF8.GetBytes(report.ToString());
            CollectionAssert.AreEqual(originalBytes, outputBytes);
        }

        [TestMethod]
        public void TestFixesDoNothing()
        {
            var goodText = File.ReadAllText(@"TestData\Normal.app", Encoding.UTF8);
            var goodReport = ReportParser.Parse(new StringReader(goodText), ReportParser.BySchemaIndex);
            /* Bug fixes recognize good data and return the same report. */
            Assert.AreSame(goodReport, goodReport.FixCompanyRows());
            Assert.AreSame(goodReport, goodReport.FixRowOrder());
        }

        [TestMethod]
        public void TestFixMissingRows()
        {
            var buggyText = File.ReadAllText(@"TestData\MissingCompanyRows.app", Encoding.UTF8);
            var buggyReport = ReportParser.Parse(new StringReader(buggyText), ReportParser.BySchemaIndex);
            Assert.IsFalse(buggyReport.Documents.All(o => o.GetRows().First().Type == "Company"));
            var fixedReport = buggyReport.FixCompanyRows();
            Assert.IsTrue(fixedReport.Documents.All(o => o.GetRows().First().Type == "Company"));
        }

        [TestMethod]
        public void TestFixRowOrder()
        {
            /* Note that IsRowGroupingAmbiguous is not necessarily the inverse of IsInSchemaOrder.
             * The test data was intentionally set up for the worst case scenario where the row
             * that appears out of schema order is last in a document and is a type that does not
             * appear in all documents, creating ambiguity. Consequently, calling FixRowOrder
             * causes IsRowGroupingAmbiguous to return false. However, FixRowOrder does not
             * actually fix the ambiguity, and the report still should not be trusted. */
            var buggyText = File.ReadAllText(@"TestData\NotInSchemaOrder.app", Encoding.UTF8);
            var buggyReport = ReportParser.Parse(new StringReader(buggyText), ReportParser.ByRowGrouping);
            Assert.IsFalse(buggyReport.Documents.All(o => o.IsInSchemaOrder(buggyReport.Schema)));
            Assert.IsTrue(buggyReport.IsRowGroupingAmbiguous());
            var fixedReport = buggyReport.FixRowOrder();
            Assert.IsTrue(fixedReport.Documents.All(o => o.IsInSchemaOrder(fixedReport.Schema)));
            Assert.IsFalse(fixedReport.IsRowGroupingAmbiguous());
        }

        [TestMethod]
        public void TestRemap()
        {
            var normalBytes = File.ReadAllBytes(@"TestData\Normal.app");
            var reader = new StreamReader(new MemoryStream(normalBytes));
            var normalReport = ReportParser.Parse(reader, ReportParser.BySchemaIndex);
            var changedText = File.ReadAllText(@"TestData\ColumnOrderChanged.app");
            var changedReport = ReportParser.Parse(new StringReader(changedText), ReportParser.BySchemaIndex);
            var remappedReport = normalReport.Schema.Remap(changedReport.Documents);
            var remappedBytes = Encoding.UTF8.GetBytes(remappedReport.ToString());
            CollectionAssert.AreEqual(normalBytes, remappedBytes);
        }
    }
}
