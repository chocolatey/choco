// Copyright © 2017 - 2025 Chocolatey Software, Inc
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
using System.Collections.Generic;
using System.Reflection;
using FluentAssertions;

namespace chocolatey.tests.integration
{
    /// <summary>
    ///   Guards the shape of the configuration the scenarios are built from.
    /// </summary>
    /// <remarks>
    ///   This is not testing the product; it is testing the harness, and it exists because
    ///   getting this wrong is silent and the resulting failure points nowhere near the cause.
    ///
    ///   Shim generation calls ExternalCommandArgsBuilder, which reflects over the entire
    ///   configuration object and, for every property whose type is NOT a built-in system
    ///   type, recurses using that property's VALUE as the next target. A null nested
    ///   property therefore blows up with
    ///   "System.Reflection.TargetException: Non-static method requires a target."
    ///   The install is then quarantined to lib-bad, and the reported error names neither the
    ///   property nor the builder.
    ///
    ///   In a real run the configuration is populated by the ConfigurationBuilder, so this
    ///   never happens; the harness constructs the configuration directly, so anything a
    ///   constructor does not initialise stays null. Any new nested configuration property
    ///   has to be given a value in Scenario.BaselineConfiguration - this test is what says so.
    ///
    ///   This bit the Chocolatey Licensed Extension harness, where two nested properties were
    ///   left null and every install of a package containing an .exe failed. The same trap is
    ///   waiting here the moment a nested configuration property is added without a default.
    /// </remarks>
    public class ScenarioConfigurationSpecs
    {
        public class When_building_the_baseline_configuration : TinySpec
        {
            private IList<string> _nullProperties;

            public override void Context()
            {
            }

            public override void Because()
            {
                _nullProperties = new List<string>();
                FindNullComplexProperties(Scenario.Install(), string.Empty, _nullProperties, 0);
            }

            [Fact]
            public void Should_have_no_null_nested_configuration_properties()
            {
                _nullProperties.Should().BeEmpty(
                    "ExternalCommandArgsBuilder recurses into every non-builtin property and would throw on these: {0}",
                    string.Join(", ", _nullProperties));
            }

            // Mirrors ExternalCommandArgsBuilder.FillArgsDictionary: anything that is not a
            // built-in system type is recursed into, so anything that is not a built-in
            // system type must not be null.
            private static void FindNullComplexProperties(object instance, string prefix, IList<string> nullProperties, int depth)
            {
                // The builder walks the whole graph; a small depth cap keeps this cheap and
                // guards against a cycle in the configuration graph.
                if (instance == null || depth > 4)
                {
                    return;
                }

                foreach (var property in instance.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (property.PropertyType.IsBuiltinType() || !property.CanRead || property.GetIndexParameters().Length > 0)
                    {
                        continue;
                    }

                    var path = string.IsNullOrEmpty(prefix) ? property.Name : "{0}.{1}".FormatWith(prefix, property.Name);
                    object value;

                    try
                    {
                        value = property.GetValue(instance, null);
                    }
                    catch (Exception)
                    {
                        // A getter that throws is not what this guard is about.
                        continue;
                    }

                    if (value == null)
                    {
                        nullProperties.Add("{0} ({1})".FormatWith(path, property.PropertyType.Name));
                        continue;
                    }

                    FindNullComplexProperties(value, path, nullProperties, depth + 1);
                }
            }
        }
    }
}
