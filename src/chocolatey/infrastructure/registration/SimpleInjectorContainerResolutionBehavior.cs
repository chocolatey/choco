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

namespace chocolatey.infrastructure.registration
{
    using System;
    using System.Linq;
    using System.Reflection;
    using SimpleInjector.Advanced;

    /// <summary>
    /// </summary>
    /// <remarks>
    ///   Adapted from https://simpleinjector.codeplex.com/wikipage?title=T4MVC%20Integration
    /// </remarks>
    public sealed class SimpleInjectorContainerResolutionBehavior : IConstructorResolutionBehavior
    {
        private readonly IConstructorResolutionBehavior _originalBehavior;

        /// <summary>
        ///   Initializes a new instance of the <see cref="SimpleInjectorContainerResolutionBehavior" /> class.
        /// </summary>
        /// <param name="originalBehavior">The original behavior.</param>
        public SimpleInjectorContainerResolutionBehavior(IConstructorResolutionBehavior originalBehavior)
        {
            _originalBehavior = originalBehavior;
        }

        /// <summary>
        ///   Gets the given <paramref name="implementationType" />'s constructor that can be used by the
        ///   container to create that instance.
        /// </summary>
        /// <param name="serviceType">Type of the abstraction that is requested.</param>
        /// <param name="implementationType">Type of the implementation to find a suitable constructor for.</param>
        /// <returns>
        ///   The <see cref="T:System.Reflection.ConstructorInfo" />.
        /// </returns>
        /// <exception cref="T:SimpleInjector.ActivationException">Thrown when no suitable constructor could be found.</exception>
        public ConstructorInfo GetConstructor(Type serviceType, Type implementationType)
        {
            if (serviceType.IsAssignableFrom(implementationType))
            {
                var longestConstructor = (from constructor in implementationType.GetConstructors()
                                          orderby constructor.GetParameters().Count() descending
                                          select constructor).Take(1);

                if (longestConstructor.Any())
                {
                    return longestConstructor.First();
                }
            }

            // fall back to the container's default behavior.
            return _originalBehavior.GetConstructor(serviceType, implementationType);
        }
    }
}