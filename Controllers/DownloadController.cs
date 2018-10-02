using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using Microsoft.Net.Http.Headers;
using System.IO;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace PDFTransformation.Controllers
{
    [Produces("application/pdf")]
    [Route("api/[controller]")]
    public class DownloadController : BaseController
    {

        public DownloadController(IHostingEnvironment hostingEnvironment): base(hostingEnvironment)
        {
           
        }

        [HttpGet]
        public IActionResult DownloadFile(string fileName)
        {
            try
            {
                FileInfo file = new FileInfo(Path.Combine(_newDownloadPath, fileName));
                IFileProvider provider = new PhysicalFileProvider(_newDownloadPath);
                IFileInfo fileInfo = provider.GetFileInfo(fileName);
                var readStream = fileInfo.CreateReadStream();
                string mimeType = "application/pdf";
                System.Net.Mime.ContentDisposition cd = new System.Net.Mime.ContentDisposition
                {
                    FileName = fileName,
                    Inline = false  // false = prompt the user for downloading;  true = browser to try to show the file inline
                };
                Response.Headers.Add("Content-Disposition", cd.ToString());
                return File(readStream, mimeType, fileName);
            }
            catch (System.Exception ex)
            {
                return Json("Download failed: " + ex.Message);
            }
        }
    }
}
