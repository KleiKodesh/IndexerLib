using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace IndexerLib.Helpers
{
    public static class StringExtensions
    {
        /// <summary>
        /// Determines whether a character is a Hebrew letter or diacritic (U+0591–U+05C7).
        /// </summary>
        private static bool IsHebrewLetterOrDiacritic(char c)
        {
            return char.IsLetter(c) || (c >= '\u0591' && c <= '\u05C7');
        }

        /// <summary>
        /// Removes Hebrew diacritics (nikud and cantillation marks) from a string.
        /// </summary>
        public static string RemoveHebrewDiactrics(this string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            var sb = new StringBuilder(input.Length);

            foreach (var c in input)
                if (c > 1487 || c < 1425) // Range 1425–1487 contains Hebrew diacritics
                    sb.Append(c);

            return sb.ToString();
        }

        private static readonly Regex HtmlTagRegex = new Regex("<.*?>", RegexOptions.Compiled);

        public static string RemoveHtmlTags(this string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            return HtmlTagRegex.Replace(input, string.Empty);
        }
    }
}
