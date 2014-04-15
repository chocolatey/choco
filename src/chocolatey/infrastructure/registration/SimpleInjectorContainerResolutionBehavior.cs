namespace chocolatey.infrastructure.registration
{
    using System;
    using System.Linq;
    using System.Reflection;
    using SimpleInjector.Advanced;

    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// Adapted from https://simpleinjector.codeplex.com/wikipage?title=T4MVC%20Integration
    /// </remarks>
    public sealed class SimpleInjectorContainerResolutionBehavior : IConstructorResolutionBehavior
    {
        private readonly IConstructorResolutionBehavior _originalBehavior;

        /// <summary>
        /// Initializes a new instance of the <see cref="SimpleInjectorContainerResolutionBehavior"/> class.
        /// </summary>
        /// <param name="originalBehavior">The original behavior.</param>
        public SimpleInjectorContainerResolutionBehavior(IConstructorResolutionBehavior originalBehavior)
        {
            _originalBehavior = originalBehavior;
        }

        /// <summary>
        /// Gets the given <paramref name="implementationType"/>'s constructor that can be used by the
        /// container to create that instance.
        /// </summary>
        /// <param name="serviceType">Type of the abstraction that is requested.</param>
        /// <param name="implementationType">Type of the implementation to find a suitable constructor for.</param>
        /// <returns>
        /// The <see cref="T:System.Reflection.ConstructorInfo"/>.
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