// Copyright © 2017 - 2025 Chocolatey Software, Inc
// Copyright © 2011 - 2017 RealDimensions Software, LLC
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
//
// You may obtain a copy of the License at
//
// 	http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

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
