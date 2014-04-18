namespace chocolatey.infrastructure.app.attributes
{
    using System;
    using commands;

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class CommandForAttribute : Attribute
    {
        public CommandNameType CommandName { get; set; }

        public CommandForAttribute(CommandNameType commandName)
        {
            CommandName = commandName;
        }
    }
}