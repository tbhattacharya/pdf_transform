using System;
using System.Linq;
using System.Text.RegularExpressions;
using PDFTransformation.Enums;

namespace PDFTransformation
{
    public static class CommonUtils
    {
        public static string GenerateFileNames(string type)
        {
            Random random = new Random(DateTime.Now.Second);
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return (new string(Enumerable.Repeat(chars, 10)
                               .Select(s => s[random.Next(s.Length)]).ToArray()) + type);
        }

        public static String MatchRegex(String input, String pattern)
        {
            //Regex word = new Regex(@"\[\(Seite \)\]TJ.*/s");
            Match match = Regex.Match(input, pattern, RegexOptions.Singleline);
            while (match.Success)
            {
                return match.Value;
            }
            return "";
        }
    }
}
