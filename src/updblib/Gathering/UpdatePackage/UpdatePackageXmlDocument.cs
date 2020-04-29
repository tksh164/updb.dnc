using System.Xml;

namespace UPDB.Gathering
{
    internal class UpdatePackageXmlDocument
    {
        private XmlDocument XmlDoc { get; set; }
        private XmlNamespaceManager NsManager { get; set; }

        public UpdatePackageXmlDocument(string xmlFilePath, (string Pefix, string Uri)[] namespaces)
        {
            XmlDoc = new XmlDocument();
            XmlDoc.Load(xmlFilePath);
            NsManager = new XmlNamespaceManager(XmlDoc.NameTable);
            foreach ((var prefix, var uri) in namespaces)
            {
                NsManager.AddNamespace(prefix, uri);
            }
        }

        public string GetXmlAttributeValue(string nodeXPath, string attributeName)
        {
            var node = XmlDoc.SelectSingleNode(nodeXPath, NsManager);
            if (node == null) throw new UpdatePackageXmlNodeNotFoundException(nodeXPath);
            var attributeValue = node?.Attributes[attributeName]?.Value;
            if (attributeValue == null) throw new UpdatePackageXmlAttributeNotFoundException(nodeXPath, attributeName);
            return attributeValue;
        }
    }
}
