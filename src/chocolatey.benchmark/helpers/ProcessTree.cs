using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace chocolatey.benchmark.helpers
{
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    public sealed class ProcessTree
    {
        private static readonly string[] _filteredParents = new[]
        {
                "explorer",
                "powershell",
                "pwsh",
                "cmd",
                "bash"
        };

        public ProcessTree(string currentProcessName)
        {
            CurrentProcessName = currentProcessName;
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
                if (!IsIgnoredProcess(currentNode.Value))
                {
                    return currentNode.Value;
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
                if (!IsIgnoredProcess(currentNode.Value))
                {
                    return currentNode.Value;
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
                return CurrentProcessName + " =>" + string.Join(" => ", Processes);
            }
        }

        private string GetDebuggerDisplay()
        {
            return "ProcessTree (" + ToString() + ")";
        }
    }
}