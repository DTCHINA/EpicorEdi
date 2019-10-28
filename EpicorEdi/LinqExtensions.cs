using System.Collections.Generic;
using System.Linq;

namespace EpicorEdi
{
    public static class LinqExtensions
    {
        public static bool AllEqual<T>(this IEnumerable<T> sequence)
        {
            /* Empty sequence returns true for consistency with All. */
            return sequence.Count() < 2 || sequence.Skip(1).All(o => o.Equals(sequence.First()));
        }
    }
}
