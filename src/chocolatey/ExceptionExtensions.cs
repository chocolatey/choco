namespace chocolatey
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public static class ExceptionExtensions
    {
        public static IEnumerable<Exception> Enumerate(this Exception error)
        {
            while (error != null)
            {
                yield return error;

                error = error.InnerException;
            }
        }
    }
}
