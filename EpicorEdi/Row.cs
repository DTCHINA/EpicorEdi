using System.Collections.Generic;
using System.Linq;

namespace EpicorEdi
{
    public class Row
    {
        public string Type { get; }
        private readonly List<string> _columnOrder = new List<string>();
        private readonly Dictionary<string, string> _data = new Dictionary<string, string>();

        internal Row(string type)
        {
            Type = type;
        }

        internal void Add(string column, string value)
        {
            _data.Add(column, value);
            _columnOrder.Add(column);
        }

        internal Row Remap(IEnumerable<string> columns, bool strict = false)
        {
            if(_columnOrder.SequenceEqual(columns))
            {
                return this;
            }
            var row = new Row(Type);
            var values = strict ? columns.Select(col => _data[col]) : columns.Select(col => _data.TryGetValue(col, out var value) ? value : "");
            row._data.EnsureCapacity(columns.Count());
            var ce = columns.GetEnumerator();
            var ve = values.GetEnumerator();
            while(ce.MoveNext())
            {
                ve.MoveNext();
                row.Add(ce.Current, ve.Current);
            }
            return row;
        }

        public string this[string column]
        {
            get { return _data[column]; }
        }

        public bool TryGetValue(string column, out string value)
        {
            return _data.TryGetValue(column, out value);
        }

        public override string ToString()
        {
            var columns = new List<string>(_data.Count + 1);
            columns.Add(Type);
            foreach(var col in _columnOrder)
            {
                columns.Add(_data[col]);
            }
            return string.Join('~', columns);
        }
    }
}
