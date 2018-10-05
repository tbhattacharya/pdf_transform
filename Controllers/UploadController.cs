using System.IO;
using System;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace PDFTransformation.Controllers
{
    [Produces("application/json")]
    [Route("api/[controller]")]
    public class UploadController : BaseController
    {

        public UploadController(IHostingEnvironment hostingEnvironment): base(hostingEnvironment)
        {

        }

        [HttpPost, DisableRequestSizeLimit]
        public ActionResult UploadFile()
        {
            try
            {
                IFormFile file = Request.Form.Files[0];

                //Generate a file name to save in serevr
                string fileName = CommonUtils.GenerateFileNames(".pdf");
                if (file.Length > 0)
                {
                    string fullPath = Path.Combine(_newUploadPath, fileName);
                    using (FileStream stream = new FileStream(fullPath, FileMode.Create))
                    {
                        file.CopyTo(stream);
                    }
                }
                //Return the file name to be used later
                return Json("fileName:"+ fileName);
            }
            catch (System.Exception ex)
            {
                return Json("Upload Failed: " + ex.Message);
            }
        }
    }
}
