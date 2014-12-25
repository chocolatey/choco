namespace chocolatey.infrastructure.adapters
{
    using System;
    using System.Diagnostics;

    public sealed class Process : IProcess
    {
        private readonly System.Diagnostics.Process _process;
        public event EventHandler<DataReceivedEventArgs> OutputDataReceived;
        public event EventHandler<DataReceivedEventArgs> ErrorDataReceived;

        public Process()
        {
            _process = new System.Diagnostics.Process();
            _process.ErrorDataReceived += (sender, args) => ErrorDataReceived.Invoke(sender, args);
            _process.OutputDataReceived += (sender, args) => OutputDataReceived.Invoke(sender, args);
        }

        public ProcessStartInfo StartInfo
        {
            get { return _process.StartInfo; }
            set { _process.StartInfo = value; }
        }

        public bool EnableRaisingEvents
        {
            get { return _process.EnableRaisingEvents; }
            set { _process.EnableRaisingEvents = value; }
        }

        public int ExitCode
        {
            get { return _process.ExitCode; }
        }

        public void Start()
        {
            _process.Start();
        }

        public void BeginErrorReadLine()
        {
            _process.BeginErrorReadLine();
        }

        public void BeginOutputReadLine()
        {
            _process.BeginOutputReadLine();
        }

        public void WaitForExit()
        {
            _process.WaitForExit();
        }

        public void Dispose()
        {
            _process.Dispose();
        }
    }
}