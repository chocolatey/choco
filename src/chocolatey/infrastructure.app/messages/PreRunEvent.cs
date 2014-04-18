namespace chocolatey.infrastructure.app.messages
{
    using events;
    using infrastructure.commands;

    public class PreRunEvent<Command> : IEvent where Command: ICommand
    {
         
    }
}