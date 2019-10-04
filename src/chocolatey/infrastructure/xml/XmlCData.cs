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

namespace chocolatey.infrastructure.xml
{
    using System.Xml;
    using System.Xml.Schema;
    using System.Xml.Serialization;

    /// <summary>
    ///   Xml CData autoconversion
    /// </summary>
    /// <remarks>
    ///   Based on http://stackoverflow.com/a/19832309/18475
    /// </remarks>
    public class XmlCData : IXmlSerializable
    {
        private string _value;

        /// <summary>
        ///   Allow direct assignment from string:
        ///   CData cdata = "abc";
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static implicit operator XmlCData(string value)
        {
            return new XmlCData(value);
        }

        /// <summary>
        ///   Allow direct assigment to string
        ///   string str = cdata;
        /// </summary>
        /// <param name="cdata"></param>
        /// <returns></returns>
        public static implicit operator string(XmlCData cdata)
        {
            if (cdata != null) return cdata._value.to_string();

            return string.Empty;
        }

        public XmlCData() : this(string.Empty)
        {
        }

        public XmlCData(string value)
        {
            _value = value;
        }

        public override string ToString()
        {
            return _value.to_string();
        }

        public XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
            _value = reader.ReadElementString();
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteCData(_value);
        }
    }
}
