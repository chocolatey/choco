// Copyright © 2011 - Present RealDimensions Software, LLC
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

namespace chocolatey.infrastructure.services
{
    using System;
    using System.Reactive.Linq;
    using System.Reactive.Subjects;
    using events;
    using guards;

    /// <summary>
    ///   Implementation of IEventSubscriptionManagerService
    /// </summary>
    public class EventSubscriptionManagerService : IEventSubscriptionManagerService
    {
        //http://joseoncode.com/2010/04/29/event-aggregator-with-reactive-extensions/
        //https://github.com/shiftkey/Reactive.EventAggregator

        //private readonly ConcurrentDictionary<Type, object> _subscriptions = new ConcurrentDictionary<Type, object>();
        private readonly ISubject<object> _subject = new Subject<object>();

        public void publish<Event>(Event eventMessage) where Event : class, IEvent
        {
            Ensure.that(() => eventMessage).is_not_null();

            this.Log().Debug(() => "Sending message '{0}' out if there are subscribers...".format_with(typeof (Event).Name));

            _subject.OnNext(eventMessage);

            //object subscription;
            //if (_subscriptions.TryGetValue(typeof (Event), out subscription))
            //{
            //    ((ISubject<Event>) subscription).OnNext(message);
            //}
        }

        public IDisposable subscribe<Event>(Action<Event> handleEvent, Action<Exception> handleError, Func<Event, bool> filter) where Event : class, IEvent
        {
            //var subject = (ISubject<Event>) _subscriptions.GetOrAdd(typeof (Event), t => new Subject<Event>());

            if (filter == null)
            {
                filter = (message) => true;
            }
            if (handleError == null)
            {
                handleError = (ex) => { };
            }

            //var subscription = subject.Where(filter).Subscribe(handleEvent, handleError);

            var subscription = _subject.OfType<Event>().AsObservable()
                                       .Where(filter)
                                       .Subscribe(handleEvent, handleError);

            return subscription;
        }
    }
}