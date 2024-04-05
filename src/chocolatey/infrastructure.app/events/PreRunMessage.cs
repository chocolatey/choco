// Copyright © 2017 - 2021 Chocolatey Software, Inc
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

using chocolatey.infrastructure.app.configuration;
using chocolatey.infrastructure.commands;
using chocolatey.infrastructure.events;

namespace chocolatey.infrastructure.app.events
{
    public class PreRunMessage : IMessage
    {
        public ChocolateyConfiguration Configuration { get; private set; }

        public PreRunMessage(ChocolateyConfiguration configuration)
        {
            Configuration = configuration;
        }
    }

    public class PreRunMessage<TCommand> : IMessage where TCommand : ICommand
    {
        public TCommand Command { get; private set; }
        public ChocolateyConfiguration Configuration { get; private set; }
        public object[] State { get; private set; }

        public PreRunMessage(TCommand command, ChocolateyConfiguration configuration, object[] state)
        {
            Command = command;
            Configuration = configuration;
            State = state;
        }
    }
}
