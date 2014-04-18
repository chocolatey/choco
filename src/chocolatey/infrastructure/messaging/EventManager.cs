namespace chocolatey.infrastructure.messaging
{
    using System;
    using services;

    public static class EventManager
    {
        private static Func<IMessageSubscriptionManagerService> _messageSubscriptionManager;

        /// <summary>
        ///   Initializes the Message platform with the subscription manager
        /// </summary>
        /// <param name="messageSubscriptionManager">The message subscription manager.</param>
        public static void initialize_with(Func<IMessageSubscriptionManagerService> messageSubscriptionManager)
        {
            _messageSubscriptionManager = messageSubscriptionManager;
        }

        /// <summary>
        ///   Gets the manager service.
        /// </summary>
        /// <value>
        ///   The manager service.
        /// </value>
        public static IMessageSubscriptionManagerService ManagerService
        {
            get { return _messageSubscriptionManager(); }
        }

        /// <summary>
        ///   Publishes the specified message.
        /// </summary>
        /// <typeparam name="TMessage">The type of the message.</typeparam>
        /// <param name="message">The message.</param>
        public static void publish<TMessage>(TMessage message) where TMessage : class, IMessage
        {
            if (_messageSubscriptionManager != null)
            {
                _messageSubscriptionManager().publish(message);
            }
        }

        /// <summary>
        ///   Subscribes to the specified message.
        /// </summary>
        /// <typeparam name="TMessage">The type of the message.</typeparam>
        /// <param name="handleMessage">The handle message.</param>
        /// <param name="handleError">The handle error.</param>
        /// <param name="filter">The filter.</param>
        /// <returns>The subscription so that a service could unsubscribe</returns>
        public static IDisposable subscribe<TMessage>(Action<TMessage> handleMessage, Action<Exception> handleError, Func<TMessage, bool> filter) where TMessage : class, IMessage
        {
            if (_messageSubscriptionManager != null)
            {
                return _messageSubscriptionManager().subscribe(handleMessage, handleError, filter);
            }

            return null;
        }
    }
}