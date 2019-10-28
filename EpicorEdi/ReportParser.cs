using CsvHelper;
using CsvHelper.Configuration;
using System.Collections.Generic;
using System.IO;

namespace EpicorEdi
{
    public class ReportParser
    {
        /// <summary>
        /// Tests whether the next row belongs to the current document.
        /// </summary>
        /// <param name="schema">the report schema</param>
        /// <param name="document">the current document</param>
        /// <param name="row">the next row</param>
        /// <returns>true if the next row belongs to the current document; otherwise false</returns>
        public delegate bool RowBelongsToDocument(Schema schema, Document document, Row row);

        /// <summary>
        /// Matches rows with documents based on the assumption that row types
        /// in each document appear in the same order as schema rows in the
        /// report.
        /// </summary>
        /// <param name="schema">the report schema</param>
        /// <param name="document">the current document</param>
        /// <param name="row">the next row</param>
        public static bool BySchemaIndex(Schema schema, Document document, Row row)
        {
            return document.GetRowCount() == 0 || row.Type == document.GetLastRow().Type || schema.GetIndex(row.Type) >= schema.GetIndex(document.GetLastRow().Type);
        }

        /// <summary>
        /// Matches rows with documents based on the assumption that rows of
        /// the same type are grouped together in each document. This is a
        /// workaround for Epicor's row order bug. This method results in
        /// ambiguity if the first and last row types are not the same in
        /// every document.
        /// </summary>
        /// <param name="schema">the report schema</param>
        /// <param name="document">the current document</param>
        /// <param name="row">the next row</param>
        public static bool ByRowGrouping(Schema schema, Document document, Row row)
        {
            return document.GetRowCount() == 0 || row.Type == document.GetLastRow().Type || !document.HasRowType(row.Type);
        }

        public static Report Parse(TextReader reader, RowBelongsToDocument rowBelongsToDocument)
        {
            var report = new Report();
            using (var csv = new CsvReader(reader, new Configuration() { Delimiter = "~", BadDataFound = null }))
            {
                var doc = new Document();
                while (csv.Read())
                {
                    if (csv.TryGetField(0, out string type)) // skips blank lines
                    {
                        if (type.StartsWith("Schema_"))
                        {
                            type = type.Substring(7);
                            for (var col = 1; csv.TryGetField(col, out string field); ++col)
                            {
                                report.Schema.Add(type, field);
                            }
                        }
                        else
                        {
                            var row = new Row(type);
                            var cols = report.Schema.GetColumns(type);
                            for (int i = 1; csv.TryGetField(i, out string value); ++i)
                            {
                                row.Add(cols[i - 1], value);
                            }
                            if (doc.GetRowCount() > 0 && !rowBelongsToDocument(report.Schema, doc, row))
                            {
                                report.Add(doc);
                                doc = new Document();
                            }
                            doc.Add(row);
                        }
                    }
                }
                if (doc.GetRowCount() > 0)
                {
                    report.Add(doc);
                }
            }
            return report;
        }
    }
}
