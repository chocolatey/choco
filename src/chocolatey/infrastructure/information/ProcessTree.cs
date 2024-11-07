using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace chocolatey.infrastructure.information
{
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    public class ProcessTree
    {
        // IGNORED USER AGENT PROCESSES
        // Our Pester tests may need their own exclusion list in the verification
        // updated when this list changes. Search the repo for the above string
        // in caps if you have trouble finding the corresponding list in tests
        // (should be in UserAgent.Tests.ps1).
        private static readonly string[] _filteredParents = new[]
        {
            // Windows processes and shells
            "explorer",
            "winlogon",
            "powershell",
            "pwsh",
            "cmd",
            "bash",
            // The name used to launch windows services
            // in the operating system.
            "services",
            "svchost",
            // Nested processes / invoked by the shim choco.exe
            "Chocolatey CLI",
            // Known Terminal Emulators
            "alacritty",
            "code",
            "ConEmu64",
            "ConEmuC64",
            "conhost",
            "c3270",
            "FireCMD",
            "Hyper",
            "SecureCRT",
            "Tabby",
            "wezterm",
            "wezterm-gui",
            "WindowsTerminal",
        };

        public ProcessTree(string currentProcessName)
        {
            CurrentProcessName = ToFriendlyName(currentProcessName);
        }

        public string CurrentProcessName { get; }

        public string FirstFilteredProcessName
        {
            get { return GetFirstProcess(includeIgnored: false); }
        }

        public string FirstProcessName
        {
            get { return GetFirstProcess(includeIgnored: true); }
        }

        public string LastFilteredProcessName
        {
            get { return GetLastProcess(includeIgnored: false); }
        }

        public string LastProcessName
        {
            get { return GetLastProcess(includeIgnored: true); }
        }

        public LinkedList<string> Processes { get; } = new LinkedList<string>();

        private static bool IsIgnoredProcess(string value)
        {
            return _filteredParents.Contains(value, StringComparer.OrdinalIgnoreCase);
        }

        protected virtual string ToFriendlyName(string value)
        {
            switch (value.ToLowerInvariant())
            {
                case "choco":
                    return "Chocolatey CLI";

                case "chocolateygui":
                    return "Chocolatey GUI";

                case "chocolatey-agent":
                    return "Chocolatey Agent";

                default:
                    return value;
            }
        }

        private string GetFirstProcess(bool includeIgnored)
        {
            if (Processes.Count == 0)
            {
                return null;
            }

            if (includeIgnored)
            {
                return Processes.First.Value;
            }

            LinkedListNode<string> currentNode = Processes.First;

            while (currentNode != null)
            {
                if (!IsIgnoredProcess(currentNode.Value) && currentNode.Value != CurrentProcessName)
                {
                    return ToFriendlyName(currentNode.Value);
                }

                currentNode = currentNode.Next;
            }

            return null;
        }

        private string GetLastProcess(bool includeIgnored)
        {
            if (Processes.Count == 0)
            {
                return null;
            }

            if (includeIgnored)
            {
                return Processes.Last.Value;
            }

            LinkedListNode<string> currentNode = Processes.Last;

            while (currentNode != null)
            {
                if (!IsIgnoredProcess(currentNode.Value) && currentNode.Value != CurrentProcessName)
                {
                    return ToFriendlyName(currentNode.Value);
                }

                currentNode = currentNode.Previous;
            }

            return null;
        }

        public override string ToString()
        {
            if (Processes.Count == 0)
            {
                return CurrentProcessName;
            }
            else
            {
                return CurrentProcessName + " => " + string.Join(" => ", Processes.Select(ToFriendlyName));
            }
        }

        private string GetDebuggerDisplay()
        {
            return "ProcessTree (" + ToString() + ")";
        }
    }
}
