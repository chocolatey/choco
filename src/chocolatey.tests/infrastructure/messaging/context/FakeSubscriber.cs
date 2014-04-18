namespace chocolatey.tests.infrastructure.messaging.context
{
    using System;
    using chocolatey.infrastructure.services;

    public class FakeSubscriber
    {
        public FakeSubscriber(IMessageSubscriptionManagerService subscriptionManager)
        {
            subscriptionManager.subscribe<FakeMessage>(x =>
                {
                    WasCalled = true;
                    ReceivedMessage = x;
                    CalledAt = DateTime.Now;
                }, null, null);
        }

        public FakeMessage ReceivedMessage { get; private set; }
        public bool WasCalled { get; private set; }
        public DateTime CalledAt { get; private set; }
    }
}