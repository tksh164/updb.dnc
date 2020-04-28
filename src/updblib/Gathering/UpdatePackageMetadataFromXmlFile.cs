namespace UPDB.Gathering
{
    public class UpdatePackageMetadataFromXmlFile
    {
        public string PackageName { get; protected set; }
        public string PackageVersion { get; protected set; }
        public string PackageLanguage { get; protected set; }
        public string PackageProcessorArchitecture { get; protected set; }
        public string InnerCabFileLocation { get; protected set; }

        internal UpdatePackageMetadataFromXmlFile(string packageXmlFilePath)
        {
            var xmlDoc = new UpdatePackageXmlDocument(packageXmlFilePath, new (string Prefix, string Uri)[] { ("u", "urn:schemas-microsoft-com:unattend") });
            PackageName = xmlDoc.GetXmlAttributeValue("/u:unattend/u:servicing/u:package/u:assemblyIdentity", "name");
            PackageVersion = xmlDoc.GetXmlAttributeValue("/u:unattend/u:servicing/u:package/u:assemblyIdentity", "version");
            PackageLanguage = xmlDoc.GetXmlAttributeValue("/u:unattend/u:servicing/u:package/u:assemblyIdentity", "language");
            PackageProcessorArchitecture = xmlDoc.GetXmlAttributeValue("/u:unattend/u:servicing/u:package/u:assemblyIdentity", "processorArchitecture");
            InnerCabFileLocation = xmlDoc.GetXmlAttributeValue("/u:unattend/u:servicing/u:package/u:source", "location");
        }
    }
}
