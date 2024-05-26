﻿// Copyright © 2017 - 2021 Chocolatey Software, Inc
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

using System;

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
        XmlType Deserialize<XmlType>(string xmlFilePath);

        /// <summary>
        ///   Deserializes the specified XML file path.
        /// </summary>
        /// <typeparam name="XmlType">The type of the ml type.</typeparam>
        /// <param name="xmlFilePath">The XML file path.</param>
        /// <param name="retryCount">The number of times to attempt deserialization on event of a failure.</param>
        /// <returns></returns>
        XmlType Deserialize<XmlType>(string xmlFilePath, int retryCount);

        /// <summary>
        ///   Serializes the specified XML type.
        /// </summary>
        /// <typeparam name="XmlType">The type of the ml type.</typeparam>
        /// <param name="xmlType">Type of the XML.</param>
        /// <param name="xmlFilePath">The XML file path.</param>
        void Serialize<XmlType>(XmlType xmlType, string xmlFilePath);

        /// <summary>
        ///   Serializes the specified XML type.
        /// </summary>
        /// <typeparam name="XmlType">The type of the ml type.</typeparam>
        /// <param name="xmlType">Type of the XML.</param>
        /// <param name="xmlFilePath">The XML file path.</param>
        /// <param name="isSilent">Log messages?</param>
        void Serialize<XmlType>(XmlType xmlType, string xmlFilePath, bool isSilent);

#pragma warning disable IDE0022, IDE1006
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        XmlType deserialize<XmlType>(string xmlFilePath);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        XmlType deserialize<XmlType>(string xmlFilePath, int retryCount);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        void serialize<XmlType>(XmlType xmlType, string xmlFilePath);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        void serialize<XmlType>(XmlType xmlType, string xmlFilePath, bool isSilent);
#pragma warning restore IDE0022, IDE1006
    }
}
