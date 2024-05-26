﻿// Copyright © 2017 - 2021 Chocolatey Software, Inc
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

using System.Collections.Generic;
using System.Linq;
using chocolatey.infrastructure.app.attributes;
using chocolatey.infrastructure.app.commands;
using chocolatey.infrastructure.app.configuration;
using FluentAssertions;

namespace chocolatey.tests.infrastructure.app.commands
{
    public class ChocolateyHelpCommandSpecs
    {
        [ConcernFor("help")]
        public abstract class ChocolateyHelpCommandSpecsBase : TinySpec
        {
            protected ChocolateyHelpCommand Command;
            protected ChocolateyConfiguration Configuration = new ChocolateyConfiguration();

            public override void Context()
            {
                Command = new ChocolateyHelpCommand(null);
            }
        }

        public class When_implementing_command_for : ChocolateyHelpCommandSpecsBase
        {
            private List<string> _results;

            public override void Because()
            {
                _results = Command.GetType().GetCustomAttributes(typeof(CommandForAttribute), false).Cast<CommandForAttribute>().Select(a => a.CommandName).ToList();
            }

            [Fact]
            public void Should_implement_help()
            {
                _results.Should().Contain("help");
            }
        }
    }
}
