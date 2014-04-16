namespace chocolatey.infrastructure.app.services
{
    using System;
    using System.IO;
    using System.Xml;
    using System.Xml.Serialization;
    using filesystem;
    using infrastructure.services;

    public sealed class XmlService : IXmlService
    {
        private readonly IFileSystem _fileSystem;

        public XmlService(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        public XmlType deserialize<XmlType>(string xmlFilePath)
        {
            try
            {
                var xmlSerializer = new XmlSerializer(typeof (XmlType));
                var xmlReader = XmlReader.Create(new StringReader(_fileSystem.read_file(xmlFilePath)));
                if (!xmlSerializer.CanDeserialize(xmlReader))
                {
                    this.Log().Warn("Cannot deserialize response of type {0}", typeof (XmlType));
                    return default(XmlType);
                }

                return (XmlType) xmlSerializer.Deserialize(xmlReader);
            }
            catch (Exception ex)
            {
                this.Log().Error("Error deserializing response of type {0}", typeof (XmlType));
                throw;
            }
        }
    }
}