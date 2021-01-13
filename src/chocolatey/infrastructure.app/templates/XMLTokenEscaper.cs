using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security;

namespace chocolatey.infrastructure.app.templates
{
    //escape strings so that
    class XMLTokenEscaper : ITokenEscaper
    {
        public string escape(string toEscape)
        {
            return SecurityElement.Escape(toEscape);
        }
    }
}
