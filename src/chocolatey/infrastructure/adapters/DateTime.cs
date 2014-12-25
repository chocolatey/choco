namespace chocolatey.infrastructure.adapters
{
    public sealed class DateTime : IDateTime
    {
        public System.DateTime Now
        {
            get { return System.DateTime.Now; }
        }

        public System.DateTime UtcNow {
            get { return System.DateTime.UtcNow; }
        }
    }
}