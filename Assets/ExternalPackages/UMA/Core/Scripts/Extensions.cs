using System;
using System.Collections.Generic;
using System.Linq;

namespace UMA
{
    internal static class Extensions
    {
        public static bool ContainsIgnoreCamelCase(this ICollection<string> collection, string target)
        {
            return collection.Any(x => string.Equals(x, target, StringComparison.OrdinalIgnoreCase));
        }
    }
}