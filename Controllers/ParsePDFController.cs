using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using PDFTransformation.PDFUtils;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace PDFTransformation.Controllers
{
    [Produces("application/pdf")]
    [Route("api/[controller]")]
    public class ParsePDFController : Controller
    {
        readonly IHostingEnvironment _hostingEnvironment;
        readonly string _wwwrootPath;
        readonly string _folderUpload = "Upload";
        readonly string _folderDownload = "Download";
        readonly string _newUploadPath;
        readonly string _newDownloadPath;

        public ParsePDFController(IHostingEnvironment hostingEnvironment)
        {
            _hostingEnvironment = hostingEnvironment;
            _wwwrootPath = _hostingEnvironment.WebRootPath;
            _newUploadPath = System.IO.Path.Combine(_wwwrootPath, _folderUpload);
            _newDownloadPath = System.IO.Path.Combine(_wwwrootPath, _folderDownload);
            if (!Directory.Exists(_newDownloadPath))
            {
                Directory.CreateDirectory(_newDownloadPath);
            }
        }

        [HttpPost, DisableRequestSizeLimit]
        public ActionResult ParseFile(string fileName)
        {
            try
            {
                string fullUploadPath = System.IO.Path.Combine(_newUploadPath, fileName);
                string downloadPath = System.IO.Path.Combine(_newDownloadPath, fileName);

                //Re-order the pdf in the required sequence as given 
                //PDFHelper.ReOrderPages(fullUploadPath, "1-4,5,3,8-13,6,7,14-15", downloadPath);
                PDFHelper.RemoveFooterPagination(fullUploadPath, downloadPath);

                //Extract elements from pdf by pages and put back in specified order.

                return Json("fileName:" + fileName);
            }
            catch (System.Exception ex)
            {
                return Json("Upload Failed: " + ex.Message);
            }
        }
    }
}
