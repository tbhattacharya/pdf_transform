using System.IO;
using System;
using iTextSharp.text.pdf;
using iTextSharp.text;
using iTextSharp.text.pdf.parser;
using iTextSharp.xtra.iTextSharp.text.pdf.pdfcleanup;
using System.Collections.Generic;

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

        public static void RemoveFooterPagination(string inputPdf, string outputPdf)
        {
            //Create our output file, nothing special here
            using (FileStream fs = new FileStream(outputPdf, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                //TaggedPdfReaderTool reader = new TaggedPdfReaderTool();
                //using (MemoryStream ms = new MemoryStream())
                //{
                //    reader.ConvertToXml(new PdfReader(inputPdf), ms);

                //}

                PdfReader reader = new PdfReader(inputPdf);
                PdfStamper stamper = new PdfStamper(reader, fs);
                List<PdfCleanUpLocation> cleanUpLocations = new List<PdfCleanUpLocation>();
                cleanUpLocations.Add(new PdfCleanUpLocation(12, new Rectangle(97f, 405f, 480f, 445f), BaseColor.GRAY));
                PdfCleanUpProcessor cleaner = new PdfCleanUpProcessor(cleanUpLocations, stamper);
                cleaner.CleanUp();
                stamper.Close();
                reader.Close();
            }
        }
        }
}
