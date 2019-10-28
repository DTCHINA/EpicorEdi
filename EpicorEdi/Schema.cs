using System;
using System.Collections.Generic;

namespace EpicorEdi
{
    public class Schema
    {
        private readonly List<string> _tableOrder = new List<string>();
        private readonly Dictionary<string, List<string>> _schema = new Dictionary<string, List<string>>();

        internal void Add(string table, string field)
        {
            if(_schema.TryGetValue(table, out List<string> columns))
            {
                columns.Add(field);
            }
            else
            {
                _tableOrder.Add(table);
                _schema.Add(table, new List<string>() { field });
            }
        }

        internal int GetIndex(string table)
        {
            int i = _tableOrder.IndexOf(table);
            if (i == -1)
            {
                throw new InvalidOperationException(string.Format("Schema does not contain table '%s'.", table));
            }
            return i;
        }

        internal List<string> GetColumns(string table)
        {
            return _schema[table];
        }

        public Report Remap(IEnumerable<Document> docs)
        {
            var report = new Report(this); // a schema is never mutated after being exposed
            foreach(var doc in docs)
            {
                var newDoc = new Document();
                foreach(var table in _tableOrder)
                {
                    var rows = doc.GetRows(table);
                    foreach(var row in rows)
                    {
                        newDoc.Add(row.Remap(GetColumns(row.Type)));
                    }
                }
                report.Add(newDoc);
            }
            return report;
        }

        // TODO to and from EDI definition

        public override string ToString()
        {
            var tables = new List<string>(_schema.Count);
            foreach(var table in _tableOrder)
            {
                var columns = new List<string>(_schema[table].Count + 1);
                columns.Add("Schema_" + table);
                columns.AddRange(_schema[table]);
                tables.Add(string.Join("~", columns));
            }
            return string.Join("\r\n", tables);
        }
    }
}
