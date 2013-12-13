namespace chocolatey
{
    using System;
    using System.IO;
    using System.Reflection;
    using infrastructure;
    using infrastructure.licensing;
    using infrastructure.logging;
    using infrastructure.registration;
    using infrastructure.runners;

    public class Program
    {
        private static void Main(string[] args)
        {
            var output_directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), ApplicationParameters.name,"logs");
            if (!Directory.Exists(output_directory)) Directory.CreateDirectory(output_directory);

            Log4NetAppender.configure(output_directory);
            Bootstrap.initialize();
            Bootstrap.startup();

            try
            {
                "chocolatey".Log().Info(()=> "Starting {0}".FormatWith(ApplicationParameters.name));
                var current_assembly = Assembly.GetExecutingAssembly().Location;
                var assembly_dir = Path.GetDirectoryName(current_assembly);
                var licencse_file = Path.Combine(assembly_dir, "license.xml");
                LicenseValidation.Validate(licencse_file);

                var runner = new ChocolateyInstallRunner();
                runner.run(args);

#if DEBUG
                Console.WriteLine("Press enter to continue...");
                Console.ReadKey();
#endif
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                "chocolatey".Log().Error(() => "{0} had an error on {1} (with user {2}):{3}{4}".FormatWith(
                                      ApplicationParameters.name,
                                      Environment.MachineName,
                                      Environment.UserName,
                                      Environment.NewLine,
                                      ex.ToString()));

#if DEBUG
                Console.WriteLine("Press enter to continue...");
                Console.ReadKey();
#endif
                Environment.Exit(1);
            }
        }
    }
}