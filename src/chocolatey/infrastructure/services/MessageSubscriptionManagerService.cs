namespace chocolatey.infrastructure.services
{
    using System;
    using System.Collections.Concurrent;
    using System.Reactive.Linq;
    using System.Reactive.Subjects;
    using guards;
    using messaging;

    /// <summary>
    ///   Implementation of IMessageSubscriptionManagerService
    /// </summary>
    public class MessageSubscriptionManagerService : IMessageSubscriptionManagerService
    {
        //http://joseoncode.com/2010/04/29/event-aggregator-with-reactive-extensions/
        //https://github.com/shiftkey/Reactive.EventAggregator

        //private readonly ConcurrentDictionary<Type, object> _subscriptions = new ConcurrentDictionary<Type, object>();
        private readonly ISubject<object> _subject = new Subject<object>();

        public void publish<TMessage>(TMessage message) where TMessage : class, IMessage
        {
            Ensure.that(() => message).is_not_null();

            this.Log().Debug(() => "Sending message '{0}' out if there are subscribers...".format_with(typeof (TMessage).Name));

            _subject.OnNext(message);

            //object subscription;
            //if (_subscriptions.TryGetValue(typeof (TMessage), out subscription))
            //{
            //    ((ISubject<TMessage>) subscription).OnNext(message);
            //}
        }

        public IDisposable subscribe<TMessage>(Action<TMessage> handleMessage, Action<Exception> handleError, Func<TMessage, bool> filter) where TMessage : class, IMessage
        {
            //var subject = (ISubject<TMessage>) _subscriptions.GetOrAdd(typeof (TMessage), t => new Subject<TMessage>());

            if (filter == null)
            {
                filter = (message) => true;
            }
            if (handleError == null)
            {
                handleError = (ex) => { };
            }

            //var subscription = subject.Where(filter).Subscribe(handleMessage, handleError);

            var subscription = _subject.OfType<TMessage>().AsObservable()
                                       .Where(filter)
                                       .Subscribe(handleMessage, handleError);

            return subscription;
        }
    }
}