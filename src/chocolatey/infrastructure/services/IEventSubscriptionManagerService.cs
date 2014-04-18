namespace chocolatey.infrastructure.services
{
    using System;
    using events;

    /// <summary>
    ///   Interface for EventSubscriptionManagerService
    /// </summary>
    public interface IEventSubscriptionManagerService
    {
        /// <summary>
        ///   Publishes the specified event message.
        /// </summary>
        /// <typeparam name="Event">The type of the event.</typeparam>
        /// <param name="eventMessage">The message to publish.</param>
        void publish<Event>(Event eventMessage) where Event : class, IEvent;

        /// <summary>
        ///   Subscribes to the specified event.
        /// </summary>
        /// <typeparam name="Event">The type of the event.</typeparam>
        /// <param name="handleEvent">The message handler.</param>
        /// <param name="handleError">The error handler.</param>
        /// <param name="filter">The message filter.</param>
        /// <returns>The subscription as Disposable</returns>
        IDisposable subscribe<Event>(Action<Event> handleEvent, Action<Exception> handleError, Func<Event, bool> filter) where Event : class, IEvent;
    }
}