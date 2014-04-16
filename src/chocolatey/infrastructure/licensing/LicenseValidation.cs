namespace chocolatey.infrastructure.licensing
{
    using Rhino.Licensing;
    using app;
    using filesystem;

    public sealed class LicenseValidation
    {
        private const string publicKey = @"";

        public static void Validate(IFileSystem fileSystem)
        {
            string licenseFile = ApplicationParameters.LicenseFileLocation;

            if (fileSystem.file_exists(licenseFile))
            {
                new LicenseValidator(publicKey, licenseFile).AssertValidLicense();
            }
            else
            {
                //free version
            }
        }
    }
}