// Copyright © 2017 Chocolatey Software, Inc
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

namespace chocolatey.infrastructure.services
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Text;
    using System.Xml;
    using System.Xml.Serialization;
    using cryptography;
    using filesystem;
    using tolerance;
    using synchronization;

    /// <summary>
    ///   XML interaction
    /// </summary>
    public sealed class XmlService : IXmlService
    {
        private readonly IFileSystem _fileSystem;
        private readonly IHashProvider _hashProvider;
        private const int MUTEX_TIMEOUT = 2000;

        public XmlService(IFileSystem fileSystem, IHashProvider hashProvider)
        {
            _fileSystem = fileSystem;
            _hashProvider = hashProvider;
        }

        public XmlType deserialize<XmlType>(string xmlFilePath)
        {
            return FaultTolerance.retry(3, () => GlobalMutex.enter(
                () =>
                {
                    return FaultTolerance.try_catch_with_logging_exception(
                    () =>
                    {
                        var xmlSerializer = new XmlSerializer(typeof(XmlType));
                        using (var fileStream = _fileSystem.open_file_readonly(xmlFilePath))
                        using (var fileReader = new StreamReader(fileStream))
                        using (var xmlReader = XmlReader.Create(fileReader))
                        {
                            if (!xmlSerializer.CanDeserialize(xmlReader))
                            {
                                this.Log().Warn("Cannot deserialize response of type {0}", typeof(XmlType));
                                return default(XmlType);
                            }

                            try
                            {
                                return (XmlType)xmlSerializer.Deserialize(xmlReader);
                            }
                            catch (InvalidOperationException ex)
                            {
                                // Check if its just a malformed document.
                                if (ex.Message.Contains("There is an error in XML document"))
                                {
                                    // If so, check for a backup file and try an parse that.
                                    if (_fileSystem.file_exists(xmlFilePath + ".backup"))
                                    {
                                        using (var backupStream = _fileSystem.open_file_readonly(xmlFilePath + ".backup"))
                                        using (var backupReader = new StreamReader(backupStream))
                                        using (var backupXmlReader = XmlReader.Create(backupReader))
                                        {
                                            var validConfig = (XmlType)xmlSerializer.Deserialize(backupXmlReader);

                                            // If there's no errors and it's valid, go ahead and replace the bad file with the backup.
                                            if (validConfig != null)
                                            {
                                                _fileSystem.copy_file(xmlFilePath + ".backup", xmlFilePath, overwriteExisting: true);
                                            }

                                            return validConfig;
                                        }
                                    }
                                }

                                throw;
                            }
                            finally
                            {
                                foreach (var updateFile in _fileSystem.get_files(_fileSystem.get_directory_name(xmlFilePath), "*.update").or_empty_list_if_null())
                                {
                                    FaultTolerance.try_catch_with_logging_exception(
                                        () => _fileSystem.delete_file(updateFile),
                                        errorMessage: "Unable to remove update file",
                                        logDebugInsteadOfError: true,
                                        isSilent: true
                                        );
                                }
                            }
                        }
                    },
                    "Error deserializing response of type {0}".format_with(typeof(XmlType)),
                    throwError: true);

                }, MUTEX_TIMEOUT),
                waitDurationMilliseconds: 200,
                increaseRetryByMilliseconds: 200);
        }

        public void serialize<XmlType>(XmlType xmlType, string xmlFilePath)
        {
            serialize(xmlType, xmlFilePath, isSilent: false);
        }

        public void serialize<XmlType>(XmlType xmlType, string xmlFilePath, bool isSilent)
        {
            _fileSystem.create_directory_if_not_exists(_fileSystem.get_directory_name(xmlFilePath));

            FaultTolerance.retry(3, () => GlobalMutex.enter(
                () =>
                {
                    FaultTolerance.try_catch_with_logging_exception(
                    () =>
                    {
                        var xmlSerializer = new XmlSerializer(typeof(XmlType));

                        // Write the updated file to memory
                        using (var memoryStream = new MemoryStream())
                        using (var streamWriter = new StreamWriter(memoryStream, encoding: new UTF8Encoding(encoderShouldEmitUTF8Identifier: true))
                        {
                            AutoFlush = true
                        }
                        ){
                            xmlSerializer.Serialize(streamWriter, xmlType);
                            streamWriter.Flush();

                            // Grab the hash of both files and compare them.
                            var originalFileHash = _hashProvider.hash_file(xmlFilePath);
                            memoryStream.Position = 0;
                            if (!originalFileHash.is_equal_to(_hashProvider.hash_stream(memoryStream)))
                            {
                                // If there wasn't a file there in the first place, just write the new one out directly.
                                if (string.IsNullOrEmpty(originalFileHash))
                                {
                                    memoryStream.Position = 0;
                                    _fileSystem.write_file(xmlFilePath, () => memoryStream);

                                    memoryStream.Close();
                                    streamWriter.Close();

                                    return;
                                }

                                // Otherwise, create an update file, and resiliently move it into place.
                                var tempUpdateFile = xmlFilePath + "." + Process.GetCurrentProcess().Id + ".update";
                                memoryStream.Position = 0;
                                _fileSystem.write_file(tempUpdateFile, () => memoryStream);

                                memoryStream.Close();
                                streamWriter.Close();

                                _fileSystem.replace_file(tempUpdateFile, xmlFilePath, xmlFilePath + ".backup");
                            }
                        }
                    },
                    errorMessage: "Error serializing type {0}".format_with(typeof(XmlType)),
                    throwError: true,
                    isSilent: isSilent);
                }, MUTEX_TIMEOUT),
                waitDurationMilliseconds: 200,
                increaseRetryByMilliseconds: 200);
        }
    }
}
