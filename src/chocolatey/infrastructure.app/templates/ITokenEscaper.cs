using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace chocolatey.infrastructure.app.templates
{
    public interface ITokenEscaper
    {
        string escape(string toEscape);
    }
}
