// Copyright © 2017 - 2026 Chocolatey Software, Inc
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

using Chocolatey.PowerShell.Helpers;
using FluentAssertions;

namespace Chocolatey.PowerShell.Tests.Helpers
{
    // ReSharper disable InconsistentNaming

    public class PSHelperSpecs
    {
        public class When_escaping_a_value_for_a_single_quoted_string : TinySpec
        {
            public override void Context()
            {
            }

            public override void Because()
            {
            }

            [InlineData(null, "")]
            [InlineData("", "")]
            [InlineData("C:\\Program Files\\tool.exe", "C:\\Program Files\\tool.exe")]
            [InlineData("Pester's tool", "Pester''s tool")]
            [InlineData("''", "''''")]
            [InlineData("a\u2018b\u2019c\u201Ad\u201Be", "a\u2018\u2018b\u2019\u2019c\u201A\u201Ad\u201B\u201Be")]
            [InlineData("\"$env:TEMP\" `n", "\"$env:TEMP\" `n")]
            public void Should_double_only_the_single_quote_characters(string value, string expected)
            {
                PSHelper.EscapeSingleQuotedStringContent(value).Should().Be(expected);
            }
        }
    }
}
