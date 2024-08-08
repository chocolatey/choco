using chocolatey.infrastructure.app.configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace chocolatey
{
    public static class OutputHelpers
    {
        public static void LimitedOutput(params string[] output)
        {
            "chocolatey".Log().Info(output.Join("|"));
        }
    }
}
