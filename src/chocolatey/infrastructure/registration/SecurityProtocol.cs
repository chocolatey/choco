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

namespace chocolatey.infrastructure.registration
{
    using System;
    using System.Net;
    using app.configuration;
    using logging;

    [Obsolete("This type is deprecated and will be removed in v3.")]
    public sealed class SecurityProtocol
    {
#pragma warning disable IDE0022, IDE1006
        public static void set_protocol(ChocolateyConfiguration config, bool provideWarning)
            => HttpsSecurity.Reset();
#pragma warning restore IDE0022, IDE1006
    }
}
