using System.Collections.Generic;
using System.Linq;

namespace EpicorEdi
{
    public class Document
    {
        private readonly List<Row> _rows = new List<Row>();

        internal void Add(Row row)
        {
            _rows.Add(row);
        }

        internal void InsertRange(int index, IEnumerable<Row> rows)
        {
            _rows.InsertRange(index, rows);
        }

        internal bool HasRowType(string type)
        {
            return _rows.Any(o => o.Type == type);
        }

        internal int FindIndex(string type)
        {
            return _rows.FindIndex(o => o.Type == type);
        }

        internal Row GetFirstRow()
        {
            return _rows.First();
        }

        internal Row GetLastRow()
        {
            return _rows.Last();
        }

        internal int GetRowCount()
        {
            return _rows.Count();
        }

        public bool IsInSchemaOrder(Schema schema)
        {
            var rows = _rows.Select(o => o.Type).Distinct();
            for (int i = 1; i < rows.Count(); ++i)
            {
                if (schema.GetIndex(rows.ElementAt(i)) < schema.GetIndex(rows.ElementAt(i - 1)))
                {
                    return false;
                }
            }
            return true;
        }

        public IEnumerable<Row> GetRows()
        {
            return _rows.AsReadOnly();
        }

        public IEnumerable<Row> GetRows(string type)
        {
            return _rows.Where(o => o.Type == type);
        }

        public override string ToString()
        {
            return string.Join("\r\n", _rows);
        }
    }
}
