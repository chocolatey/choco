namespace chocolatey.infrastructure.services
{
    public interface IXmlService
    {
        XmlType deserialize<XmlType>(string xmlFilePath);
    }
}