namespace chocolatey.infrastructure.services
{
    using System;
    using messaging;

    /// <summary>
    ///   Interface for MessageSubscriptionManagerService
    /// </summary>
    public interface IMessageSubscriptionManagerService
    {
        /// <summary>
        ///   Publishes the specified message.
        /// </summary>
        /// <typeparam name="TMessage">The type of the message.</typeparam>
        /// <param name="message">The message to publish.</param>
        void publish<TMessage>(TMessage message) where TMessage : class, IMessage;

        /// <summary>
        ///   Subscribes to the specified message.
        /// </summary>
        /// <typeparam name="TMessage">The type of the message.</typeparam>
        /// <param name="handleMessage">The message handler.</param>
        /// <param name="handleError">The error handler.</param>
        /// <param name="filter">The message filter.</param>
        /// <returns>The subscription as Disposable</returns>
        IDisposable subscribe<TMessage>(Action<TMessage> handleMessage, Action<Exception> handleError, Func<TMessage, bool> filter) where TMessage : class, IMessage;
    }
}