namespace chocolatey.infrastructure.adapters
{
    using System;
    using System.Diagnostics;

    // ReSharper disable InconsistentNaming

    public interface IProcess : IDisposable
    {

        event EventHandler<DataReceivedEventArgs> OutputDataReceived;
        event EventHandler<DataReceivedEventArgs> ErrorDataReceived;
        ProcessStartInfo StartInfo { get; set; }
        bool EnableRaisingEvents { get; set; }
        int ExitCode { get; }
        void Start();
        void BeginErrorReadLine();
        void BeginOutputReadLine();
        void WaitForExit();
    }

    // ReSharper restore InconsistentNaming
}