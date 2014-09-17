namespace chocolatey.infrastructure.services
{
    public interface IXmlService
    {
        /// <summary>
        ///   Deserializes the specified XML file path.
        /// </summary>
        /// <typeparam name="XmlType">The type of the ml type.</typeparam>
        /// <param name="xmlFilePath">The XML file path.</param>
        /// <returns></returns>
        XmlType deserialize<XmlType>(string xmlFilePath);

        /// <summary>
        ///   Serializes the specified XML type.
        /// </summary>
        /// <typeparam name="XmlType">The type of the ml type.</typeparam>
        /// <param name="xmlType">Type of the XML.</param>
        /// <param name="xmlFilePath">The XML file path.</param>
        void serialize<XmlType>(XmlType xmlType, string xmlFilePath);
    }
}