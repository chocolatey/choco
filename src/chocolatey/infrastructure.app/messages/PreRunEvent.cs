namespace chocolatey.infrastructure.app.messages
{
    using events;
    using infrastructure.commands;

    public class PreRunEvent<TCommand> : IEvent where TCommand : ICommand
    {
        public TCommand Command { get; private set; }
        public object[] State { get; private set; }

        public PreRunEvent(TCommand command, object[] state)
        {
            Command = command;
            State = state;
        }
    }

    public class PostRunEvent<TCommand> : IEvent where TCommand : ICommand
    {
        public TCommand Command { get; private set; }
        public object[] State { get; private set; }

        public PostRunEvent(TCommand command, object[] state)
        {
            Command = command;
            State = state;
        }
    }
}