namespace chocolatey.infrastructure.app.attributes
{
    using System;
    using domain;

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class CommandForAttribute : Attribute
    {
        public string CommandName { get; private set; }

        public CommandForAttribute(CommandNameType commandName)
        {
            CommandName = commandName.to_string();
        } 
        
        public CommandForAttribute(string commandName)
        {
            CommandName = commandName;
        }
    }
}