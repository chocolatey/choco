// Copyright © 2017 - 2021 Chocolatey Software, Inc
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
        private const int MutexTimeout = 2000;

        public XmlService(IFileSystem fileSystem, IHashProvider hashProvider)
        {
            _fileSystem = fileSystem;
            _hashProvider = hashProvider;
        }

        public XmlType Deserialize<XmlType>(string xmlFilePath)
        {
            return Deserialize<XmlType>(xmlFilePath, 3);
        }

        public XmlType Deserialize<XmlType>(string xmlFilePath, int retryCount)
        {
            return FaultTolerance.Retry(retryCount, () => GlobalMutex.Enter(
               () =>
               {
                   this.Log().Trace("Entered mutex to deserialize '{0}'".FormatWith(xmlFilePath));

                   return FaultTolerance.TryCatchWithLoggingException(
                   () =>
                   {
                       var xmlSerializer = new XmlSerializer(typeof(XmlType));
                       using (var fileStream = _fileSystem.OpenFileReadonly(xmlFilePath))
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
                                   if (_fileSystem.FileExists(xmlFilePath + ".backup"))
                                   {
                                       using (var backupStream = _fileSystem.OpenFileReadonly(xmlFilePath + ".backup"))
                                       using (var backupReader = new StreamReader(backupStream))
                                       using (var backupXmlReader = XmlReader.Create(backupReader))
                                       {
                                           var validConfig = (XmlType)xmlSerializer.Deserialize(backupXmlReader);

                                           // If there's no errors and it's valid, go ahead and replace the bad file with the backup.
                                           if (validConfig != null)
                                           {
                                               // Close fileReader so that we can copy the file without it being locked.
                                               fileReader.Close();
                                               _fileSystem.CopyFile(xmlFilePath + ".backup", xmlFilePath, overwriteExisting: true);
                                           }

                                           return validConfig;
                                       }
                                   }
                               }

                               throw;

                           }
                           finally
                           {
                               foreach (var updateFile in _fileSystem.GetFiles(_fileSystem.GetDirectoryName(xmlFilePath), "*.update").OrEmpty())
                               {
                                   this.Log().Debug("Removing '{0}'".FormatWith(updateFile));
                                   FaultTolerance.TryCatchWithLoggingException(
                                       () => _fileSystem.DeleteFile(updateFile),
                                       errorMessage: "Unable to remove update file",
                                       logDebugInsteadOfError: true,
                                       isSilent: true
                                       );
                               }
                           }
                       }
                   },
                   "Error deserializing response of type {0}".FormatWith(typeof(XmlType)),
                   throwError: true);

               }, MutexTimeout),
                waitDurationMilliseconds: 200,
                increaseRetryByMilliseconds: 200);
        }

        public void Serialize<XmlType>(XmlType xmlType, string xmlFilePath)
        {
            Serialize(xmlType, xmlFilePath, isSilent: false);
        }

        public void Serialize<XmlType>(XmlType xmlType, string xmlFilePath, bool isSilent)
        {
            _fileSystem.EnsureDirectoryExists(_fileSystem.GetDirectoryName(xmlFilePath));

            FaultTolerance.Retry(3, () => GlobalMutex.Enter(
                () =>
                {
                    this.Log().Trace("Entered mutex to serialize '{0}'".FormatWith(xmlFilePath));
                    FaultTolerance.TryCatchWithLoggingException(
                    () =>
                    {
                        var xmlSerializer = new XmlSerializer(typeof(XmlType));

                        this.Log().Trace("Opening memory stream for xml file creation.");
                        using (var memoryStream = new MemoryStream())
                        using (var streamWriter = new StreamWriter(memoryStream, encoding: new UTF8Encoding(encoderShouldEmitUTF8Identifier: true))
                        {
                            AutoFlush = true
                        }
                        ){
                            xmlSerializer.Serialize(streamWriter, xmlType);
                            streamWriter.Flush();

                            // Grab the hash of both files and compare them.
                            this.Log().Trace("Hashing original file at '{0}'".FormatWith(xmlFilePath));
                            var originalFileHash = _hashProvider.ComputeFileHash(xmlFilePath);
                            memoryStream.Position = 0;
                            if (!originalFileHash.IsEqualTo(_hashProvider.ComputeStreamHash(memoryStream)))
                            {
                                this.Log().Trace("The hashes were different.");
                                // If there wasn't a file there in the first place, just write the new one out directly.
                                if (string.IsNullOrEmpty(originalFileHash))
                                {
                                    this.Log().Debug("There was no original file at '{0}'".FormatWith(xmlFilePath));
                                    memoryStream.Position = 0;
                                    _fileSystem.WriteFile(xmlFilePath, () => memoryStream);

                                    this.Log().Trace("Closing xml memory stream.");
                                    memoryStream.Close();
                                    streamWriter.Close();

                                    return;
                                }

                                // Otherwise, create an update file, and resiliently move it into place.
                                var tempUpdateFile = xmlFilePath + "." + Process.GetCurrentProcess().Id + ".update";
                                this.Log().Trace("Creating a temp file at '{0}'".FormatWith(tempUpdateFile));
                                memoryStream.Position = 0;
                                this.Log().Trace("Writing file '{0}'".FormatWith(tempUpdateFile));
                                _fileSystem.WriteFile(tempUpdateFile, () => memoryStream);

                                memoryStream.Close();
                                streamWriter.Close();

                                this.Log().Trace("Replacing file '{0}' with '{1}'.".FormatWith(xmlFilePath, tempUpdateFile));
                                _fileSystem.ReplaceFile(tempUpdateFile, xmlFilePath, xmlFilePath + ".backup");
                            }
                        }
                    },
                    errorMessage: "Error serializing type {0}".FormatWith(typeof(XmlType)),
                    throwError: true,
                    isSilent: isSilent);
                }, MutexTimeout),
                waitDurationMilliseconds: 200,
                increaseRetryByMilliseconds: 200);
        }

#pragma warning disable IDE1006
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public XmlType deserialize<XmlType>(string xmlFilePath)
            => Deserialize<XmlType>(xmlFilePath);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public XmlType deserialize<XmlType>(string xmlFilePath, int retryCount)
            => Deserialize<XmlType>(xmlFilePath, retryCount);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public void serialize<XmlType>(XmlType xmlType, string xmlFilePath)
            => Serialize<XmlType>(xmlType, xmlFilePath);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public void serialize<XmlType>(XmlType xmlType, string xmlFilePath, bool isSilent)
            => Serialize<XmlType>(xmlType, xmlFilePath, isSilent);
#pragma warning restore IDE1006
    }
}
