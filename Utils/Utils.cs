using System;
using System.Linq;
using PDFTransformation.Enums;

namespace PDFTransformation
{
    public class CommonUtils
    {
        public static string GenerateFileNames(string type)
        {
            Random random = new Random(DateTime.Now.Second);
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return (new string(Enumerable.Repeat(chars, 10)
                               .Select(s => s[random.Next(s.Length)]).ToArray()) + type);
        }
    }
}
