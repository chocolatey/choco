namespace chocolatey.infrastructure.adapters
{
    using System.IO;

    public sealed class Console : IConsole
    {
        public string ReadLine()
        {
            return System.Console.ReadLine();
        }

        public TextWriter Error
        {
            get { return System.Console.Error; }
        }
    }
}