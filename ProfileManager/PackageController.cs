using CFIT.AppLogger;
using System;
using System.IO;
using System.IO.Compression;

namespace ProfileManager
{
    public class PackageController
    {
        public virtual PackageManifest Manifest { get; protected set; }
        public virtual string PackageName { get; protected set; }
        public virtual string PathPackage { get; protected set; }
        public virtual string PathManifest => Path.Combine(PathPackage, "_Package\\package.json");
        public virtual string PathPackageFiles => Path.Combine(PathPackage, "_Package");
        public virtual string PathScripts => Path.Combine(PathPackageFiles, "Scripts");
        public virtual string PathReleases => Path.Combine(PathPackage, "_Releases");

        public virtual bool LoadPackagePath(string pathPackage)
        {
            try
            {
                PathPackage = pathPackage;

                if (Path.Exists(PathPackage) && Path.Exists(PathPackageFiles) && File.Exists(Path.Combine(PathPackageFiles, "package.json")))
                {
                    Manifest = PackageManifest.LoadManifest(File.ReadAllText(PathManifest));
                }
                else
                {
                    Directory.CreateDirectory(PathPackage);
                    Directory.CreateDirectory(PathPackageFiles);
                    Manifest = new PackageManifest();
                    Manifest.SaveManifest(PathManifest);
                }

                PackageName = new DirectoryInfo(PathPackage).Name;

                Directory.CreateDirectory(Path.Combine(PathPackageFiles, "Images"));
                Directory.CreateDirectory(Path.Combine(PathPackageFiles, "Profiles"));

                Directory.CreateDirectory(PathScripts);
                Directory.CreateDirectory(Path.Combine(PathScripts, "global"));
                Directory.CreateDirectory(Path.Combine(PathScripts, "image"));
                Directory.CreateDirectory(Path.Combine(PathPackage, "_Releases"));
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                return false;
            }
        }

        public virtual bool SaveManifest()
        {
            try
            {
                Manifest.SaveManifest(PathManifest);
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                return false;
            }
        }

        public virtual bool CheckVersionExists()
        {
            try
            {
                return Path.Exists(GetReleaseFilePath(".zip")) || Path.Exists(GetReleaseFilePath(".ppp"));
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                return false;
            }
        }

        protected virtual string GetReleaseFilePath(string extension)
        {
            return Path.Combine(PathReleases, $"{PackageName} v{Manifest.VersionPackage}{extension}");
        }

        public virtual bool CreatePackage()
        {
            try
            {
                SaveManifest();

                string fileZip = GetReleaseFilePath(".zip");
                string filePpp = GetReleaseFilePath(".ppp");
                if (File.Exists(fileZip))
                    File.Delete(fileZip);
                if (File.Exists(filePpp))
                    File.Delete(filePpp);

                ZipFile.CreateFromDirectory(PathPackageFiles, fileZip);
                ZipFile.CreateFromDirectory(PathPackageFiles, filePpp);
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                return false;
            }
        }
    }
}
