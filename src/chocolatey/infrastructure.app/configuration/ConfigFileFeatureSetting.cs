﻿// Copyright © 2011 - Present RealDimensions Software, LLC
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
    ///   XML config file features element
    /// </summary>
    [Serializable]
    [XmlType("feature")]
    public sealed class ConfigFileFeatureSetting
    {
        [XmlAttribute(AttributeName = "name")]
        public string Name { get; set; }

        [XmlAttribute(AttributeName = "enabled")]
        public bool Enabled { get; set; }

        [XmlAttribute(AttributeName = "setExplicitly")]
        public bool SetExplicitly { get; set; }

        [XmlAttribute(AttributeName = "description")]
        public string Description { get; set; }
    }
}