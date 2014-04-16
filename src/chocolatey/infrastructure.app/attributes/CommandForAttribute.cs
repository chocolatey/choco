namespace chocolatey.infrastructure.app.attributes
{
    using System;
    using commands;

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class CommandForAttribute : Attribute
    {
        private readonly CommandNameType _type;

        public CommandForAttribute(CommandNameType type)
        {
            _type = type;
        }
    }
}