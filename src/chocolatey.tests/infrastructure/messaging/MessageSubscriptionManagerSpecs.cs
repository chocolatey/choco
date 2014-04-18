namespace chocolatey.tests.infrastructure.messaging
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using NUnit.Framework;
    using Should;
    using chocolatey.infrastructure.services;
    using context;

    public class MessageSubscriptionManagerSpecs
    {
        public abstract class MessageSubscriptionManagerSpecsBase : TinySpec
        {
            protected FakeMessage Message;

            public IMessageSubscriptionManagerService SubscriptionManager { get; private set; }

            public override void Context()
            {
                Message = new FakeMessage("yo", 12);
                SubscriptionManager = new MessageSubscriptionManagerService();
            }
        }

        public class When_using_MessageSubscriptionManager_to_subscribe_to_a_message : MessageSubscriptionManagerSpecsBase
        {
            private bool _wasCalled;
            private FakeMessage _localFakeMessage;

            public override void Context()
            {
                base.Context();
                SubscriptionManager.subscribe<FakeMessage>(x =>
                    {
                        _wasCalled = true;
                        _localFakeMessage = x;
                    }, null, null);
            }

            public override void Because()
            {
                SubscriptionManager.publish(Message);
            }

            [Fact]
            public void should_have_called_the_action()
            {
                _wasCalled.ShouldBeTrue();
            }

            [Fact]
            public void should_have_passed_the_message()
            {
                _localFakeMessage.ShouldEqual(Message);
            }

            [Fact]
            public void should_have_passed_the_name_correctly()
            {
                _localFakeMessage.Name.ShouldEqual("yo");
            }

            [Fact]
            public void should_have_passed_the_digits_correctly()
            {
                _localFakeMessage.Digits.ShouldEqual(12d);
            }
        }

        public class When_using_MessageSubscriptionManager_with_long_running_events : MessageSubscriptionManagerSpecsBase
        {
            private bool _wasCalled;
            private FakeMessage _localFakeMessage;

            public override void Context()
            {
                base.Context();
                SubscriptionManager.subscribe<FakeMessage>(m =>
                    {
                        //stuff is happening
                        Thread.Sleep(2000);
                        _wasCalled = true;
                        _localFakeMessage = m;
                        Console.WriteLine("event complete");
                    }, null, null);
            }

            public override void Because()
            {
                SubscriptionManager.publish(Message);
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
                _localFakeMessage.ShouldEqual(Message);
            }
        }

        public class When_using_MessageSubscriptionManager_to_subscribe_to_a_message_with_a_filter_that_the_message_satisfies : MessageSubscriptionManagerSpecsBase
        {
            private bool _wasCalled;
            private FakeMessage _localFakeMessage;

            public override void Context()
            {
                base.Context();
                SubscriptionManager.subscribe<FakeMessage>(x =>
                    {
                        _wasCalled = true;
                        _localFakeMessage = x;
                    }, null, (message) => message.Digits > 3);
            }

            public override void Because()
            {
                SubscriptionManager.publish(Message);
            }

            [Fact]
            public void should_have_called_the_action()
            {
                _wasCalled.ShouldBeTrue();
            }

            [Fact]
            public void should_have_passed_the_message()
            {
                _localFakeMessage.ShouldEqual(Message);
            }

            [Fact]
            public void should_have_passed_the_name_correctly()
            {
                _localFakeMessage.Name.ShouldEqual("yo");
            }

            [Fact]
            public void should_have_passed_the_digits_correctly()
            {
                _localFakeMessage.Digits.ShouldEqual(12d);
            }
        }

        public class When_using_MessageSubscriptionManager_to_subscribe_to_a_message_with_a_filter_that_the_message_does_not_satisfy : MessageSubscriptionManagerSpecsBase
        {
            private bool _wasCalled;
            private FakeMessage _localFakeMessage;

            public override void Context()
            {
                base.Context();
                SubscriptionManager.subscribe<FakeMessage>(x =>
                    {
                        _wasCalled = true;
                        _localFakeMessage = x;
                    }, null, (message) => message.Digits < 3);
            }

            public override void Because()
            {
                SubscriptionManager.publish(Message);
            }

            [Fact]
            public void should_not_have_called_the_action()
            {
                _wasCalled.ShouldBeFalse();
            }

            [Fact]
            public void should_not_have_passed_the_message()
            {
                _localFakeMessage.ShouldNotEqual(Message);
            }
        }

        public class When_using_MessageSubscriptionManager_and_multiple_parties_subscribe_to_the_same_message : MessageSubscriptionManagerSpecsBase
        {
            private IList<FakeSubscriber> _list;

            public override void Context()
            {
                base.Context();

                _list = new List<FakeSubscriber>();
                do
                {
                    _list.Add(new FakeSubscriber(SubscriptionManager));
                } while (_list.Count < 5);
            }

            public override void Because()
            {
                SubscriptionManager.publish(Message);
            }
        }

        public class When_using_MessageSubscriptionManager_to_send_a_null_message : MessageSubscriptionManagerSpecsBase
        {
            private bool _errored;

            public override void Because()
            {
                try
                {
                    SubscriptionManager.publish<FakeMessage>(null);
                }
                catch (Exception)
                {
                    _errored = true;
                }
            }

            [Fact]
            public void should_throw_an_error()
            {
                Assert.Throws<ArgumentNullException>(() => SubscriptionManager.publish<FakeMessage>(null));
                _errored.ShouldBeTrue();
            }
        }
    }
}