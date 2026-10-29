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

using System.Globalization;
using System.Threading;
using FluentAssertions;

namespace Chocolatey.PowerShell.Tests
{
    // ReSharper disable InconsistentNaming

    public class FileSizeExtensionsSpecs
    {
        public class When_formatting_a_file_size : TinySpec
        {
            private CultureInfo _originalCulture;

            public override void Context()
            {
            }

            public override void Because()
            {
            }

            public override void BeforeEachSpec()
            {
                // The code is sensitive to culture number format changes, so this
                // ensures we have a sensible common test case.
                _originalCulture = Thread.CurrentThread.CurrentCulture;
                Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            }

            public override void AfterEachSpec()
            {
                Thread.CurrentThread.CurrentCulture = _originalCulture;
            }

            [InlineData(0L, "0 B")]
            [InlineData(1023L, "1023 B")]
            [InlineData(1024L, "1 KB")]
            [InlineData(1536L, "1.5 KB")]
            [InlineData(1500000L, "1.43 MB")]
            [InlineData(1073741824L, "1 GB")]
            [InlineData(-1L, "-1 B")]
            public void Should_use_the_largest_unit_and_keep_up_to_two_decimal_places(long size, string expected)
            {
                size.AsFileSizeString().Should().Be(expected);
            }
        }
    }

    // ReSharper restore InconsistentNaming
}
