﻿// Copyright © 2023-Present Chocolatey Software, Inc
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

namespace chocolatey.infrastructure.rules
{
    using System;
    using System.Collections.Generic;
    using chocolatey.infrastructure.app.attributes;
    using NuGet.Packaging;

    [MultiService]
    public interface IMetadataRule
    {
        IEnumerable<RuleResult> Validate(NuspecReader reader);

        IReadOnlyList<ImmutableRule> GetAvailableRules();

#pragma warning disable IDE0022, IDE1006
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        IEnumerable<RuleResult> validate(NuspecReader reader);
#pragma warning restore IDE0022, IDE1006
    }
}
