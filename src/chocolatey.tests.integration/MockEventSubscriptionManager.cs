namespace chocolatey.tests.integration
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Reactive.Subjects;
    using Moq;
    using chocolatey.infrastructure.messaging;
    using chocolatey.infrastructure.services;

    public class MockEventSubscriptionManager : Mock<IMessageSubscriptionManagerService>, IMessageSubscriptionManagerService
    {
        private readonly Lazy<ConcurrentDictionary<Type, IList<object>>> _messages = new Lazy<ConcurrentDictionary<Type, IList<object>>>();

        public ConcurrentDictionary<Type, IList<object>> Messages
        {
            get { return _messages.Value; }
        }

        public void publish<TMessage>(TMessage message) where TMessage : class, IMessage
        {
            var list = _messages.Value.GetOrAdd(typeof (TMessage), new List<object>());
            list.Add(message);
            Object.publish(message);
        }

        public IDisposable subscribe<TMessage>(Action<TMessage> handleMessage, Action<Exception> handleError, Func<TMessage, bool> filter) where TMessage : class, IMessage
        {
            return new Subject<TMessage>();
        }
    }
}