// Copyright © 2017 - 2018 Chocolatey Software, Inc
// Copyright © 2011 - 2017 RealDimensions Software, LLC
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

        [XmlAttribute(AttributeName = "selfService")]
        public bool AllowSelfService { get; set; }

        [XmlAttribute(AttributeName = "adminOnly")]
        public bool VisibleToAdminsOnly { get; set; }

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

        public override bool Equals(object obj)
        {
            // Check for null values and compare run-time types.
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            var item = (ConfigFileSourceSetting) obj;

            return (Id == item.Id)
                && (Value == item.Value)
                && (Disabled == item.Disabled)
                && (BypassProxy == item.BypassProxy)
                && (AllowSelfService == item.AllowSelfService)
                && (VisibleToAdminsOnly == item.VisibleToAdminsOnly)
                && (UserName == item.UserName)
                && (Password == item.Password)
                && (Priority == item.Priority)
                && (Certificate == item.Certificate)
                && (CertificatePassword == item.CertificatePassword);
        }

        public override int GetHashCode()
        {
            return HashCode
                .Of(Id)
                .And(Value)
                .And(Disabled)
                .And(BypassProxy)
                .And(AllowSelfService)
                .And(VisibleToAdminsOnly)
                .And(UserName)
                .And(Password)
                .And(Priority)
                .And(Certificate)
                .And(CertificatePassword);
        }
    }
}