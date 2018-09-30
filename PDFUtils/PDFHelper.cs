using System.IO;
using System;
using iTextSharp.text.pdf;
using iTextSharp.text;
using iTextSharp.text.pdf.parser;
using iTextSharp.xtra.iTextSharp.text.pdf.pdfcleanup;
using System.Collections.Generic;
using System.Drawing;
using System.Text.RegularExpressions;

namespace PDFTransformation.PDFUtils
{
    public static class PDFHelper
    {
        public static void ReOrderPages(string inputPdf, string pageSelection, string outputPdf)
        {
            //Bind a reader to our input file
            var reader = new PdfReader(inputPdf);

            //Create our output file, nothing special here
            using (FileStream fs = new FileStream(outputPdf, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                using (Document doc = new Document(reader.GetPageSizeWithRotation(1)))
                {
                    //Use a PdfCopy to duplicate each page
                    using (PdfCopy copy = new PdfCopy(doc, fs))
                    {
                        doc.Open();
                        copy.SetLinearPageMode();
                        for (int i = 1; i <= reader.NumberOfPages; i++)
                        {
                            copy.AddPage(copy.GetImportedPage(reader, i));
                        }
                        //Reorder pages
                        copy.ReorderPages(new int[] { 1, 2, 3, 4, 5, 8, 9, 10, 11, 12, 13, 6, 7, 14, 15 });
                        doc.Close();
                    }
                }
            }
        }

        public static string GetTextFromPages(String pdfPath, int[] pages)
        {
            PdfReader reader = new PdfReader(pdfPath);

            StringWriter output = new StringWriter();
            if (!String.IsNullOrEmpty(pdfPath) && pages != null && pages.Length == 0)
            {
                for (int i = pages[0]; i <= pages.Length; i++)
                {
                    output.WriteLine(PdfTextExtractor.GetTextFromPage(reader, i, new SimpleTextExtractionStrategy()));
                }
            }
            return output.ToString();
        }

        public static void UpdateFooterPagination(string inputPdf, string outputPdf)
        {

            PdfReader reader = new PdfReader(inputPdf);
            FileStream fs = new FileStream(outputPdf, FileMode.Create, FileAccess.Write);
            int n = reader.NumberOfPages;
            for (int i = 6; i <= n; i++)
            {
                PdfDictionary dict = reader.GetPageN(i);
                PdfObject obj = dict.GetDirectObject(PdfName.CONTENTS);
                if (obj.GetType() == typeof(PRStream))
                {
                    PRStream stream = (PRStream)obj;
                    byte[] data = PdfReader.GetStreamBytes(stream);
                    String oldStr = System.Text.Encoding.UTF8.GetString(data);

                    String pageString = MatchRegex(oldStr, @"\[\(Seite \)\]TJ.*\[\(");

                    //Regex replacement of page string
                    String updatedPageString = Regex.Replace(pageString, @"\[\(\d+\)\]", "[(" +i+")]");
                    String newString = Regex.Replace(oldStr, @"\[\(Seite \)\]TJ.*\[\(", updatedPageString, RegexOptions.Singleline);
                    stream.SetData(System.Text.Encoding.UTF8.GetBytes(newString));
                }
            }
            PdfStamper stamper = new PdfStamper(reader, fs);
            stamper.Close();
            reader.Close();
        }

        private static String MatchRegex(String input, String pattern)
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
