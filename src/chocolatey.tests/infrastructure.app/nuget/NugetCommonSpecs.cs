// Copyright © 2017 - 2018 Chocolatey Software, Inc
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

namespace chocolatey.tests.infrastructure.app.nuget
{
    using System;
    using System.Linq;
    using chocolatey.infrastructure.app.configuration;
    using chocolatey.infrastructure.app.nuget;
    using Moq;
    using NuGet;
    using Should;

    public class NugetCommonSpecs 
    {
        private class when_gets_remote_repository : TinySpec
        {
            private Action because;
            private readonly Mock<ILogger> nugetLogger = new Mock<ILogger>();
            private readonly Mock<IPackageDownloader> packageDownloader = new Mock<IPackageDownloader>();
            private ChocolateyConfiguration configuration;
            private IPackageRepository packageRepository;

            public override void Context()
            {
                configuration = new ChocolateyConfiguration();
                nugetLogger.ResetCalls();
                packageDownloader.ResetCalls();
            }

            public override void Because()
            {
                because = () => packageRepository = NugetCommon.GetRemoteRepository(configuration, nugetLogger.Object, packageDownloader.Object);
            }

            [Fact]
            public void should_create_repository_when_source_is_null()
            {
                Context();
                configuration.Sources = null;
            
                because();

                ((AggregateRepository)packageRepository).Repositories.Count().ShouldEqual(0);
            }
        }
    }
}
