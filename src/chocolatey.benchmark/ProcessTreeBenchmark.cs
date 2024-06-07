using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using chocolatey.benchmark.helpers;
using chocolatey.infrastructure.information;

namespace chocolatey.benchmark
{
    public class ProcessTreeBenchmark
    {
        [Benchmark, MethodImpl(MethodImplOptions.NoInlining)]
        [ArgumentsSource(nameof(GetProcessTree))]
        public string GetFirstFilteredProcessName(ProcessTree tree)
        {
            return tree.FirstFilteredProcessName;
        }

        [Benchmark, MethodImpl(MethodImplOptions.NoInlining)]
        [ArgumentsSource(nameof(GetProcessTree))]
        public string GetFirstProcessName(ProcessTree tree)
        {
            return tree.FirstProcessName;
        }

        [Benchmark, MethodImpl(MethodImplOptions.NoInlining)]
        [ArgumentsSource(nameof(GetProcessTree))]
        public string GetLastFilteredProcessName(ProcessTree tree)
        {
            return tree.LastFilteredProcessName;
        }

        [Benchmark, MethodImpl(MethodImplOptions.NoInlining)]
        [ArgumentsSource(nameof(GetProcessTree))]
        public string GetLastProcessName(ProcessTree tree)
        {
            return tree.LastProcessName;
        }

        [Benchmark(Baseline = true), MethodImpl(MethodImplOptions.NoInlining)]
        [ArgumentsSource(nameof(GetProcessTree))]
        public LinkedList<string> GetProcessesList(ProcessTree tree)
        {
            return tree.Processes;
        }

        public IEnumerable<object> GetProcessTree()
        {
            var currentProcess = Process.GetCurrentProcess();

            var tree = new ProcessTree(currentProcess.ProcessName);
            tree.Processes.AddLast("devenv");
            tree.Processes.AddLast("cmd");
            tree.Processes.AddLast("Tabby");
            tree.Processes.AddLast("explorer");
            yield return tree;

            yield return new ProcessTree(currentProcess.ProcessName);

            tree = new ProcessTree(currentProcess.ProcessName);
            tree.Processes.AddLast(currentProcess.ProcessName);
            tree.Processes.AddLast("WindowsTerminal");
            yield return tree;

            yield return PinvokeProcessHelper.GetUndocumentedProcessTree(currentProcess);
        }
    }
}