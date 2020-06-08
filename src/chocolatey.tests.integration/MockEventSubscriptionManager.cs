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

namespace chocolatey.tests.integration
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Reactive.Subjects;
    using chocolatey.infrastructure.events;
    using chocolatey.infrastructure.services;
    using Moq;

    public class MockEventSubscriptionManager : Mock<IEventSubscriptionManagerService>, IEventSubscriptionManagerService
    {
        private readonly Lazy<ConcurrentDictionary<Type, IList<object>>> _messages = new Lazy<ConcurrentDictionary<Type, IList<object>>>();

        public ConcurrentDictionary<Type, IList<object>> Messages
        {
            get { return _messages.Value; }
        }

        public void publish<Event>(Event eventMessage) where Event : class, IMessage
        {
            var list = _messages.Value.GetOrAdd(typeof(Event), new List<object>());
            list.Add(eventMessage);
            Object.publish(eventMessage);
        }

        public IDisposable subscribe<Event>(Action<Event> handleEvent, Action<Exception> handleError, Func<Event, bool> filter) where Event : class, IMessage
        {
            return new Subject<Event>();
        }
    }
}
