using System;
using System.Linq;
using System.Text.RegularExpressions;
using PDFTransformation.Enums;

namespace PDFTransformation
{
    public static class CommonUtils
    {
        /// <summary>
        /// Generates the file names by using Random Generator for uploaded files.
        /// </summary>
        /// <returns>The file names.</returns>
        /// <param name="type">Type/extention of the file.</param>
        public static string GenerateFileNames(string type)
        {
            Random random = new Random(DateTime.Now.Second);
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return (new string(Enumerable.Repeat(chars, 10)
                               .Select(s => s[random.Next(s.Length)]).ToArray()) + type);
        }

        /// <summary>
        /// Matchs the regex on the given string and return the matching string.
        /// </summary>
        /// <returns>The string matching the regex pattern.</returns>
        /// <param name="input">String on which regex matching is to be run.</param>
        /// <param name="pattern">The regex pattern to be matched.</param>
        public static String MatchRegex(String input, String pattern)
        {
            Match match = Regex.Match(input, pattern, RegexOptions.Singleline);
            while (match.Success)
            {
                return match.Value;
            }
            return "";
        }
    }
}
