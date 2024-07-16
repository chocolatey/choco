using System.Linq;
using BenchmarkDotNet.Running;

namespace chocolatey.benchmark
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var switcher = BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly);

            if (args.Length > 0)
            {
                switcher.Run(args, BenchmarkConfig.Get()).ToArray();
            }
            else
            {
                switcher.RunAll(BenchmarkConfig.Get()).ToArray();
            }
        }
    }
}
