using System.IO;

namespace VizGurka.Helpers
{
    public static class ImageHelper
    {
        public static void CopyImageFilesToWwwroot()
        {
            string sourceDirectory = Path.Combine(Directory.GetCurrentDirectory(), "bin", "Debug", "net8.0", "GurkaFiles");
            string destinationDirectory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");

            if (!Directory.Exists(destinationDirectory))
            {
                Directory.CreateDirectory(destinationDirectory);
            }

            string[] extensions = new[] { "*.png", "*.jpeg", "*.jpg", "*.svg", "*.gif" };

            // Copy all files with the specified extensions to the destination directory
            {
                string[] files = Directory.GetFiles(sourceDirectory, extension);
                foreach (string file in files)
                {
                    string fileName = Path.GetFileName(file);
                    string destFile = Path.Combine(destinationDirectory, fileName);
                    File.Copy(file, destFile, true);
                }
            }
        }
    }
}