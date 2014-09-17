namespace chocolatey.infrastructure.app.domain
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml.Serialization;

    /// <summary>
    ///   The installer registry as a snapshot
    /// </summary>
    [Serializable]
    [XmlType("registrySnapshot")]
    public class Registry
    {
        /// <summary>
        ///   Initializes a new instance of the <see cref="Registry" /> class.
        /// </summary>
        public Registry()
            : this(string.Empty, new HashSet<RegistryApplicationKey>())
        {
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="Registry" /> class.
        /// </summary>
        /// <param name="user">The user</param>
        /// <param name="keys">The keys.</param>
        public Registry(string user, IEnumerable<RegistryApplicationKey> keys)
        {
            User = user;
            RegistryKeys = keys.ToList();
        }

        [XmlElement(ElementName = "user")]
        public string User { get; set; }

        [XmlArray("keys")]
        public List<RegistryApplicationKey> RegistryKeys { get; private set; }
    }
}