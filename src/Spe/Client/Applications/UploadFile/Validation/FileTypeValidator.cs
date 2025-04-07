using System;
using System.Linq;
using System.Web;

namespace Spe.Client.Applications.UploadFile.Validation
{
    internal class FileTypeValidator
    {
        private readonly Func<string, bool> _validate;

        public FileTypeValidator(string pattern)
        {
            if (pattern.Contains('.'))
            {
                _validate = fileName => fileName.EndsWith(pattern);
            }
            else if (pattern.Contains("/*"))
            {
                _validate = fileName => MimeMapping.GetMimeMapping(fileName).Split('/').FirstOrDefault() == pattern.Split('/').First();
            }
            else if (pattern.Contains("/"))
            {
                _validate = fileName => MimeMapping.GetMimeMapping(fileName) == pattern;
            }
            else
            {
                throw new NotSupportedException("Pattern isn't supported");
            }
        }

        public bool IsValid(string fileName)
        {
            return _validate(fileName);
        }
    }
}
