namespace chocolatey.tests.infrastructure.messaging.context
{
    using chocolatey.infrastructure.messaging;

    public class FakeMessage : IMessage
    {
        public FakeMessage(string name, double digits)
        {
            Name = name;
            Digits = digits;
        }

        public string Name { get; private set; }
        public double Digits { get; private set; }
    }
}