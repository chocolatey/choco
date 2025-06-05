using Chocolatey.PowerShell.Shared; 
using System; 
using System.Collections; 
using System.Collections.Generic; 
using System.Management.Automation; 
using System.Text; 
using System.Text.RegularExpressions;
 
namespace Chocolatey.PowerShell.Commands
{ 
    [Cmdlet(VerbsCommon.Get, "PackageParameter")]
    [OutputType(typeof(Hashtable))]
    public class GetPackageParameterCommand : ChocolateyCmdlet
    {
        /*
.SYNOPSIS
Parses a string and returns a hash table array of those values for use
in package scripts.

.DESCRIPTION
This looks at a string value and parses it into a hash table array for
use in package scripts. By default this will look at
`$env:ChocolateyPackageParameters` (`--params="'/ITEM:value'"`) and
`$env:ChocolateyPackageParametersSensitive`
(`--package-parameters-sensitive="'/PASSWORD:value'"` in commercial
editions).

Learn more about using this at https://docs.chocolatey.org/en-us/guides/create/parse-packageparameters-argument

.NOTES
If you need compatibility with older versions of Chocolatey,
take a dependency on the `chocolatey-core.extension` package which
also provides this functionality. If you are pushing to the community
package repository (https://community.chocolatey.org/packages), you are required
to take a dependency on the core extension until January 2018. How to
do this is explained at https://docs.chocolatey.org/en-us/guides/create/parse-packageparameters-argument#step-3---use-core-community-extension.

The differences between this and the `chocolatey-core.extension` package
functionality is that the extension function can only do one string at a
time and it only looks at `$env:ChocolateyPackageParameters` by default.
It also only supports splitting by `:`, with this function you can
either split by `:` or `=`. For compatibility with the core extension,
build all docs with `/Item:Value`.

.INPUTS
None

.OUTPUTS
[HashTable]

.PARAMETER Parameters
OPTIONAL - Specify a string to parse. If not set, will use
`$env:ChocolateyPackageParameters` and
`$env:ChocolateyPackageParametersSensitive` to parse values from.

Parameters should be passed as "/NAME:value" or "/NAME=value". For
compatibility with `chocolatey-core.extension`, use `:`.

For example `-Parameters "/ITEM1:value /ITEM2:value with spaces"

To maintain compatibility with the prior art of the chocolatey-core.extension
function by the same name, quotes and apostrophes surrounding
parameter values will be removed. When the param is used, those items
can be added back if desired, but it's most important to ensure that
existing packages are compatible on upgrade.

.PARAMETER IgnoredArguments
Allows splatting with arguments that do not apply and future expansion.
Do not use directly.

.EXAMPLE
>
# The default way of calling, uses `$env:ChocolateyPackageParameters`
# and `$env:ChocolateyPackageParametersSensitive` - this is typically
# how things are passed in from choco.exe
$pp = Get-PackageParameters

.EXAMPLE
>
# see https://docs.chocolatey.org/en-us/guides/create/parse-packageparameters-argument
# command line call: `choco install <pkg_id> --params "'/LICENSE:value'"`
$pp = Get-PackageParameters
# Read-Host, PromptForChoice, etc are not blocking calls with Chocolatey.
# Chocolatey has a custom PowerShell host that will time these calls
# after 30 seconds, allowing headless operation to continue but offer
# prompts to users to ask questions during installation.
if (!$pp['LICENSE']) { $pp['LICENSE'] = Read-Host 'License key?' }
# set a default if not passed
if (!$pp['LICENSE']) { $pp['LICENSE'] = '1234' }

.EXAMPLE
>
$pp = Get-PackageParameters
if (!$pp['UserName']) { $pp['UserName'] = "$env:UserName" }
if (!$pp['Password']) { $pp['Password'] = Read-Host "Enter password for $($pp['UserName']):" -AsSecureString}
# fail the install/upgrade if not value is not determined
if (!$pp['Password']) { throw "Package needs Password to install, that must be provided in params or in prompt." }

.EXAMPLE
>
# Pass in your own values
Get-PackageParameters -Parameters "/Shortcut /InstallDir:'c:\program files\xyz' /NoStartup" | set r
if ($r.Shortcut) {... }
Write-Host $r.InstallDir

.LINK
Install-ChocolateyPackage

.LINK
Install-ChocolateyInstallPackage

.LINK
Install-ChocolateyZipPackage
         */

        private const string PackageParameterPattern = @"(?:^|\s+)\/(?<ItemKey>[^\:\=\s)]+)(?:(?:\:|=){1}(?:\''|\""){0,1}(?<ItemValue>.*?)(?:\''|\""){0,1}(?:(?=\s+\/)|$))?";
        private static readonly Regex _packageParameterRegex = new Regex(PackageParameterPattern, RegexOptions.Compiled);

        [Parameter(Position = 0)]
        [Alias("Params")]
        public string Parameters { get; set; } = string.Empty;

        protected override void End()
        {
            var paramStrings = new List<string>();
            var logParams = true;

            if (!string.IsNullOrEmpty(Parameters))
            {
                paramStrings.Add(Parameters);
            }
            else
            {
                WriteDebug("Parsing $env:ChocolateyPackageParameters and $env:ChocolateyPackageParametersSensitive for parameters");

                var packageParams = EnvironmentVariable(EnvironmentVariables.ChocolateyPackageParameters);
                if (!string.IsNullOrEmpty(packageParams))
                {
                    paramStrings.Add(packageParams);
                }

                var sensitiveParams = EnvironmentVariable(EnvironmentVariables.ChocolateyPackageParametersSensitive);
                if (!string.IsNullOrEmpty(sensitiveParams))
                {
                    logParams = false;
                    WriteDebug("Sensitive parameters detected, no logging of parameters.");
                    paramStrings.Add(sensitiveParams);
                }
            }

            var paramHash = new Hashtable(StringComparer.OrdinalIgnoreCase);

            foreach (var param in paramStrings)
            {
                foreach (Match match in _packageParameterRegex.Matches(param))
                {                    
                    var name = match.Groups["ItemKey"].Value.Trim();
                    var valueGroup = match.Groups["ItemValue"];

                    object value;
                    if (valueGroup.Success)
                    { 
                        value = valueGroup.Value.Trim();
                    }
                    else
                    { 
                        value = (object)true;
                    }

                    if (logParams)
                    {
                        WriteDebug($"Adding package param '{name}'='{value}'");
                    }

                    paramHash[name] = value;
                }
            }

            WriteObject(paramHash);
        }
    }
}
