using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace chocolatey.infrastructure.app.templates
{
    // Represents the escaping strategy of not escaping anything
    class PlaintextTokenEscaper : ITokenEscaper
    {
        public string escape(string toEscape)
        {
            return toEscape;
        }
    }
}
