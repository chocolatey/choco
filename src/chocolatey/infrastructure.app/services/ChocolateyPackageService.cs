namespace chocolatey.infrastructure.app.services
{
    using System;
    using System.Collections.Concurrent;
    using System.Linq;
    using configuration;
    using logging;
    using results;

    public class ChocolateyPackageService : IChocolateyPackageService
    {
        private readonly INugetService _nugetService;
        private readonly IPowershellService _powershellService;

        public ChocolateyPackageService(INugetService nugetService, IPowershellService powershellService)
        {
            _nugetService = nugetService;
            _powershellService = powershellService;
        }

        public void list_noop(ChocolateyConfiguration configuration)
        {
            if (configuration.Source.is_equal_to(SpecialSourceTypes.webpi.to_string()))
            {
                //todo: webpi
            }
            else
            {
                _nugetService.list_noop(configuration);
            }
        }

        public void list_run(ChocolateyConfiguration configuration, bool logResults)
        {
            this.Log().Debug(() => "Searching for package information");

            if (configuration.Source.is_equal_to(SpecialSourceTypes.webpi.to_string()))
            {
                //todo: webpi
                //install webpi if not installed
                //run the webpi command 
                this.Log().Info("Command not yet functional, stay tuned...");
            }
            else
            {
                _nugetService.list_run(configuration, logResults: true);
            }
        }

        public void install_noop(ChocolateyConfiguration configuration)
        {
            _nugetService.install_noop(configuration,
                (pkg) =>
                {
                _powershellService.install_noop(pkg);
                });
        }

        public ConcurrentDictionary<string, PackageResult> install_run(ChocolateyConfiguration configuration)
        {
            //todo:is this a packages.config? If so run that command (that will call back into here).
            //todo:are we installing from an alternate source? If so run that command instead

            this.Log().Info(@"Installing the following packages:");
            this.Log().Info(ChocolateyLoggers.Important, @"{0}".format_with(configuration.PackageNames));
            this.Log().Info(@"
By installing you accept licenses for the packages.
");

            var packageInstalls = _nugetService.install_run(
                configuration,
                (pkg) =>
                    {
                        _powershellService.install(pkg);

                        //todo: batch/shim redirection


                        if (!pkg.Success)
                        {
                            this.Log().Error(ChocolateyLoggers.Important, "{0} install unsuccessful.".format_with(pkg.Name));
                            foreach (var message in pkg.Messages.Where(m => m.MessageType == ResultType.Error))
                            {
                                this.Log().Error(message.Message);
                            }

                            return;
                        }

                        this.Log().Info(" {0} has been installed.".format_with(pkg.Name));
                    }
                );

            //foreach (var packageInstall in packageInstalls.Where(p => p.Value.Success && !p.Value.Inconclusive).or_empty_list_if_null())
            //{

            //}

            var installFailures = packageInstalls.Count(p => !p.Value.Success);
            this.Log().Warn(ChocolateyLoggers.Important, () => @"{0}{1} installed {2}/{3} packages. {4} packages failed.{0}See the log for details.".format_with(
                Environment.NewLine,
                ApplicationParameters.Name,
                packageInstalls.Count(p => p.Value.Success && !p.Value.Inconclusive),
                packageInstalls.Count,
                installFailures));

            this.Log().Warn("Command not yet fully functional, stay tuned...");

            if (installFailures != 0 && Environment.ExitCode == 0)
            {
                Environment.ExitCode = 1;
            }

            return packageInstalls;
        }
    }
}