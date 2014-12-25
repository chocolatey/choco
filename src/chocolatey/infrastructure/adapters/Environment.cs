namespace chocolatey.infrastructure.adapters
{
    public sealed class Environment : IEnvironment
    {
        public System.OperatingSystem OSVersion
        {
            get { return System.Environment.OSVersion; }
        }
    }
}