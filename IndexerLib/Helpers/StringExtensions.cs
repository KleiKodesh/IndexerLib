using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SimplifiedIndexerLib.Helpers
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

        public static bool IsHebrewOrLatinLetter(this char c)
    => (c >= 'A' && c <= 'Z') ||
       (c >= 'a' && c <= 'z') ||
       (c >= 'א' && c <= 'ת');

        public static bool IsDiacritic(this char c)
        {
            // Hebrew nikud + taamim (but exclude maqaf \u05BE)
            if (c >= '\u0591' && c <= '\u05C7' && c != '\u05BE')
                return true;

            // Combining Diacritical Marks (U+0300–U+036F)
            if (c >= '\u0300' && c <= '\u036F')
                return true;

            // Combining Diacritical Marks Extended (U+1AB0–U+1AFF)
            if (c >= '\u1AB0' && c <= '\u1AFF')
                return true;

            // Combining Diacritical Marks Supplement (U+1DC0–U+1DFF)
            if (c >= '\u1DC0' && c <= '\u1DFF')
                return true;

            // Combining Diacritical Marks for Symbols (U+20D0–U+20FF)
            if (c >= '\u20D0' && c <= '\u20FF')
                return true;

            // Combining Half Marks (U+FE20–U+FE2F)
            if (c >= '\uFE20' && c <= '\uFE2F')
                return true;

            return false;
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
