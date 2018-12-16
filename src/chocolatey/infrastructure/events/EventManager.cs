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
        public static void publish<Event>(Event message) where Event : class, IMessage
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
        public static IDisposable subscribe<Event>(Action<Event> handleEvent, Action<Exception> handleError, Func<Event, bool> filter) where Event : class, IMessage
        {
            if (_messageSubscriptionManager != null)
            {
                return _messageSubscriptionManager().subscribe(handleEvent, handleError, filter);
            }

            return null;
        }
    }
}
