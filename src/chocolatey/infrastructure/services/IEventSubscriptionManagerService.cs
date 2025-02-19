﻿// Copyright © 2017 - 2025 Chocolatey Software, Inc
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

using System;
using chocolatey.infrastructure.events;

namespace chocolatey.infrastructure.services
{
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
        void Publish<Event>(Event eventMessage) where Event : class, IMessage;

        /// <summary>
        ///   Subscribes to the specified event.
        /// </summary>
        /// <typeparam name="Event">The type of the event.</typeparam>
        /// <param name="handleEvent">The message handler.</param>
        /// <param name="handleError">The error handler.</param>
        /// <param name="filter">The message filter.</param>
        /// <returns>The subscription as Disposable</returns>
        IDisposable Subscribe<Event>(Action<Event> handleEvent, Action<Exception> handleError, Func<Event, bool> filter) where Event : class, IMessage;

#pragma warning disable IDE0022, IDE1006
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        void publish<Event>(Event eventMessage) where Event : class, IMessage;

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        IDisposable subscribe<Event>(Action<Event> handleEvent, Action<Exception> handleError, Func<Event, bool> filter) where Event : class, IMessage;
#pragma warning restore IDE0022, IDE1006
    }
}
