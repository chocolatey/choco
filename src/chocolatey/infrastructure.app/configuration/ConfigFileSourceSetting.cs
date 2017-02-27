// Copyright © 2011 - Present RealDimensions Software, LLC
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// 
// You may obtain a copy of the License at
// 
// 	http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace chocolatey.infrastructure.app.configuration
{
    using System;
    using System.Xml.Serialization;

    /// <summary>
    ///   XML config file sources element
    /// </summary>
    [Serializable]
    [XmlType("source")]
    public sealed class ConfigFileSourceSetting
    {
        [XmlAttribute(AttributeName = "id")]
        public string Id { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }

        [XmlAttribute(AttributeName = "disabled")]
        public bool Disabled { get; set; }

        [XmlAttribute(AttributeName = "bypassProxy")]
        public bool BypassProxy { get; set; }

        [XmlAttribute(AttributeName = "user")]
        public string UserName { get; set; }

        [XmlAttribute(AttributeName = "password")]
        public string Password { get; set; }   
        
        [XmlAttribute(AttributeName = "priority")]
        public int Priority { get; set; }

        [XmlAttribute(AttributeName = "certificate")]
        public string Certificate { get; set; }

        [XmlAttribute(AttributeName = "certificatePassword")]
        public string CertificatePassword { get; set; }
    }
}