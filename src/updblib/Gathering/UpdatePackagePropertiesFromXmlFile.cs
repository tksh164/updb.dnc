using System.Xml;

namespace UPDB.Gathering
{
    public class UpdatePackagePropertiesFromXmlFile
    {
        public string PackageName { get; protected set; }
        public string PackageVersion { get; protected set; }
        public string PackageLanguage { get; protected set; }
        public string PackageProcessorArchitecture { get; protected set; }
        public string InnerCabFileLocation { get; protected set; }

        internal UpdatePackagePropertiesFromXmlFile(string packageXmlFilePath)
        {
            var rootXmlDoc = new XmlDocument();
            rootXmlDoc.Load(packageXmlFilePath);
            var nsManager = new XmlNamespaceManager(rootXmlDoc.NameTable);
            nsManager.AddNamespace("u", "urn:schemas-microsoft-com:unattend");
            PackageName = GetXmlAttributeValue(rootXmlDoc, nsManager, "/u:unattend/u:servicing/u:package/u:assemblyIdentity", "name");
            PackageVersion = GetXmlAttributeValue(rootXmlDoc, nsManager, "/u:unattend/u:servicing/u:package/u:assemblyIdentity", "version");
            PackageLanguage = GetXmlAttributeValue(rootXmlDoc, nsManager, "/u:unattend/u:servicing/u:package/u:assemblyIdentity", "language");
            PackageProcessorArchitecture = GetXmlAttributeValue(rootXmlDoc, nsManager, "/u:unattend/u:servicing/u:package/u:assemblyIdentity", "processorArchitecture");
            InnerCabFileLocation = GetXmlAttributeValue(rootXmlDoc, nsManager, "/u:unattend/u:servicing/u:package/u:source", "location");
        }

        private static string GetXmlAttributeValue(XmlDocument rootXmlDoc, XmlNamespaceManager nsManager, string nodeXPath, string attributeName)
        {
            var node = rootXmlDoc.SelectSingleNode(nodeXPath, nsManager);
            if (node == null) throw new UpdatePackageXmlNodeNotFoundException(nodeXPath);
            var attributeValue = node?.Attributes[attributeName]?.Value;
            if (attributeValue == null) throw new UpdatePackageXmlAttributeNotFoundException(nodeXPath, attributeName);
            return attributeValue;
        }
    }
}
