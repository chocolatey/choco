// Copyright © 2017 - 2022 Chocolatey Software, Inc
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
namespace chocolatey.infrastructure.app.registration
{
    using System;

    public interface IContainerRegistrator
    {
        bool RegistrationFailed { get; }

        void RegisterValidator(Func<Type, bool> validation_func);

        void RegisterService<TService, TImplementation>(bool transient = false)
            where TImplementation : class, TService;

        void RegisterService<TService>(params Type[] types);

        void RegisterSourceRunner<TService>()
            where TService : class;

        void RegisterSourceRunner(Type serviceType);

        void RegisterInstance<TImplementation>(Func<TImplementation> instance)
            where TImplementation : class;

        void RegisterInstance<TService, TImplementation>(Func<TImplementation> instance)
            where TImplementation : class, TService;

        void RegisterInstance<TService, TImplementation>(Func<IContainerResolver, TImplementation> instance)
            where TImplementation : class, TService;

#pragma warning disable IDE1006
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        void register_validator(Func<Type, bool> validation_func);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        void register_service<TService, TImplementation>(bool transient = false)
            where TImplementation : class, TService;

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        void register_service<TService>(params Type[] types);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        void register_instance<TImplementation>(Func<TImplementation> instance)
            where TImplementation : class;

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        void register_instance<TService, TImplementation>(Func<TImplementation> instance)
            where TImplementation : class, TService;

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        void register_instance<TService, TImplementation>(Func<IContainerResolver, TImplementation> instance)
            where TImplementation : class, TService;
#pragma warning restore IDE1006
    }
}
