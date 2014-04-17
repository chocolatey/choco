namespace chocolatey
{
    using System;
    using System.Collections.Generic;
    using SimpleInjector;

    public static class SimpleInjectorExtensions
    {
        public static void RegisterAll<TService>(this Container container, IEnumerable<Func<TService>> instanceCreators) where TService : class
        {
            //Type[] singletons = instanceCreators.ToArray();

            foreach (var instanceCreator in instanceCreators.OrEmptyListIfNull())
            {
                container.RegisterSingle(typeof (TService), instanceCreator);
                //container.RegisterSingle<TService>(instanceCreator);
            }

            container.RegisterAll<TService>(typeof (TService));
        }
    }
}