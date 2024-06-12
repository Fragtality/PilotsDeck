using System.IO;
using System.IO.Compression;

namespace Extensions
{
    public static class ZipArchiveExtensions
    {
        public static void ExtractToDirectory(this ZipArchive archive, string destinationDirectoryName, bool overwrite)
        {
            if (!overwrite)
            {
                archive.ExtractToDirectory(destinationDirectoryName);
                return;
            }
            foreach (ZipArchiveEntry file in archive.Entries)
            {
                string completeFileName = Path.Combine(destinationDirectoryName, file.FullName);
                string directory = Path.GetDirectoryName(completeFileName);

                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                if (file.Name != "")
                {
                    //if (directory.Contains(@"\Profiles") && !File.Exists(completeFileName))
                    //    file.ExtractToFile(completeFileName, true);
                    //else
                        file.ExtractToFile(completeFileName, overwrite);
                }
            }
        }
    }
}
