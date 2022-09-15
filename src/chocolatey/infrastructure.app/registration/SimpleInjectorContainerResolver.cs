// Copyright © 2017 - 2022 Chocolatey Software, Inc
// Copyright © 2011 - 2017 RealDimensions Software, LLC
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
//
// You may obtain a copy of the License at
//
//  http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace chocolatey.infrastructure.app.registration
{
    using System.Collections.Generic;
    using SimpleInjector;

    internal class SimpleInjectorContainerResolver : IContainerResolver
    {
        private readonly Container _container;

        public SimpleInjectorContainerResolver(Container container)
        {
            _container = container;
        }

        public TService resolve<TService>()
            where TService : class
        {
            return _container.GetInstance<TService>();
        }

        public IEnumerable<TService> resolve_all<TService>()
            where TService : class
        {
            return _container.GetAllInstances<TService>();
        }
    }
}
