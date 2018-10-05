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
        //Original document content to use as reference.
        private static Dictionary<string, string> _contents = new Dictionary<string, string>{
                    {"Title section", "1,2"},
                    {"Impressum", "3,4"},
                    {"Inhaltsverzeichnis", "5"},
                    {"Abbildungsverzeichnis", "6"},
                    {"Tabellenverzeichnis", "7"},
                    {"Dynamic content", "8,9,10,11,12,13"},
                    {"Abkürzungsverzeichnis", "14,15"}
                    };

        /// <summary>
        /// This method will create a new page order as per the order of contents provided based on the original pdf.
        /// </summary>
        /// <param name="content">
        /// The input content order in string
        /// </param>
        /// <returns>
        /// The order of page number in string
        /// </returns>
        public static string CreateNewOrder(string content)
        {
            List<string> result = new List<string>();
            JArray obj = (JArray)JObject.Parse(content)["order"];

            foreach (JValue item in obj)
            {
                //Match against the old content and get page numbers
                _contents.TryGetValue(item.Value.ToString(), out string res);
                result.Add(res);
            }
            //Return comma separated page number in new order
            return String.Join(",", result.ToArray());
        }

        /// <summary>
        /// Finds the page number of the section(page) in the content order (content).
        /// </summary>
        /// <returns>The page number.</returns>
        /// <param name="content">Content order.</param>
        /// <param name="page">The sextion for which page number is needed.</param>
        public static int FindPageinContent(string content, string page)
        {
            List<string> result = new List<string>();
            JArray obj = (JArray)JObject.Parse(content)["order"];
            foreach (JValue item in obj)
            {
                //Match against the old content and get page numbers
                _contents.TryGetValue(item.Value.ToString(), out string res);
                //If it matched the section
                if (item.Value.ToString() == page)
                {
                    string[] pages = String.Join(",", result.ToArray()).Split(",");
                    //Return the new page number for the section
                    return (pages.Length + 1);
                }
                else
                {
                    result.Add(res);
                }
            }
            return -1;
        }

        /// <summary>
        /// Re-order the pages of a given pdf and create a new PDF at the output file
        /// </summary>
        /// <param name="inputPdf">Input pdf</param>
        /// <param name="pageSelection">The new order (comma separated pagenumber).</param>
        /// <param name="outputPdf">Output pdf.</param>
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
                            //Check if tagged pdf
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

        /// <summary>
        /// Extract the text from pdf. And write to a text file.
        /// </summary>
        /// <returns>The text from pages.</returns>
        /// <param name="pdfPath">The Pdf to read from.</param>
        /// <param name="pages">The page numbers to read from.</param>
        /// <param name="outputfile">Outputfile to write the text.</param>
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

        /// <summary>
        /// Gets the contents text from page to put in TOC.
        /// </summary>
        /// <returns>The name of the page.</returns>
        /// <param name="pdfPath">The Pdf to read from.</param>
        /// <param name="page">The pages to read from.</param>
        public static string GetContentsTextFromPage(String pdfPath, int page)
        {
            PdfReader reader = new PdfReader(pdfPath);
            StringWriter output = new StringWriter();

            //Create rectangle to read from header
            Rectangle mediabox = reader.GetPageSize(page);
            float llx = mediabox.GetRight(10f) - 100f;
            float urx = mediabox.GetRight(0f);
            float lly = mediabox.GetTop(10f) - 50f;
            float ury = mediabox.GetTop(0f);
            Rectangle rect = new Rectangle(llx, lly, urx, ury);

            //The header contains the name of the page. Read from Heaedr.
            RenderFilter regionFilter = new RegionTextRenderFilter(rect);
            ITextExtractionStrategy strategy = new FilteredTextRenderListener(
                    new LocationTextExtractionStrategy(), regionFilter);
            output.WriteLine(PdfTextExtractor.GetTextFromPage(reader, page, strategy));
            Console.WriteLine(output.ToString());
            string ret = output.ToString();
            //Remove newline characters
            return Regex.Replace(ret, @"\t|\n|\r", "");

        }

        /// <summary>
        /// Updates the pagination in the footer.
        /// </summary>
        /// <param name="inputPdf">The pdf to modify.</param>
        /// <param name="outputPdf">The pdf created with updated pagination.</param>
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

                    //Get the string matching the pagination
                    String pageString = CommonUtils.MatchRegex(oldStr, @"\[\(Seite \)\]TJ.*\[\(");

                    //Regex replacement of page string with updated page number
                    String updatedPageString = Regex.Replace(pageString, @"\[\(\d+\)\]", "[(" + i + ")]");
                    String newString = Regex.Replace(oldStr, @"\[\(Seite \)\]TJ.*\[\(", updatedPageString, RegexOptions.Singleline);
                    stream.SetData(System.Text.Encoding.UTF8.GetBytes(newString));
                }
            }
            PdfStamper stamper = new PdfStamper(reader, fs);
            stamper.Close();
            reader.Close();
        }

        /// <summary>
        /// Removes the pagination from the footer.
        /// </summary>
        /// <param name="inputPdf">The pdf to modify.</param>
        /// <param name="outputPdf">The pdf created with removed pagination.</param>
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

        /// <summary>
        /// Clears the contents of a given page leaving header and footer.
        /// </summary>
        /// <param name="inputPdf">The pdf to modify.</param>
        /// <param name="outputPdf">The pdf created after modification.</param>
        /// <param name="pageNum">The page number to be cleared.</param>
        public static void ClearContents(string inputPdf, string outputPdf, int pageNum)
        {
            using (FileStream fs = new FileStream(outputPdf, FileMode.Create, FileAccess.Write))
            {
                PdfReader reader = new PdfReader(inputPdf);
                PdfStamper stamper = new PdfStamper(reader, fs);

                //Get the reactangle leaving header and footer
                Rectangle mediabox = reader.GetPageSize(pageNum);
                float llx = mediabox.GetLeft(10f);
                float urx = mediabox.GetRight(10f);
                float lly = mediabox.GetBottom(10f) + 100f; // Leave footer
                float ury = mediabox.GetTop(10f) - 120f; //Leave header

                List<PdfCleanUpLocation> cleanUpLocations = new List<PdfCleanUpLocation>
                {
                    new PdfCleanUpLocation(pageNum, new iTextSharp.text.Rectangle(llx, lly, urx, ury), BaseColor.WHITE)
                };
                //Use the PdfCleanUpProcessor to clean the page
                PdfCleanUpProcessor cleaner = new PdfCleanUpProcessor(cleanUpLocations, stamper);
                cleaner.CleanUp();
                stamper.Close();
                reader.Close();
            }
        }

        /// <summary>
        /// Recreate the pdf with a Table of Contents at the given location.
        /// </summary>
        /// <param name="inputPdf">The pdf to read from.</param>
        /// <param name="outputPdf">The pdf created with the TOC.</param>
        /// <param name="pageTOC">Page number for TOC.</param>
        /// <param name="pageStart">Starting page for TOC.</param>
        /// <param name="pageEnd">Ending page for TOC.</param>
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

                        //Get the rectangle to copy and paste
                        Rectangle mediabox = reader.GetPageSize(1);
                        float llx = mediabox.GetLeft(0f);
                        float lly = mediabox.GetBottom(0f);

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

                        //Start copying the pdf
                        for (int i = 1; i <= reader.NumberOfPages; i++)
                        {
                            if (i == pageTOC) //This is the TOC page.
                            {
                                //Add header and footer
                                doc.NewPage();
                                PdfImportedPage page = writer.GetImportedPage(reader, i);
                                cb.AddTemplate(page, llx, lly);

                                //Create TOC
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
                            else // other pages write
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

        /// <summary>
        /// Creates just the TOC page.
        /// </summary>
        /// <param name="inputPdf">The pdf to read from.</param>
        /// <param name="outputPdf">The toc pdf.</param>
        /// <param name="pageTOC">Page number for TOC page.</param>
        /// <param name="pageStart">The start of Contents for TOC.</param>
        /// <param name="pageEnd">The end of contents for TOC.</param>
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
                            toc.Add(new PageIndex() { Text = text, Name = "dest" + (i), Page = i });
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
