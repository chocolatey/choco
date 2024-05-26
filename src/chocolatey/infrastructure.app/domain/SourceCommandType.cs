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

namespace chocolatey.infrastructure.app.domain
{
    public enum SourceCommandType
    {
        Unknown,
        List,
        Add,
        Remove,
        Enable,
        Disable,

#pragma warning disable IDE0022, IDE1006
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        unknown = Unknown,
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        list = List,
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        add = Add,
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        remove = Remove,
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        enable = Enable,
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        disable = Disable,
#pragma warning restore IDE0022, IDE1006
    }
}
