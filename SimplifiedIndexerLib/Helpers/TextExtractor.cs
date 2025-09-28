using System.IO;
using System.Text;
using Docnet.Core;          // Docnet library
using Docnet.Core.Models;

namespace SimplifiedIndexerLib.Helpers
{
    public static class TextExtractor
    {
        public static string GetText(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return string.Empty;

            string extension = Path.GetExtension(filePath).ToLower();

            if (extension.EndsWith("txt"))
            {
                // read plain text file
                return File.ReadAllText(filePath);
            }
            else if (extension.EndsWith("pdf"))
            {
                var stb = new StringBuilder();
                using (var docLib = DocLib.Instance)
                using (var docReader = docLib.GetDocReader(File.ReadAllBytes(filePath), new PageDimensions()))
                    for (int i = 0; i < docReader.GetPageCount(); i++)
                        using (var pageReader = docReader.GetPageReader(i))
                            stb.Append(pageReader.GetText()); // get text per page
               
                return stb.ToString();
            }

            return string.Empty;
        }
    }
}
