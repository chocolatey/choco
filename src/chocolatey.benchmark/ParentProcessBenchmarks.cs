using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using chocolatey.benchmark.helpers;
using chocolatey.infrastructure.information;

namespace chocolatey.benchmark
{
    public class ParentProcessBenchmarks
    {
        [Benchmark, MethodImpl(MethodImplOptions.NoInlining)]
        public string GetParentProcessDocumentedPinvoke()
        {
            return PinvokeProcessHelper.GetDocumentedParent();
        }

        [Benchmark, MethodImpl(MethodImplOptions.NoInlining)]
        public string GetParentProcessFilteredDocumentedPinvoke()
        {
            return PinvokeProcessHelper.GetDocumentedParentFiltered();
        }

        [Benchmark, MethodImpl(MethodImplOptions.NoInlining)]
        public string GetParentProcessFilteredManaged()
        {
            return ManagedProcessHelper.GetParent();
        }

        [Benchmark, MethodImpl(MethodImplOptions.NoInlining)]
        public string GetParentProcessFilteredUndocumentedPinvoke()
        {
            return PinvokeProcessHelper.GetUndocumentedParentFiltered();
        }

        [Benchmark(Baseline = true), MethodImpl(MethodImplOptions.NoInlining)]
        public string GetParentProcessManaged()
        {
            return ManagedProcessHelper.GetParent();
        }

        [Benchmark, MethodImpl(MethodImplOptions.NoInlining)]
        public ProcessTree GetParentProcessTreeDocumentedPinvoke()
        {
            return PinvokeProcessHelper.GetDocumentedProcessTree();
        }

        [Benchmark, MethodImpl(MethodImplOptions.NoInlining)]
        public ProcessTree GetParentProcessTreeManaged()
        {
            return ManagedProcessHelper.GetProcessTree();
        }

        [Benchmark, MethodImpl(MethodImplOptions.NoInlining)]
        public ProcessTree GetParentProcessTreeUndocumentedPinvoke()
        {
            return PinvokeProcessHelper.GetUndocumentedProcessTree();
        }

        [Benchmark, MethodImpl(MethodImplOptions.NoInlining)]
        public string GetParentProcessUndocumentedPinvoke()
        {
            return PinvokeProcessHelper.GetUndocumentedParent();
        }
        [Benchmark, MethodImpl(MethodImplOptions.NoInlining)]
        public ProcessTree GetParentProcessTreeImplemented()
        {
            return ProcessInformation.GetProcessTree();
        }
    }
}