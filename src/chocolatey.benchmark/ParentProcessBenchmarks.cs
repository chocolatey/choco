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