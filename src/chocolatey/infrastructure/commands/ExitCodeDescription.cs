// Copyright © 2023 Chocolatey Software, Inc
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

namespace chocolatey.infrastructure.commands
{
    using System;

    public sealed class ExitCodeDescription
    {
        public ExitCodeDescription(string description, params int[] exitCodes)
        {
            // We use intern to save a bit of bytes so the same memory location
            // is reused when possible.
            Description = string.Intern(description);
            ExitCodes = exitCodes;
        }

        public string Description { get; }

        public int[] ExitCodes { get; }
    }
}