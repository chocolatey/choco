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

namespace chocolatey.infrastructure.app.services
{
    using System;
    using System.Collections.Generic;
    using configuration;

    public interface IChocolateyConfigSettingsService
    {
        void noop(ChocolateyConfiguration configuration);
        IEnumerable<ChocolateySource> source_list(ChocolateyConfiguration configuration);
        void source_add(ChocolateyConfiguration configuration);
        void source_remove(ChocolateyConfiguration configuration);
        void source_disable(ChocolateyConfiguration configuration);
        void source_enable(ChocolateyConfiguration configuration);
        void feature_list(ChocolateyConfiguration configuration);
        void feature_disable(ChocolateyConfiguration configuration);
        void feature_enable(ChocolateyConfiguration configuration);
        string get_api_key(ChocolateyConfiguration configuration, Action<ConfigFileApiKeySetting> keyAction);
        void set_api_key(ChocolateyConfiguration configuration);
        void remove_api_key(ChocolateyConfiguration configuration);
        void config_list(ChocolateyConfiguration configuration);
        void config_get(ChocolateyConfiguration configuration);
        void config_set(ChocolateyConfiguration configuration);
        void config_unset(ChocolateyConfiguration configuration);
    }
}
