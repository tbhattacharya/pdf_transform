using System;
using iTextSharp.text.pdf;
using iTextSharp.text;
using System.Collections.Generic;

namespace PDFTransformation.PDFUtils
{
    /// <summary>
    /// The PDFPageEvent helper class to generate the TOC.
    /// </summary>
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

}
