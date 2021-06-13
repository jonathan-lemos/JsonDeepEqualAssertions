using System.Collections.Generic;

namespace JsonDeepEqualAssertions
{
    internal static class Extensions
    {
        public static IEnumerable<(int Index, T Element)> Enumerate<T>(this IEnumerable<T> elements)
        {
            var ct = 0;
            foreach (var e in elements)
            {
                yield return (ct, e);
                ct += 1;
            }
        }
    }
}