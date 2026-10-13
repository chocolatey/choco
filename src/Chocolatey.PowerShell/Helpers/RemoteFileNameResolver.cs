// Copyright © 2017 - 2026 Chocolatey Software, Inc
// Copyright © 2011 - 2017 RealDimensions Software, LLC
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
//
// You may obtain a copy of the License at
//
// 	http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.IO;
using System.Text.RegularExpressions;

namespace Chocolatey.PowerShell.Helpers
{
    /// <summary>
    /// Works out the name of a remote file from the details of the request that was made for it.
    /// </summary>
    public static class RemoteFileNameResolver
    {
        private static readonly Regex _badCharacters = new Regex("[" + Regex.Escape(new string(Path.GetInvalidFileNameChars())) + "\\=\\;]");

        /// <summary>
        /// Chooses a file name, trying the <c>Content-Disposition</c> header, then the <c>Location</c> header, then the
        /// address the response came from, then the address that was requested, and falling back to the default name
        /// when none of them gives a usable name.
        /// </summary>
        /// <param name="defaultName">The name to use when a name cannot be determined.</param>
        /// <param name="requestUrl">The address that was requested, as it was given.</param>
        /// <param name="responseUri">The address the response came from.</param>
        /// <param name="contentDisposition">The value of the <c>Content-Disposition</c> header, if there was one.</param>
        /// <param name="location">The value of the <c>Location</c> header, if there was one.</param>
        /// <param name="debug">Receives a description of each step taken.</param>
        /// <returns>The file name.</returns>
        public static string Resolve(
            string defaultName,
            string requestUrl,
            Uri responseUri,
            string contentDisposition,
            string location,
            Action<string> debug)
        {
            string fileName = null;

            // start with content-disposition header
            if (!string.IsNullOrEmpty(contentDisposition))
            {
                const string fileHeaderName = "filename=";
                var index = contentDisposition.LastIndexOf(fileHeaderName, StringComparison.OrdinalIgnoreCase);
                if (index > -1)
                {
                    debug("Using header 'Content-Disposition' to determine file name.");
                    fileName = contentDisposition.Substring(index + fileHeaderName.Length).Replace("\"", string.Empty);
                }
            }

            fileName = WithoutBadCharacters(fileName);

            // If empty, check location header next
            if (string.IsNullOrEmpty(fileName) && !string.IsNullOrEmpty(location))
            {
                debug("Using header 'Location' to determine file name.");
                fileName = Path.GetFileName(location);
            }

            fileName = WithoutBadCharacters(fileName);

            // Next comes using the response url value
            if (string.IsNullOrEmpty(fileName))
            {
                var responseUrl = responseUri.ToString();
                if (!responseUrl.Contains("?"))
                {
                    debug($"Using response url to determine file name. '{responseUrl}'");
                    fileName = Path.GetFileName(responseUrl);
                }
            }

            fileName = WithoutBadCharacters(fileName);

            // Next comes using the request url value
            if (string.IsNullOrEmpty(fileName))
            {
                var extension = Path.GetExtension(requestUrl);
                if (!requestUrl.Contains("?") && !string.IsNullOrEmpty(extension))
                {
                    debug($"Using request url to determine file name. ' {requestUrl}'");
                    fileName = Path.GetFileName(requestUrl);
                }
            }

            // when all else fails, default the name
            if (string.IsNullOrEmpty(fileName) || _badCharacters.IsMatch(fileName))
            {
                debug($"File name is null or illegal. Using {defaultName} instead.");
                fileName = defaultName;
            }

            debug($"File name determined from url is '{fileName}'");

            return fileName;
        }

        private static string WithoutBadCharacters(string fileName)
        {
            return !(fileName is null) && _badCharacters.IsMatch(fileName) ? null : fileName;
        }
    }
}
