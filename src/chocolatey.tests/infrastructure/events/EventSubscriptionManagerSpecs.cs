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

namespace chocolatey.tests.infrastructure.events
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using chocolatey.infrastructure.services;
    using context;
    using NUnit.Framework;
    using Should;

    public class EventSubscriptionManagerSpecs
    {
        public abstract class EventSubscriptionManagerSpecsBase : TinySpec
        {
            protected FakeEvent Event;

            public IEventSubscriptionManagerService SubscriptionManager { get; private set; }

            public override void Context()
            {
                Event = new FakeEvent("yo", 12);
                SubscriptionManager = new EventSubscriptionManagerService();
            }
        }

        public class when_using_eventSubscriptionManager_to_subscribe_to_an_event : EventSubscriptionManagerSpecsBase
        {
            private bool _wasCalled;
            private FakeEvent _localFakeEvent;

            public override void Context()
            {
                base.Context();
                SubscriptionManager.subscribe<FakeEvent>(
                    x =>
                    {
                        _wasCalled = true;
                        _localFakeEvent = x;
                    },
                    null,
                    null);
            }

            public override void Because()
            {
                SubscriptionManager.publish(Event);
            }

            [Fact]
            public void should_have_called_the_action()
            {
                _wasCalled.ShouldBeTrue();
            }

            [Fact]
            public void should_have_passed_the_message()
            {
                _localFakeEvent.ShouldEqual(Event);
            }

            [Fact]
            public void should_have_passed_the_name_correctly()
            {
                _localFakeEvent.Name.ShouldEqual("yo");
            }

            [Fact]
            public void should_have_passed_the_digits_correctly()
            {
                _localFakeEvent.Digits.ShouldEqual(12d);
            }
        }

        public class when_using_eventSubscriptionManager_with_long_running_event_subscriber : EventSubscriptionManagerSpecsBase
        {
            private bool _wasCalled;
            private FakeEvent _localFakeEvent;

            public override void Context()
            {
                base.Context();
                SubscriptionManager.subscribe<FakeEvent>(
                    m =>
                    {
                        //stuff is happening
                        Thread.Sleep(2000);
                        _wasCalled = true;
                        _localFakeEvent = m;
                        Console.WriteLine("event complete");
                    },
                    null,
                    null);
            }

            public override void Because()
            {
                SubscriptionManager.publish(Event);
            }

            [Fact]
            public void should_wait_the_event_to_complete()
            {
                Console.WriteLine("event complete should be above this");
                _wasCalled.ShouldBeTrue();
            }

            [Fact]
            public void should_have_passed_the_message()
            {
                _localFakeEvent.ShouldEqual(Event);
            }
        }

        public class when_using_eventSubscriptionManager_to_subscribe_to_an_event_with_a_filter_that_the_event_satisfies : EventSubscriptionManagerSpecsBase
        {
            private bool _wasCalled;
            private FakeEvent _localFakeEvent;

            public override void Context()
            {
                base.Context();
                SubscriptionManager.subscribe<FakeEvent>(
                    x =>
                    {
                        _wasCalled = true;
                        _localFakeEvent = x;
                    },
                    null,
                    (message) => message.Digits > 3);
            }

            public override void Because()
            {
                SubscriptionManager.publish(Event);
            }

            [Fact]
            public void should_have_called_the_action()
            {
                _wasCalled.ShouldBeTrue();
            }

            [Fact]
            public void should_have_passed_the_message()
            {
                _localFakeEvent.ShouldEqual(Event);
            }

            [Fact]
            public void should_have_passed_the_name_correctly()
            {
                _localFakeEvent.Name.ShouldEqual("yo");
            }

            [Fact]
            public void should_have_passed_the_digits_correctly()
            {
                _localFakeEvent.Digits.ShouldEqual(12d);
            }
        }

        public class when_using_eventSubscriptionManager_to_subscribe_to_an_event_with_a_filter_that_the_event_does_not_satisfy : EventSubscriptionManagerSpecsBase
        {
            private bool _wasCalled;
            private FakeEvent _localFakeEvent;

            public override void Context()
            {
                base.Context();
                SubscriptionManager.subscribe<FakeEvent>(
                    x =>
                    {
                        _wasCalled = true;
                        _localFakeEvent = x;
                    },
                    null,
                    (message) => message.Digits < 3);
            }

            public override void Because()
            {
                SubscriptionManager.publish(Event);
            }

            [Fact]
            public void should_not_have_called_the_action()
            {
                _wasCalled.ShouldBeFalse();
            }

            [Fact]
            public void should_not_have_passed_the_message()
            {
                _localFakeEvent.ShouldNotEqual(Event);
            }
        }

        public class when_using_eventSubscriptionManager_and_multiple_parties_subscribe_to_the_same_event : EventSubscriptionManagerSpecsBase
        {
            private IList<FakeSubscriber> _list;

            public override void Context()
            {
                base.Context();

                _list = new List<FakeSubscriber>();
                do
                {
                    _list.Add(new FakeSubscriber(SubscriptionManager));
                }
                while (_list.Count < 5);
            }

            public override void Because()
            {
                SubscriptionManager.publish(Event);
            }
        }

        public class when_using_eventSubscriptionManager_to_send_a_null_event_message : EventSubscriptionManagerSpecsBase
        {
            private bool _errored;

            public override void Because()
            {
                try
                {
                    SubscriptionManager.publish<FakeEvent>(null);
                }
                catch (Exception)
                {
                    _errored = true;
                }
            }

            [Fact]
            public void should_throw_an_error()
            {
                Assert.Throws<ArgumentNullException>(() => SubscriptionManager.publish<FakeEvent>(null));
                _errored.ShouldBeTrue();
            }
        }
    }
}
