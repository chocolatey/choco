namespace chocolatey.infrastructure.app.nuget
{
    using NuGet;

    // ReSharper disable InconsistentNaming

    public sealed class ChocolateyNugetLogger : ILogger
    {
        public FileConflictResolution ResolveFileConflict(string message)
        {
            return FileConflictResolution.OverwriteAll;
            //this.Log().Info(ChocolateyLoggers.Important,message);
            //this.Log().Info(ChocolateyLoggers.Important,"Skipping the above...");
        }

        public void Log(MessageLevel level, string message, params object[] args)
        {
            switch (level)
            {
                case MessageLevel.Debug:
                    this.Log().Debug(message, args);
                    break;
                case MessageLevel.Info:
                    this.Log().Info(message, args);
                    break;
                case MessageLevel.Warning:
                    this.Log().Warn(message, args);
                    break;
                case MessageLevel.Error:
                    this.Log().Error(message, args);
                    break;
            }
        }
    }

    // ReSharper restore InconsistentNaming
}