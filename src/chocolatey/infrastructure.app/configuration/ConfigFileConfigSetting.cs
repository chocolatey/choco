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
    ///   XML config file config element
    /// </summary>
    [Serializable]
    [XmlType("add")]
    public sealed class ConfigFileConfigSetting
    {
        [XmlAttribute(AttributeName = "key")]
        public string Key { get; set; }

        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }

        [XmlAttribute(AttributeName = "description")]
        public string Description { get; set; }

        public override bool Equals(object obj)
        {
            // Check for null values and compare run-time types.
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            var item = (ConfigFileConfigSetting) obj;

            return (Key == item.Key)
                   && (Value == item.Value)
                   && (Description == item.Description);
        }

        public override int GetHashCode()
        {
            return HashCode
                .Of(Key)
                .And(Value)
                .And(Description);
        }
    }
}
