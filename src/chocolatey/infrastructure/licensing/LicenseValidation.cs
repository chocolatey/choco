namespace chocolatey.infrastructure.licensing
{
    using Rhino.Licensing;
    using app;
    using filesystem;

    public sealed class LicenseValidation
    {
        private const string PUBLIC_KEY = @"";

        public static void validate(IFileSystem fileSystem)
        {
            string licenseFile = ApplicationParameters.LicenseFileLocation;

            if (fileSystem.file_exists(licenseFile))
            {
                new LicenseValidator(PUBLIC_KEY, licenseFile).AssertValidLicense();
            }
            else
            {
                //free version
            }
        }
    }
}