// Copyright © 2017 - 2022 Chocolatey Software, Inc
// Copyright © 2011 - 2017 RealDimensions Software, LLC
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
//
// You may obtain a copy of the License at
//
//  http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace chocolatey.infrastructure.app.commands
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using attributes;
    using commandline;
    using configuration;
    using infrastructure.commands;
    using logging;
    using SimpleInjector;

    [CommandFor("help", "displays top level help information for choco")]
    public class ChocolateyHelpCommand : ICommand
    {
        private readonly Container _container;

        public ChocolateyHelpCommand(Container container)
        {
            _container = container;
        }

        public virtual void configure_argument_parser(OptionSet optionSet, ChocolateyConfiguration configuration)
        {
        }

        public virtual void handle_additional_argument_parsing(IList<string> unparsedArguments, ChocolateyConfiguration configuration)
        {
            configuration.Input = string.Join(" ", unparsedArguments);
        }

        public virtual void handle_validation(ChocolateyConfiguration configuration)
        {
        }

        public virtual void help_message(ChocolateyConfiguration configuration)
        {
            display_help_message(_container);
        }

        public virtual void noop(ChocolateyConfiguration configuration)
        {
            display_help_message(_container);
        }

        public virtual void run(ChocolateyConfiguration configuration)
        {
            display_help_message(_container);
        }

        public virtual bool may_require_admin_access()
        {
            return false;
        }

        public static void display_help_message(Container container = null)
        {
            var commandsLog = new StringBuilder();
            if (null == container)
            {
                container = new Container();
                "chocolatey".Log().Warn(@"You have encountered a scenario where our container is not available. Please:
 1. Run `choco -?` for a list of commands available to be run.
 2. If you're able to reproduce this message, open an issue on GitHub so we can investigate.
");
            }

            IEnumerable<ICommand> commands = container.GetAllInstances<ICommand>();

            foreach (var command in commands.or_empty_list_if_null().SelectMany(c =>
            {
                return c.GetType().GetCustomAttributes(typeof(CommandForAttribute), false).Cast<CommandForAttribute>();
            }).OrderBy(c => c.CommandName))
            {
                commandsLog.AppendFormat(" * {0} - {1}\n", command.CommandName, command.Description);
            }

            "chocolatey".Log().Info(@"This is a listing of all of the different things you can pass to choco.
");

            "chocolatey".Log().Warn(ChocolateyLoggers.Important, "DEPRECATION NOTICE");
            "chocolatey".Log().Warn(@"
The shims `chocolatey`, `cinst`, `clist`, `cpush`, `cuninst` and `cup` are deprecated.
We recommend updating all scripts to use their full command equivalent as these will be
removed in v2.0.0 of Chocolatey.
");
            "chocolatey".Log().Info(ChocolateyLoggers.Important, "Options and Switches");

            "chocolatey".Log().Info(@"
 -v, --version
     Version - Prints out the Chocolatey version. Available in 0.9.9+.
");

            "chocolatey".Log().Info(ChocolateyLoggers.Important, "Commands");
            "chocolatey".Log().Info(@"
{0}

Please run chocolatey with `choco command -help` for specific help on
 each command.
".format_with(commandsLog.ToString()));
            "chocolatey".Log().Info(ChocolateyLoggers.Important, @"How To Pass Options / Switches");
            "chocolatey".Log().Info(@"
You can pass options and switches in the following ways:

 * Unless stated otherwise, an option/switch should only be passed one
   time. Otherwise you may find weird/non-supported behavior.
 * `-`, `/`, or `--` (one character switches should not use `--`)
 * **Option Bundling / Bundled Options**: One character switches can be
   bundled. e.g. `-d` (debug), `-f` (force), `-v` (verbose), and `-y`
   (confirm yes) can be bundled as `-dfvy`.
 * NOTE: If `debug` or `verbose` are bundled with local options
   (not the global ones above), some logging may not show up until after
   the local options are parsed.
 * **Use Equals**: You can also include or not include an equals sign
   `=` between options and values.
 * **Quote Values**: When you need to quote an entire argument, such as
   when using spaces, please use a combination of double quotes and
   apostrophes (`""'value'""`). In cmd.exe you can just use double quotes
   (`""value""`) but in powershell.exe you should use backticks
   (`` `""value`"" ``) or apostrophes (`'value'`). Using the combination
   allows for both shells to work without issue, except for when the next
   section applies.
 * **Pass quotes in arguments**: When you need to pass quoted values to
   to something like a native installer, you are in for a world of fun. In
   cmd.exe you must pass it like this: `-ia ""/yo=""""Spaces spaces""""""`. In
   PowerShell.exe, you must pass it like this: `-ia '/yo=""""Spaces spaces""""'`.
   No other combination will work. In PowerShell.exe if you are on version
   v3+, you can try `--%` before `-ia` to just pass the args through as is,
   which means it should not require any special workarounds.
 * **Periods in PowerShell**: If you need to pass a period as part of a
   value or a path, PowerShell doesn't always handle it well. Please
   quote those values using ""Quote Values"" section above.
 * Options and switches apply to all items passed, so if you are
   installing multiple packages, and you use `--version=1.0.0`, choco
   is going to look for and try to install version 1.0.0 of every
   package passed. So please split out multiple package calls when
   wanting to pass specific options.
");
            "chocolatey".Log().Info(ChocolateyLoggers.Important, @"Scripting / Integration - Best Practices / Style Guide");
            "chocolatey".Log().Info(@"
When writing scripts, such as PowerShell scripts passing options and
switches, there are some best practices to follow to ensure that you
don't run into issues later. This also applies to integrations that
are calling Chocolatey and parsing output. Chocolatey **uses**
PowerShell, but it is an exe, so it cannot return PowerShell objects.

Following these practices ensures both readability of your scripts AND
compatibility across different versions and editions of Chocolatey.
Following this guide will ensure your experience is not frustrating
based on choco not receiving things you think you are passing to it.

 * For consistency, always use `choco`, not `choco.exe`. Never use
   shortcut commands like `cinst` or `cup` (The shortcuts `cinst`
   and `cup` will be removed in v2.0.0).
 * Always have the command as the first argument to `choco`. e.g.
   `choco install`, where `install` is the command.
 * If there is a subcommand, ensure that is the second argument. e.g.
   `choco source list`, where `source` is the command and `list` is the
   subcommand.
 * Typically the subject comes next. If installing packages, the
   subject would be the package names, e.g. `choco install pkg1 pkg2`.
 * Never use 'nupkg' or point directly to a nupkg file UNLESS using
   'choco push'. Use the source folder instead, e.g. `choco install
   <package id> --source=""'c:\folder\with\package'""` instead of
   `choco install DoNotDoThis.1.0.nupkg` or `choco install DoNotDoThis
    --source=""'c:\folder\with\package\DoNotDoThis.1.0.nupkg'""`.
 * Switches and parameters are called simply options. Options come
   after the subject. e.g. `choco install pkg1 --debug --verbose`.
 * Never use the force option (`--force`/`-f`) in scripts (or really
   otherwise as a default mode of use). Force is an override on
   Chocolatey behavior. If you are wondering why Chocolatey isn't doing
   something like the documentation says it should, it's likely because
   you are using force. Stop.
 * Always use full option name. If the short option is `-n`, and the
   full option is `--name`, use `--name`. The only acceptable short
   option for use in scripts is `-y`. Find option names in help docs
   online or through `choco -?` /`choco [Command Name] -?`.
 * For scripts that are running automated, always use `-y`. Do note
   that even with `-y` passed, some things / state issues detected will
   temporarily stop for input - the key here is temporarily. They will
   continue without requiring any action after the temporary timeout
   (typically 30 seconds).
 * Full option names are prepended with two dashes, e.g. `--` or
   `--debug --verbose --ignore-proxy`.
 * When setting a value to an option, always put an equals (`=`)
   between the name and the setting, e.g. `--source=""'local'""`.
 * When setting a value to an option, always surround the value
   properly with double quotes bookending apostrophes, e.g.
   `--source=""'internal_server'""`.
 * If you are building PowerShell scripts, you can most likely just
   simply use apostrophes surrounding option values, e.g.
   `--source='internal_server'`.
 * Prefer upgrade to install in scripts. You can't `install` to a newer
   version of something, but you can `choco upgrade` which will do both
   upgrade or install (unless switched off explicitly).
 * If you are sharing the script with others, pass `--source` to be
   explicit about where the package is coming from. Use full link and
   not source name ('https://community.chocolatey.org/api/v2' versus
   'chocolatey').
 * If parsing output, you might want to use `--limit-output`/`-r` to
   get output in a more machine parseable format. NOTE: Not all
   commands handle return of information in an easily digestible
   output.
 * Use exit codes to determine status. Chocolatey exits with 0 when
   everything worked appropriately and other exits codes like 1 when
   things error. There are package specific exit codes that are
   recommended to be used and reboot indicating exit codes as well. To
   check exit code when using PowerShell, immediately call
   `$exitCode = $LASTEXITCODE` to get the value choco exited with.

Here's an example following bad practices (line breaks added for
 readability):

  `choco install pkg1 -y -params '/Option:Value /Option2:value with
   spaces' --c4b-option 'Yaass' --option-that-is-new 'dude upgrade'`

Now here is that example written with best practices (again line
 breaks added for readability - there are not line continuations
 for choco):

  `choco upgrade pkg1 -y --source=""'https://community.chocolatey.org/api/v2'""
   --package-parameters=""'/Option:Value /Option2:value with spaces'""
   --c4b-option=""'Yaass'"" --option-that-is-new=""'dude upgrade'""`

Note the differences between the two:
 * Which is more self-documenting?
 * Which will allow for the newest version of something installed or
   upgraded to (which allows for more environmental consistency on
   packages and versions)?
 * Which may throw an error on a badly passed option?
 * Which will throw errors on unknown option values? See explanation
   below.

Chocolatey ignores options it doesn't understand, but it can only
 ignore option values if they are tied to the option with an
 equals sign ('='). Note those last two options in the examples above?
 If you roll off of a commercial edition or someone with older version
 attempts to run the badly crafted script `--c4b-option 'Yaass'
 --option-that-is-new 'dude upgrade'`, they are likely to see errors on
 'Yaass' and 'dude upgrade' because they are not explicitly tied to the
 option they are written after. Now compare that to the other script.
 Choco will ignore `--c4b-option=""'Yaass'""` and
 `--option-that-is-new=""'dude upgrade'""` as a whole when it doesn't
 register the options. This means that your script doesn't error.

Following these scripting best practices will ensure your scripts work
 everywhere they are used and with newer versions of Chocolatey.

");
            "chocolatey".Log().Info(ChocolateyLoggers.Important, "Default Options and Switches");
        }
    }
}
