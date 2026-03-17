// Copyright © 2017 - 2025 Chocolatey Software, Inc
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

using SimpleInjector;
using SimpleInjector.Advanced;
using System;
using System.Linq;
using System.Reflection;

namespace chocolatey.infrastructure.registration
{
    /// <summary>
    /// </summary>
    /// <remarks>
    ///   Adapted from https://simpleinjector.codeplex.com/wikipage?title=T4MVC%20Integration
    /// </remarks>
    public sealed class SimpleInjectorContainerResolutionBehavior : IConstructorResolutionBehavior
    {
        private readonly IConstructorResolutionBehavior _originalBehavior;

        public SimpleInjectorContainerResolutionBehavior(IConstructorResolutionBehavior originalBehavior)
        {
            _originalBehavior = originalBehavior ?? throw new ArgumentNullException(nameof(originalBehavior));
        }

        public ConstructorInfo TryGetConstructor(Type implementationType, out string errorMessage)
        {
            if (implementationType == null)
            {
                throw new ArgumentNullException(nameof(implementationType));
            }

            // Prefer the public constructor with the most parameters
            var longest = implementationType
                .GetConstructors()
                .OrderByDescending(c => c.GetParameters().Length)
                .FirstOrDefault();

            if (longest != null)
            {
                errorMessage = null;
                return longest;
            }

            // Fall back to the container's default behavior (propagates its error message)
            return _originalBehavior.TryGetConstructor(implementationType, out errorMessage);
        }
    }
}
