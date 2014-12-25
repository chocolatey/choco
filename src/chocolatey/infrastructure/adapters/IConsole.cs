namespace chocolatey.infrastructure.adapters
{
    using System.IO;

    // ReSharper disable InconsistentNaming

    public interface IConsole
    {
        string ReadLine();
        TextWriter Error { get; }
    }

    // ReSharper restore InconsistentNaming
}