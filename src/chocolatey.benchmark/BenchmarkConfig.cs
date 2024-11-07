using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;

namespace chocolatey.benchmark
{
    internal static class BenchmarkConfig
    {
        public static IConfig Get()
        {
            var config = ManualConfig.CreateEmpty();

            foreach (var platform in new[] { Platform.X64/*, Platform.X86*/ })
            {
                config = config.AddJob(Job.Default.WithRuntime(ClrRuntime.Net48).WithPlatform(platform).WithJit(Jit.LegacyJit));
            }

            return config
                .AddDiagnoser(MemoryDiagnoser.Default)
                .AddColumnProvider(DefaultColumnProviders.Instance)
                .AddLogger(ConsoleLogger.Default)
                .AddExporter(CsvExporter.Default)
                .AddExporter(HtmlExporter.Default)
                .AddExporter(MarkdownExporter.Default)
                .AddExporter(AsciiDocExporter.Default)
                .AddAnalyser(GetAnalysers().ToArray());
        }

        private static IEnumerable<IAnalyser> GetAnalysers()
        {
            yield return EnvironmentAnalyser.Default;
            yield return OutliersAnalyser.Default;
            yield return MinIterationTimeAnalyser.Default;
            yield return MultimodalDistributionAnalyzer.Default;
            yield return RuntimeErrorAnalyser.Default;
            yield return ZeroMeasurementAnalyser.Default;
            yield return BaselineCustomAnalyzer.Default;
        }
    }
}
