namespace chocolatey.tests.integration
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Reactive.Subjects;
    using Moq;
    using chocolatey.infrastructure.events;
    using chocolatey.infrastructure.services;

    public class MockEventSubscriptionManager : Mock<IEventSubscriptionManagerService>, IEventSubscriptionManagerService
    {
        private readonly Lazy<ConcurrentDictionary<Type, IList<object>>> _messages = new Lazy<ConcurrentDictionary<Type, IList<object>>>();

        public ConcurrentDictionary<Type, IList<object>> Messages
        {
            get { return _messages.Value; }
        }

        public void publish<Event>(Event eventMessage) where Event : class, IEvent
        {
            var list = _messages.Value.GetOrAdd(typeof (Event), new List<object>());
            list.Add(eventMessage);
            Object.publish(eventMessage);
        }

        public IDisposable subscribe<Event>(Action<Event> handleEvent, Action<Exception> handleError, Func<Event, bool> filter) where Event : class, IEvent
        {
            return new Subject<Event>();
        }
    }
}