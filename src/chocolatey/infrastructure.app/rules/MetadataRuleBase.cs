// Copyright © 2023-Present Chocolatey Software, Inc
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

namespace chocolatey.infrastructure.app.rules
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml.Linq;
    using chocolatey.infrastructure.rules;
    using NuGet.Packaging;

    public abstract class MetadataRuleBase : IMetadataRule
    {
        private IDictionary<string, ImmutableRule> _cachedRules;

        public abstract IEnumerable<RuleResult> Validate(NuspecReader reader);

        public IReadOnlyList<ImmutableRule> GetAvailableRules()
        {
            if (_cachedRules is null || _cachedRules.Count == 0)
            {
                _cachedRules = GetRules().ToDictionary(r => r.Id, r => r);
            }

            return _cachedRules.Values.ToList().AsReadOnly();
        }

        protected RuleResult GetRule(string id, string summary = null)
        {
            if (_cachedRules is null || _cachedRules.Count == 0)
            {
                // Just to populate the cached dictionary
                GetAvailableRules();
            }

            if (!_cachedRules.TryGetValue(id, out ImmutableRule result))
            {
                throw new ArgumentOutOfRangeException(nameof(id), "No rule with the identifier {0} could be found!".FormatWith(id));
            }

            return RuleResult.FromImmutableRule(result, summary);
        }

        protected static bool HasElement(NuspecReader reader, string name)
        {
            var metadataNode = reader.Xml.Root.Elements().FirstOrDefault(e => StringComparer.Ordinal.Equals(e.Name.LocalName, "metadata"));

            return !(metadataNode is null || metadataNode.Elements(XName.Get(name, metadataNode.GetDefaultNamespace().NamespaceName)).FirstOrDefault() is null);
        }

        protected static string GetElementValue(NuspecReader reader, string name)
        {
            var metadataNode = reader.Xml.Root.Elements().FirstOrDefault(e => StringComparer.Ordinal.Equals(e.Name.LocalName, "metadata"));

            if (metadataNode is null)
            {
                return null;
            }

            var element = metadataNode.Elements(XName.Get(name, metadataNode.GetDefaultNamespace().NamespaceName)).FirstOrDefault();

            if (element is null)
            {
                return null;
            }

            return element.Value;
        }

        protected abstract IEnumerable<ImmutableRule> GetRules();

#pragma warning disable IDE1006
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public virtual IEnumerable<RuleResult> validate(NuspecReader reader)
            => Validate(reader);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        protected static bool has_element(NuspecReader reader, string name)
            => HasElement(reader, name);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        protected static string get_element_value(NuspecReader reader, string name)
            => GetElementValue(reader, name);
#pragma warning restore IDE1006
    }
}