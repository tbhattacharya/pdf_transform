using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using System.IO;
using Syncfusion.Pdf.Parsing;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace PDFTransformation.Controllers
{
    [Produces("application/pdf")]
    [Route("api/[controller]")]
    public class ParsePDFController : Controller
    {
        private readonly IHostingEnvironment _hostingEnvironment;

        public ParsePDFController(IHostingEnvironment hostingEnvironment)
        {
            _hostingEnvironment = hostingEnvironment;
        }

        [HttpPost, DisableRequestSizeLimit]
        public ActionResult UploadFile()
        {
            try
            {
                string wwwrootPath = _hostingEnvironment.WebRootPath;
                string folderName = "Upload";
                string newPath = Path.Combine(wwwrootPath, folderName);
                string fileName = @"SEP-Bericht Testaufgabe.pdf";
                string fullPath = Path.Combine(newPath, fileName);
                FileStream fileStreamInput = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
                PdfLoadedDocument loadedDocument = new PdfLoadedDocument(fileStreamInput);
                string extractedText = string.Empty;
                for (var i = 0; i < loadedDocument.Pages.Count; i++)
                {
                    extractedText += loadedDocument.Pages[i].ExtractText(true);
                }
                byte[] byteArray = System.Text.Encoding.UTF8.GetBytes(extractedText);
                MemoryStream stream = new MemoryStream(byteArray);
                FileStreamResult fileStreamResult = new FileStreamResult(stream, "application/txt");
                fileStreamResult.FileDownloadName = "Sample.txt";
                return fileStreamResult;
            }
            catch (System.Exception ex)
            {
                return Json("Upload Failed: " + ex.Message);
            }
        }
    }
}
