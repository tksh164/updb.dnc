using System;
using System.Globalization;

namespace UPDB.Gathering
{
    public class UpdatePackageMetadataFromUpdateMumFile
    {
        public string Description { get; protected set; }
        public string DisplayName { get; protected set; }
        public string Company { get; protected set; }
        public string Copyright { get; protected set; }
        public string SupportInformation { get; protected set; }
        public DateTime CreationTimeStamp { get; protected set; }
        public DateTime LastUpdateTimeStamp { get; protected set; }
        public string Name { get; protected set; }
        public string Version { get; protected set; }
        public string Language { get; protected set; }
        public string ProcessorArchitecture { get; protected set; }
        public string Identifier { get; protected set; }
        public string ApplicabilityEvaluation { get; protected set; }
        public string ReleaseType { get; protected set; }
        public string Restart { get; protected set; }
        public string SelfUpdate { get; protected set; }
        public string Permanence { get; protected set; }

        internal UpdatePackageMetadataFromUpdateMumFile(string updateMumFilePath)
        {
            var xmlDoc = new UpdatePackageXmlDocument(updateMumFilePath, new (string Prefix, string Uri)[] { ("u", "urn:schemas-microsoft-com:asm.v3") });

            Description = xmlDoc.GetXmlAttributeValue("/u:assembly", "description");
            DisplayName = xmlDoc.GetXmlAttributeValue("/u:assembly", "displayName");
            Company = xmlDoc.GetXmlAttributeValue("/u:assembly", "company");
            Copyright = xmlDoc.GetXmlAttributeValue("/u:assembly", "copyright");
            SupportInformation = xmlDoc.GetXmlAttributeValue("/u:assembly", "supportInformation");
            var creationTimeStamp = xmlDoc.GetXmlAttributeValue("/u:assembly", "creationTimeStamp");
            CreationTimeStamp = DateTime.ParseExact(creationTimeStamp, "yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture).ToUniversalTime();
            var lastUpdateTimeStamp = xmlDoc.GetXmlAttributeValue("/u:assembly", "lastUpdateTimeStamp");
            LastUpdateTimeStamp = DateTime.ParseExact(lastUpdateTimeStamp, "yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture).ToUniversalTime();

            Name = xmlDoc.GetXmlAttributeValue("/u:assembly/u:assemblyIdentity", "name");
            Version = xmlDoc.GetXmlAttributeValue("/u:assembly/u:assemblyIdentity", "version");
            Language = xmlDoc.GetXmlAttributeValue("/u:assembly/u:assemblyIdentity", "language");
            ProcessorArchitecture = xmlDoc.GetXmlAttributeValue("/u:assembly/u:assemblyIdentity", "processorArchitecture");

            Identifier = xmlDoc.GetXmlAttributeValue("/u:assembly/u:package", "identifier");
            ApplicabilityEvaluation = xmlDoc.GetXmlAttributeValue("/u:assembly/u:package", "applicabilityEvaluation");
            ReleaseType = xmlDoc.GetXmlAttributeValue("/u:assembly/u:package", "releaseType");
            Restart = xmlDoc.GetXmlAttributeValue("/u:assembly/u:package", "restart");
            try
            {
                SelfUpdate = xmlDoc.GetXmlAttributeValue("/u:assembly/u:package", "selfUpdate");
            }
            catch (UpdatePackageXmlAttributeNotFoundException)
            {
                SelfUpdate = null;
            }
            try
            {
                Permanence = xmlDoc.GetXmlAttributeValue("/u:assembly/u:package", "permanence");
            }
            catch (UpdatePackageXmlAttributeNotFoundException)
            {
                Permanence = null;
            }
        }
    }
}
