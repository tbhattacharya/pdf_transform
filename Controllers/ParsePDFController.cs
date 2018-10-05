using Microsoft.AspNetCore.Mvc;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using PDFTransformation.PDFUtils;
using System.Threading.Tasks;
using System;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace PDFTransformation.Controllers
{
    [Produces("application/pdf")]
    [Route("api/[controller]")]
    public class ParsePDFController : BaseController
    {
        public ParsePDFController(IHostingEnvironment hostingEnvironment) : base(hostingEnvironment)
        {

        }

        [HttpPost, DisableRequestSizeLimit]
        public ActionResult ParseFile(string fileName)
        {
            try
            {

                using (StreamReader reader = new StreamReader(Request.Body))
                {
                    //Read the given sequence for the new pdf
                    String content = reader.ReadToEnd();

                    //Define the upload and download paths
                    string fullUploadPath = System.IO.Path.Combine(_newUploadPath, fileName);
                    string downloadPath = System.IO.Path.Combine(_newDownloadPath, fileName);

                    //The pdf will be written multipl times. Use temp files and delete later
                    string temp1 = System.IO.Path.Combine(_newTempPath, "1" + fileName);
                    string temp2 = System.IO.Path.Combine(_newTempPath, "2" + fileName);
                    string tocPage = System.IO.Path.Combine(_newTempPath, "toc.pdf");

                    //Remove existing Table of contents from page 5 and create temp1 pdf
                    PDFHelper.ClearContents(fullUploadPath, temp1, 5);

                    //Re-order the pdf in the required sequence as given from temp1 and create temp2
                    PDFHelper.ReOrderPages(temp1, PDFHelper.CreateNewOrder(content), temp2);

                    //Remove the pagination and update
                    //PDFHelper.RemoveFooterPagination(tempPath, downloadPath);

                    //Update pagination in footer from temp2 and recreate temp1
                    PDFHelper.UpdateFooterPagination(temp2, temp1);

                    //Get page number for TOC and Dynamic Content

                    int pageNumTOC = PDFHelper.FindPageinContent(content, "Inhaltsverzeichnis");
                    int pageNumDCStart = PDFHelper.FindPageinContent(content, "Dynamic content");
                    int pageNumDCEnd = pageNumDCStart + 6; //6 pages of dynamic content as per given pdf

                    //Create just the TOC
                    //PDFHelper.CreateTOCPage(temp2, tocPage, pageNumTOC, pageNumDCStart, pageNumDCEnd);

                    //Generate new TOC for the dynamic conent only as given in the source pdf and create 
                    // the new file in download folder.
                    PDFHelper.CreateTOC(temp2, downloadPath, pageNumTOC, pageNumDCStart, pageNumDCEnd);

                    //Delete temp file
                    System.IO.File.Delete(temp1);
                    System.IO.File.Delete(temp2);

                    return Json("fileName:" + fileName);
                }
            }
            catch (System.Exception ex)
            {
                return Json("Upload Failed: " + ex.Message);
            }
        }

    }
}
