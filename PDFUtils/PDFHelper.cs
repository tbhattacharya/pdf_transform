using System.IO;
using System;
using System.Linq;
using iTextSharp.text.pdf;
using iTextSharp.text;
using iTextSharp.text.pdf.parser;
using iTextSharp.xtra.iTextSharp.text.pdf.pdfcleanup;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using System.Drawing;
using System.Text.RegularExpressions;

namespace PDFTransformation.PDFUtils
{
    public static class PDFHelper
    {
        private static Dictionary<string, string> _contents = new Dictionary<string, string>{
                    {"Title section", "1,2"},
                    {"Impressum", "3,4"},
                    {"Inhaltsverzeichnis", "5"},
                    {"Abbildungsverzeichnis", "6"},
                    {"Tabellenverzeichnis", "7"},
                    {"Dynamic content", "8,9,10,11,12,13"},
                    {"Abkürzungsverzeichnis", "14,15"}
                    };

        public static string CreateNewOrder(string content){
            List<string> result = new List<string>();
            JArray obj = (JArray)JObject.Parse(content)["order"];
            foreach (JValue item in obj)
            {
                _contents.TryGetValue(item.Value.ToString(), out string res);
                result.Add(res);
            }
            return String.Join(",", result.ToArray());
        }

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
                        int[] newOrder = pageSelection.Split(',').Select(n => int.Parse(n)).ToArray();
                        copy.ReorderPages(newOrder);
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

                    String pageString = CommonUtils.MatchRegex(oldStr, @"\[\(Seite \)\]TJ.*\[\(");

                    //Regex replacement of page string
                    String updatedPageString = Regex.Replace(pageString, @"\[\(\d+\)\]", "[(" + i + ")]");
                    String newString = Regex.Replace(oldStr, @"\[\(Seite \)\]TJ.*\[\(", updatedPageString, RegexOptions.Singleline);
                    stream.SetData(System.Text.Encoding.UTF8.GetBytes(newString));
                }
            }
            PdfStamper stamper = new PdfStamper(reader, fs);
            stamper.Close();
            reader.Close();
        }

        public static void RemoveFooterPagination(string inputPdf, string outputPdf){
            using (FileStream fs = new FileStream(outputPdf, FileMode.Create, FileAccess.Write))
            {
                //TaggedPdfReaderTool reader = new TaggedPdfReaderTool();
                //using (MemoryStream ms = new MemoryStream())
                //{
                //    reader.ConvertToXml(new PdfReader(inputPdf), ms);

                //} // Not a tagged pdf

                PdfReader reader = new PdfReader(inputPdf);
                PdfStamper stamper = new PdfStamper(reader, fs);
                List<PdfCleanUpLocation> cleanUpLocations = new List<PdfCleanUpLocation>
                {
                    new PdfCleanUpLocation(12, new iTextSharp.text.Rectangle(400,0,440,40), BaseColor.WHITE)
                };
                PdfCleanUpProcessor cleaner = new PdfCleanUpProcessor(cleanUpLocations, stamper);
                cleaner.CleanUp();

                //If clean up does not work add a white image
                iTextSharp.text.Image image = iTextSharp.text.Image.GetInstance(new Bitmap(120, 20), BaseColor.WHITE);
                image.SetAbsolutePosition(400, 40);
                //Adds the image to the output pdf
                stamper.GetOverContent(1).AddImage(image, true);
                stamper.Close();
                reader.Close();
            }
        }
    }
}
