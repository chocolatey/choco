namespace chocolatey.infrastructure.events
{
    using System;
    using System.ComponentModel;
    using services;

    public static class EventManager
    {
        private static Func<IEventSubscriptionManagerService> _messageSubscriptionManager = () => new EventSubscriptionManagerService();

        /// <summary>
        ///   Initializes the Message platform with the subscription manager
        /// </summary>
        /// <param name="messageSubscriptionManager">The message subscription manager.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void initialize_with(Func<IEventSubscriptionManagerService> messageSubscriptionManager)
        {
            _messageSubscriptionManager = messageSubscriptionManager;
        }

        /// <summary>
        ///   Gets the manager service.
        /// </summary>
        /// <value>
        ///   The manager service.
        /// </value>
        public static IEventSubscriptionManagerService ManagerService
        {
            get { return _messageSubscriptionManager(); }
        }

        /// <summary>
        ///   Publishes the specified message.
        /// </summary>
        /// <typeparam name="Event">The type of the event.</typeparam>
        /// <param name="message">The message.</param>
        public static void publish<Event>(Event message) where Event : class, IEvent
        {
            if (_messageSubscriptionManager != null)
            {
                _messageSubscriptionManager().publish(message);
            }
        }

        /// <summary>
        ///   Subscribes to the specified message.
        /// </summary>
        /// <typeparam name="Event">The type of the event.</typeparam>
        /// <param name="handleEvent">The handle message.</param>
        /// <param name="handleError">The handle error.</param>
        /// <param name="filter">The filter.</param>
        /// <returns>The subscription so that a service could unsubscribe</returns>
        public static IDisposable subscribe<Event>(Action<Event> handleEvent, Action<Exception> handleError, Func<Event, bool> filter) where Event : class, IEvent
        {
            if (_messageSubscriptionManager != null)
            {
                return _messageSubscriptionManager().subscribe(handleEvent, handleError, filter);
            }

            return null;
        }
    }
}