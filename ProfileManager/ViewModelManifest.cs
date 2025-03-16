namespace ProfileManager
{
    public class ViewModelManifest(PackageManifest packageManifest)
    {
        public virtual PackageManifest Manifest { get; } = packageManifest;
        public virtual int PackageFormat { get => Manifest.PackageFormat; set => Manifest.PackageFormat = value; }
        public virtual string Title { get => Manifest.Title; set => Manifest.Title = value; }
        public virtual string VersionPackage { get => Manifest.VersionPackage; set => Manifest.VersionPackage = value; }
        public virtual string Aircraft { get => Manifest.Aircraft; set => Manifest.Aircraft = value; }
        public virtual string Author { get => Manifest.Author; set => Manifest.Author = value; }
        public virtual string URL { get => Manifest.URL; set => Manifest.URL = value; }
        public virtual string Notes { get => Manifest.Notes; set => Manifest.Notes = value; }
        public virtual string VersionPlugin { get => Manifest.VersionPlugin; set => Manifest.VersionPlugin = value; }
    }
}
