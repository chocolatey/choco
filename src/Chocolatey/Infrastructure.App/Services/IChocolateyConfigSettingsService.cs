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

namespace Chocolatey.Infrastructure.App.Services
{
    using System;
    using System.Collections.Generic;
    using Configuration;

    public interface IChocolateyConfigSettingsService
    {
        void DryRun(ChocolateyConfiguration configuration);
        IEnumerable<ChocolateySource> ListSources(ChocolateyConfiguration configuration);
        void AddSource(ChocolateyConfiguration configuration);
        void RemoveSource(ChocolateyConfiguration configuration);
        void DisableSource(ChocolateyConfiguration configuration);
        void EnableSource(ChocolateyConfiguration configuration);
        void ListFeatures(ChocolateyConfiguration configuration);
        void DisableFeature(ChocolateyConfiguration configuration);
        void EnableFeature(ChocolateyConfiguration configuration);
        string GetApiKey(ChocolateyConfiguration configuration, Action<ConfigFileApiKeySetting> keyAction);
        void SetApiKey(ChocolateyConfiguration configuration);
        void RemoveApiKey(ChocolateyConfiguration configuration);
        void ListConfig(ChocolateyConfiguration configuration);
        void GetConfig(ChocolateyConfiguration configuration);
        void SetConfig(ChocolateyConfiguration configuration);
        void UnsetConfig(ChocolateyConfiguration configuration);
    }
}
