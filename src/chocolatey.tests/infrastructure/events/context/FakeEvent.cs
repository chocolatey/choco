namespace chocolatey.tests.infrastructure.events.context
{
    using chocolatey.infrastructure.events;

    public class FakeEvent : IEvent
    {
        public FakeEvent(string name, double digits)
        {
            Name = name;
            Digits = digits;
        }

        public string Name { get; private set; }
        public double Digits { get; private set; }
    }
}