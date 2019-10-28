using System.Collections.Generic;
using System.Linq;

namespace EpicorEdi
{
    public class Report
    {
        public Schema Schema { get; }
        public IEnumerable<Document> Documents { get { return _docs.AsReadOnly(); } }
        private readonly List<Document> _docs = new List<Document>();

        internal Report()
        {
            Schema = new Schema();
        }

        internal Report(Schema schema)
        {
            Schema = schema;
        }

        internal void Add(Document document)
        {
            _docs.Add(document);
        }

        /// <summary>
        /// Workaround for the Epicor bug where an EDI Report Style with an
        /// associated EDI Definition only outputs the "Company" row for the
        /// first document in a report (PRB0216635).
        /// </summary>
        public Report FixCompanyRows()
        {
            if(_docs.Count < 2 || _docs.Skip(1).All(o => o.HasRowType("Company")))
            {
                return this;
            }
            var index = _docs[0].FindIndex("Company");
            var missingRows = _docs[0].GetRows("Company");
            var report = new Report(Schema);
            report.Add(_docs[0]);
            foreach (var doc in _docs.Skip(1))
            {
                if(doc.HasRowType("Company"))
                {
                    report.Add(doc);
                }
                else
                {
                    var newDoc = new Document();
                    newDoc.InsertRange(0, doc.GetRows());
                    newDoc.InsertRange(index, missingRows);
                    report.Add(newDoc);
                }
            }
            return report;
        }

        /// <summary>
        /// Tests whether this report would be ambiguous if read with <see
        /// cref="ReportParser.ByRowGrouping(Schema, Document, Row)">
        /// ByRowGrouping</see>.
        /// </summary>
        /// <returns>true if this report is ambiguous; otherwise false</returns>
        public bool IsRowGroupingAmbiguous()
        {
            return _docs.Count > 1 && (!_docs.Select(o => o.GetFirstRow().Type).AllEqual() || !_docs.Select(o => o.GetLastRow().Type).AllEqual());
        }

        /// <summary>
        /// Workaround for the Epicor bug where data rows for tables added to
        /// a custom Report Data Definition do not always appear in the same
        /// order as the schema rows. This method can cause <see
        /// cref="IsRowGroupingAmbiguous"/> to incorrectly return false.
        /// </summary>
        public Report FixRowOrder()
        {
            return _docs.All(o => o.IsInSchemaOrder(Schema)) ? this : Schema.Remap(_docs);
        }

        public override string ToString()
        {
            var objects = new List<object>();
            objects.Add(Schema);
            objects.AddRange(_docs);
            return string.Join("\r\n", objects);
        }
    }
}
