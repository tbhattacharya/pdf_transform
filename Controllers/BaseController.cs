using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace PDFTransformation.Controllers
{
    public class BaseController : Controller
    {
        //Variables to be used frequently in the operation
        readonly IHostingEnvironment _hostingEnvironment;
        protected readonly string _wwwrootPath;
        protected readonly string _folderUpload = "Upload";
        protected readonly string _folderDownload = "Download";
        protected readonly string _folderTemp = "Temp";
        protected readonly string _newUploadPath;
        protected readonly string _newTempPath;
        protected readonly string _newDownloadPath;

        public BaseController(IHostingEnvironment hostingEnvironment)
        {
            _hostingEnvironment = hostingEnvironment;
            _wwwrootPath = _hostingEnvironment.WebRootPath;
            _newUploadPath = System.IO.Path.Combine(_wwwrootPath, _folderUpload);
            _newTempPath = System.IO.Path.Combine(_wwwrootPath, _folderTemp);
            _newDownloadPath = System.IO.Path.Combine(_wwwrootPath, _folderDownload);
            if (!Directory.Exists(_newDownloadPath))
            {
                Directory.CreateDirectory(_newDownloadPath);
            }
            if (!Directory.Exists(_newTempPath))
            {
                Directory.CreateDirectory(_newTempPath);
            }
        }
    }
}
