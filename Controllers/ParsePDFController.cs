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
        public ParsePDFController(IHostingEnvironment hostingEnvironment): base(hostingEnvironment)
        {

        }

        [HttpPost, DisableRequestSizeLimit]
        public ActionResult ParseFile(string fileName)
        {
            try
            {

                using (var reader = new StreamReader(Request.Body))
                {
                    String content = reader.ReadToEnd();
                    string fullUploadPath = System.IO.Path.Combine(_newUploadPath, fileName);
                    string tempPath = System.IO.Path.Combine(_newTempPath, fileName);
                    string downloadPath = System.IO.Path.Combine(_newDownloadPath, fileName);

                    //Re-order the pdf in the required sequence as given 
                    PDFHelper.ReOrderPages(fullUploadPath, PDFHelper.CreateNewOrder(content), tempPath);
                    //Remove the pagination and update
                    //PDFHelper.RemoveFooterPagination(tempPath, downloadPath);

                    //Update pagination in footer
                    PDFHelper.UpdateFooterPagination(tempPath, downloadPath);

                    //Create title

                    //Delete temp file
                    System.IO.File.Delete(tempPath);

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
