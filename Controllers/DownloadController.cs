using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using System.IO;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace PDFTransformation
{
    [Produces("application/pdf")]
    [Route("api/[controller]")]
    public class DownloadController : Controller
    {
        private readonly IHostingEnvironment _hostingEnvironment;

        public DownloadController(IHostingEnvironment hostingEnvironment)
        {
            _hostingEnvironment = hostingEnvironment;
        }

        [HttpGet]
        public IActionResult DownloadFile()
        {
            try
            {
                string wwwrootPath = _hostingEnvironment.WebRootPath;
                string folderName = "Upload";
                string newPath = Path.Combine(wwwrootPath, folderName);
                string fileName = @"SEP-Bericht Testaufgabe.pdf";
                FileInfo file = new FileInfo(Path.Combine(newPath, fileName));
                IFileProvider provider = new PhysicalFileProvider(newPath);
                IFileInfo fileInfo = provider.GetFileInfo(fileName);
                var readStream = fileInfo.CreateReadStream();
                string mimeType = @"application/pdf";
                return File(readStream, mimeType, fileName);
            }
            catch (System.Exception ex)
            {
                return Json("Download failed: " + ex.Message);
            }
        }
    }
}
