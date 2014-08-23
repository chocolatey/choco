namespace chocolatey.infrastructure.app.nuget
{
    using System.Collections.Generic;
    using NuGet;

    internal sealed class DictionaryPropertyProvider : IPropertyProvider
    {
        private readonly IDictionary<string, string> _properties;

        public DictionaryPropertyProvider(IDictionary<string, string> properties)
        {
            _properties = properties;
        }

        public dynamic GetPropertyValue(string propertyName)
        {
            string value;
            if (_properties.TryGetValue(propertyName, out value))
            {
                return value;
            }
            return null;
        }
    }
}
