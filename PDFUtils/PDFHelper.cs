using System.IO;
using System;
using System.Linq;
using iTextSharp.text.pdf;
using iTextSharp.text;
using iTextSharp.text.pdf.parser;
using iTextSharp.text.pdf.draw;
using iTextSharp.xtra.iTextSharp.text.pdf.pdfcleanup;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using System.Drawing;
using System.Text.RegularExpressions;
using iTextSharp.awt.geom;
using Rectangle = iTextSharp.text.Rectangle;

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

        public static string CreateNewOrder(string content)
        {
            List<string> result = new List<string>();
            JArray obj = (JArray)JObject.Parse(content)["order"];
            foreach (JValue item in obj)
            {
                _contents.TryGetValue(item.Value.ToString(), out string res);
                result.Add(res);
            }
            return String.Join(",", result.ToArray());
        }

        public static int FindPageinContent(string content, string page)
        {
            List<string> result = new List<string>();
            JArray obj = (JArray)JObject.Parse(content)["order"];
            foreach (JValue item in obj)
            {
                _contents.TryGetValue(item.Value.ToString(), out string res);
                if (item.Value.ToString() == page)
                {
                    string[] pages = String.Join(",", result.ToArray()).Split(",");
                    return (pages.Length + 1);
                }
                else
                {
                    result.Add(res);
                }
            }
            return -1;
        }

        public static void ReOrderPages(string inputPdf, string pageSelection, string outputPdf)
        {
            //Bind a reader to our input file
            PdfReader reader = new PdfReader(inputPdf);

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
                            PdfImportedPage page = copy.GetImportedPage(reader, i);
                            Boolean tagged = page.IsTagged();

                            copy.AddPage(page);
                        }

                        //Reorder pages
                        int[] newOrder = pageSelection.Split(',').Select(n => int.Parse(n)).ToArray();
                        copy.ReorderPages(newOrder);
                        doc.Close();
                    }
                }
            }
        }

        public static string GetTextFromPages(String pdfPath, int[] pages, string outputfile)
        {
            PdfReader reader = new PdfReader(pdfPath);

            StringWriter output = new StringWriter();
            using (System.IO.StreamWriter file =
                   new System.IO.StreamWriter(outputfile, true))
            {
                if (!String.IsNullOrEmpty(pdfPath) && pages != null && pages.Length != 0)
                {
                    for (int i = pages[0]; i <= pages[pages.Length - 1]; i++)
                    {
                        file.WriteLine(PdfTextExtractor.GetTextFromPage(reader, i, new SimpleTextExtractionStrategy()));
                    }
                }
            }

            return output.ToString();
        }

        public static string GetContentsTextFromPage(String pdfPath, int page)
        {
            PdfReader reader = new PdfReader(pdfPath);
            StringWriter output = new StringWriter();
            Rectangle mediabox = reader.GetPageSize(page);
            float llx = mediabox.GetRight(10f) - 100f;
            float urx = mediabox.GetRight(0f);
            float lly = mediabox.GetTop(10f) - 50f;
            float ury = mediabox.GetTop(0f);
            Rectangle rect = new Rectangle(llx, lly, urx, ury);
            RenderFilter regionFilter = new RegionTextRenderFilter(rect);
            ITextExtractionStrategy strategy = new FilteredTextRenderListener(
                    new LocationTextExtractionStrategy(), regionFilter);
            output.WriteLine(PdfTextExtractor.GetTextFromPage(reader, page, strategy));
            Console.WriteLine(output.ToString());
            string ret = output.ToString();
            return Regex.Replace(ret, @"\t|\n|\r", "");

        }

        public static void UpdateFooterPagination(string inputPdf, string outputPdf)
        {

            PdfReader reader = new PdfReader(inputPdf);
            FileStream fs = new FileStream(outputPdf, FileMode.Create, FileAccess.Write);
            int n = reader.NumberOfPages;
            for (int i = 1; i <= n; i++)
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

        public static void RemoveFooterPagination(string inputPdf, string outputPdf)
        {
            using (FileStream fs = new FileStream(outputPdf, FileMode.Create, FileAccess.Write))
            {
                //TaggedPdfReaderTool reader = new TaggedPdfReaderTool();
                //using (MemoryStream ms = new MemoryStream())
                //{
                //    reader.ConvertToXml(new PdfReader(inputPdf), ms);

                //} // Not a tagged pdf

                PdfReader reader = new PdfReader(inputPdf);
                PdfStamper stamper = new PdfStamper(reader, fs);
                List<PdfCleanUpLocation> cleanUpLocations = new List<PdfCleanUpLocation>();
                int n = reader.NumberOfPages;
                for (int i = 6; i <= n; i++)
                {
                    cleanUpLocations.Add(new PdfCleanUpLocation(i, new iTextSharp.text.Rectangle(400, 0, 440, 40), BaseColor.WHITE));
                }
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

        public static void ClearContents(string inputPdf, string outputPdf, int pageNum)
        {
            using (FileStream fs = new FileStream(outputPdf, FileMode.Create, FileAccess.Write))
            {
                PdfReader reader = new PdfReader(inputPdf);
                PdfStamper stamper = new PdfStamper(reader, fs);
                Rectangle mediabox = reader.GetPageSize(pageNum);
                float llx = mediabox.GetLeft(10f);
                float urx = mediabox.GetRight(10f);
                float lly = mediabox.GetBottom(10f) + 100f; // Leave footer
                float ury = mediabox.GetTop(10f) - 120f; //Leave header

                List<PdfCleanUpLocation> cleanUpLocations = new List<PdfCleanUpLocation>
                {
                    new PdfCleanUpLocation(pageNum, new iTextSharp.text.Rectangle(llx, lly, urx, ury), BaseColor.WHITE)
                };
                PdfCleanUpProcessor cleaner = new PdfCleanUpProcessor(cleanUpLocations, stamper);
                cleaner.CleanUp();
                stamper.Close();
                reader.Close();
            }
        }

        //Generating all contents not working
        public static void CreateTOC(string inputPdf, string outputPdf, int pageTOC, int pageStart, int pageEnd)
        {
            //Bind a reader to our input file
            using (PdfReader reader = new PdfReader(inputPdf))
            {
                using (FileStream fs = new FileStream(outputPdf, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
                {
                    using (Document doc = new Document(reader.GetPageSizeWithRotation(1)))
                    {
                        PdfWriter writer = PdfWriter.GetInstance(doc, fs);
                        doc.Open();
                        PdfContentByte cb = writer.DirectContentUnder;
                        Rectangle mediabox = reader.GetPageSize(1);
                        float llx = mediabox.GetLeft(0f);
                        float lly = mediabox.GetBottom(0f); // Leave footer

                        //Not working
                        //TOCEvent evento = new TOCEvent(); 
                        //writer.PageEvent = evento;

                        //Create the list of pageIndex manulally.
                        List<PageIndex> toc = new List<PageIndex>();
                        for (int i = pageStart; i <= pageEnd; i++)
                        {
                            String text = GetContentsTextFromPage(inputPdf, i);
                            toc.Add(new PageIndex() { Text = text, Name = text, Page = i });
                        }

                        for (int i = 1; i <= reader.NumberOfPages; i++)
                        {
                            if (i == pageTOC)
                            {
                                doc.NewPage();
                                PdfImportedPage page = writer.GetImportedPage(reader, i);
                                cb.AddTemplate(page, llx, lly);
                                Chunk dottedLine = new Chunk(new DottedLineSeparator());
                                // Change later for drawing with rectangle
                                Paragraph gap = new Paragraph
                                {
                                    "\n\n\n\n\n\n\n"
                                };
                                doc.Add(gap);

                                Paragraph p;
                                foreach (PageIndex pageIndex in toc)
                                {
                                    Chunk chunk = new Chunk(pageIndex.Text);
                                    chunk.SetAction(PdfAction.GotoLocalPage(pageIndex.Name, false));
                                    p = new Paragraph(chunk)
                                        {
                                            dottedLine
                                        };

                                    chunk = new Chunk(pageIndex.Page.ToString());
                                    chunk.SetAction(PdfAction.GotoLocalPage(pageIndex.Name, false));
                                    p.Add(chunk);
                                    doc.Add(p);
                                }
                            }
                            else
                            {
                                doc.NewPage();
                                PdfImportedPage page = writer.GetImportedPage(reader, i);
                                cb.AddTemplate(page, llx, lly);
                            }
                        }
                    }
                }
            }
        }

        //Generate just TOC page
        public static void CreateTOCPage(string inputPdf, string outputPdf, int pageTOC, int pageStart, int pageEnd)
        {
            //Bind a reader to our input file
            using (PdfReader reader = new PdfReader(inputPdf))
            {
                using (FileStream fs = new FileStream(outputPdf, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
                {
                    using (Document doc = new Document(reader.GetPageSizeWithRotation(1)))
                    {
                        PdfWriter writer = PdfWriter.GetInstance(doc, fs);
                        doc.Open();

                        //Create the list of pageIndex manulally.
                        List<PageIndex> toc = new List<PageIndex>();
                        for (int i = pageStart; i <= pageEnd; i++)
                        {
                            String text = GetContentsTextFromPage(inputPdf, i);
                            toc.Add(new PageIndex() { Text = text, Name = text, Page = i });
                        }

                        doc.NewPage();
                        Chunk dottedLine = new Chunk(new DottedLineSeparator());
                        Paragraph p;
                        foreach (PageIndex pageIndex in toc)
                        {
                            Chunk chunk = new Chunk(pageIndex.Text);
                            chunk.SetAction(PdfAction.GotoLocalPage(pageIndex.Name, false));
                            p = new Paragraph(chunk)
                                        {
                                            dottedLine
                                        };

                            chunk = new Chunk(pageIndex.Page.ToString());
                            chunk.SetAction(PdfAction.GotoLocalPage(pageIndex.Name, false));
                            p.Add(chunk);
                            doc.Add(p);
                        }
                    }
                }
            }
        }
    }
}

public class TOCEvent : PdfPageEventHelper
{
    protected int counter = 0;
    protected List<PageIndex> toc = new List<PageIndex>();

    public override void OnGenericTag(PdfWriter writer, Document document, iTextSharp.text.Rectangle rect, string text)
    {
        String name = "dest" + (counter++);
        int page = writer.PageNumber;
        toc.Add(new PageIndex() { Text = text, Name = name, Page = page });
        writer.DirectContent.LocalDestination(name, new PdfDestination(PdfDestination.FITH, rect.GetTop(0)));
    }

    public List<PageIndex> GetTOC()
    {
        return toc;
    }
}

public class PageIndex
{
    public string Text { get; set; }
    public string Name { get; set; }
    public int Page { get; set; }

}
