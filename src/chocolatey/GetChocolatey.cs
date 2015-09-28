// Copyright © 2011 - Present RealDimensions Software, LLC
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

namespace chocolatey
{
    using System;
    using System.Collections.Generic;
    using SimpleInjector;
    using infrastructure.adapters;
    using infrastructure.app;
    using infrastructure.app.builders;
    using infrastructure.app.configuration;
    using infrastructure.app.runners;
    using infrastructure.configuration;
    using infrastructure.extractors;
    using infrastructure.filesystem;
    using infrastructure.logging;
    using infrastructure.registration;
    using infrastructure.services;
    using resources;

    // ReSharper disable InconsistentNaming

    /// <summary>
    /// Entry point for API
    /// </summary>
    public static class Lets
    {
        public static GetChocolatey GetChocolatey()
        {
            return new GetChocolatey();
        }
    }

    /// <summary>
    /// The place where all the magic happens.
    /// </summary>
    public class GetChocolatey
    {
        private readonly Container _container;
        private Action<ChocolateyConfiguration> _propConfig;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetChocolatey"/> class.
        /// </summary>
        public GetChocolatey()
        {
            Log4NetAppenderConfiguration.configure();
            Bootstrap.initialize();
            _container = SimpleInjectorContainer.Container;
        }

        /// <summary>
        ///   This is an optional helper to give you the correct settings for a logger. You can still set this in the set by calling propConfig.Logger without having to call this method.
        /// </summary>
        /// <param name="logger">This is the logger you want Chocolatey to also use.</param>
        /// <returns>This <see cref="GetChocolatey"/> instance</returns>
        public GetChocolatey SetCustomLogging(ILog logger)
        {
            Log.InitializeWith(logger);
            return this;
        }

        /// <summary>
        ///   Set your options for running chocolatey here. It looks like Set(c => {c.CommandName = "install"; c.PackageNames = "bob";}).Run();
        /// </summary>
        /// <param name="propConfig">The configuration to set</param>
        /// <returns>This <see cref="GetChocolatey"/> instance</returns>
        public GetChocolatey Set(Action<ChocolateyConfiguration> propConfig)
        {
            _propConfig = propConfig;
            return this;
        }

        /// <summary>
        /// Registers a container component. Does not require a dependency on Simple Injector.
        /// Will override existing component if registered.
        /// </summary>
        /// <param name="service">The service.</param>
        /// <param name="implementation">The implementation.</param>
        /// <returns>This <see cref="GetChocolatey"/> instance</returns>
        public GetChocolatey RegisterContainerComponent(Type service, Type implementation)
        {
            _container.Register(service,implementation,Lifestyle.Singleton);

            return this;
        }

        /// <summary>
        /// Registers a container component. Does not require a dependency on Simple Injector. 
        /// Will override existing component if registered.
        /// </summary>
        /// <typeparam name="Service">The type of the service.</typeparam>
        /// <typeparam name="Implementation">The type of the Implementation.</typeparam>
        /// <returns>This <see cref="GetChocolatey"/> instance</returns>
        public GetChocolatey RegisterContainerComponent<Service,Implementation>() 
            where Service : class 
            where Implementation : class, Service
        {
            return RegisterContainerComponent<Service, Implementation>(Lifestyle.Singleton);
        }

        /// <summary>
        /// Registers a container component. 
        /// Will override existing component if registered.
        /// NOTE: This requires you take a dependency on SimpleInjector.
        /// </summary>
        /// <typeparam name="Service">The type of the service.</typeparam>
        /// <typeparam name="Implementation">The type of the Implementation.</typeparam>
        /// <param name="lifestyle">The lifestyle.</param>
        /// <returns>This <see cref="GetChocolatey"/> instance</returns>
        public GetChocolatey RegisterContainerComponent<Service,Implementation>(Lifestyle lifestyle) 
            where Service : class 
            where Implementation : class, Service
        {
            _container.Register<Service,Implementation>(lifestyle);

            return this;
        }

        /// <summary>
        /// Registers a container component. Does not require a dependency on Simple Injector. 
        /// Will override existing component if registered.
        /// </summary>
        /// <typeparam name="Service">The type of the ervice.</typeparam>
        /// <param name="implementationCreator">The implementation creator.</param>
        /// <returns>This <see cref="GetChocolatey"/> instance</returns>
        public GetChocolatey RegisterContainerComponent<Service>(Func<Service> implementationCreator)
             where Service : class
        {
            _container.Register(implementationCreator,Lifestyle.Singleton);

            return this;
        }

        /// <summary>
        /// Register container components when you need to do multiple setups and want to work with the container directly. 
        /// Will override existing components if registered.
        /// NOTE: This requires you take a dependency on SimpleInjector.
        /// </summary>
        /// <param name="containerSetup">The container setup.</param>
        /// <returns>This <see cref="GetChocolatey"/> instance</returns>
        public GetChocolatey RegisterContainerComponents(Action<Container> containerSetup)
        {
            if (containerSetup != null)
            {
                containerSetup.Invoke(_container);
            }

            return this;
        }

        /// <summary>
        ///   Call this method to run chocolatey after you have set the options.
        /// </summary>
        public void Run()
        {
            extract_resources();
            var configuration = create_configuration(new List<string>());
            var runner = new GenericRunner();
            runner.run(configuration, _container, isConsole: false, parseArgs: null);
        }

        /// <summary>
        ///   Call this method to run chocolatey after you have set the options.
        /// <param name="args">Commandline arguments to add to configuration.</param>
        /// </summary>
        public void RunConsole(string[] args)
        {
            extract_resources();
            var configuration = create_configuration(new List<string>(args));
            var runner = new ConsoleApplication();
            runner.run(args, configuration, _container);
        }

        private ChocolateyConfiguration create_configuration(IList<string> args)
        {
            var configuration = new ChocolateyConfiguration();
            ConfigurationBuilder.set_up_configuration(args, configuration, _container.GetInstance<IFileSystem>(), _container.GetInstance<IXmlService>(), null);
            Config.initialize_with(configuration);

            configuration.PromptForConfirmation = false;
            configuration.AcceptLicense = true;
            if (_propConfig != null)
            {
                _propConfig.Invoke(configuration);
            }
            return configuration;
        }

        private void extract_resources()
        {
            //refactor - thank goodness this is temporary, cuz manifest resource streams are dumb
            IList<string> folders = new List<string>
            {
                "helpers",
                "functions",
                "redirects",
                "tools"
            };

            AssemblyFileExtractor.extract_all_resources_to_relative_directory(_container.GetInstance<IFileSystem>(), Assembly.GetAssembly(typeof(ChocolateyResourcesAssembly)), ApplicationParameters.InstallLocation, folders, ApplicationParameters.ChocolateyFileResources);
        }

        public IEnumerable<T> List<T>()
        {
            extract_resources();
            var configuration = create_configuration(new List<string>());
            configuration.RegularOutput = true;
            var runner = new GenericRunner();
            return runner.list<T>(configuration, _container, isConsole: false, parseArgs: null);
        }

        public int Count()
        {
            extract_resources();
            var configuration = create_configuration(new List<string>());
            configuration.RegularOutput = true;
            var runner = new GenericRunner();
            return runner.count(configuration, _container, isConsole: false, parseArgs: null);
        }
    }

    // ReSharper restore InconsistentNaming
}
