namespace chocolatey.tests.infrastructure.events.context
{
    using System;
    using chocolatey.infrastructure.services;

    public class FakeSubscriber
    {
        public FakeSubscriber(IEventSubscriptionManagerService subscriptionManager)
        {
            subscriptionManager.subscribe<FakeEvent>(x =>
                {
                    WasCalled = true;
                    ReceivedEvent = x;
                    CalledAt = DateTime.Now;
                }, null, null);
        }

        public FakeEvent ReceivedEvent { get; private set; }
        public bool WasCalled { get; private set; }
        public DateTime CalledAt { get; private set; }
    }
}