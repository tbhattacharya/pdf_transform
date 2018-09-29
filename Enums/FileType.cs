using System;
namespace PDFTransformation
{
    [AttributeUsageAttribute(AttributeTargets.All)]
    public class FileType : System.Attribute
    {
        private readonly int id;
        private readonly string fileExtension;

        public FileType(int id, string fileExtension)
        {
            this.id = id;
            this.fileExtension = fileExtension;
        }

        public string FileExtension(){
            return this.fileExtension;
        }
    }
}
