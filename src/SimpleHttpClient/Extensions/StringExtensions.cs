using System;

namespace SimpleHttpClient.Extensions
{
    /// <summary>
    /// Extensions for strings
    /// </summary>
    internal static class StringExtensions
    {
        /// <summary>
        /// Allow using Contains with string comparison
        /// </summary>
        internal static bool Contains(this string source, string toCheck, StringComparison comp) =>
            source?.IndexOf(toCheck, comp) >= 0;

        /// <summary>
        /// !string.IsNullOrWhiteSpace shorthand
        /// </summary>
        internal static bool HasValue(this string source) =>
            !string.IsNullOrWhiteSpace(source);
    }
}
