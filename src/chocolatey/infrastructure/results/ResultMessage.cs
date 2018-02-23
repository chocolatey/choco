// Copyright © 2017 - 2018 Chocolatey Software, Inc
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

namespace chocolatey.infrastructure.results
{
    /// <summary>
    ///   A result message
    /// </summary>
    public class ResultMessage
    {
        /// <summary>
        ///   Gets or sets the type of the message.
        /// </summary>
        /// <value>
        ///   The type of the message.
        /// </value>
        public ResultType MessageType { get; private set; }

        /// <summary>
        ///   Gets or sets the message.
        /// </summary>
        /// <value>
        ///   The message.
        /// </value>
        public string Message { get; private set; }

        /// <summary>
        ///   Initializes a new instance of the <see cref="ResultMessage" /> class.
        /// </summary>
        /// <param name="messageType">Type of the message.</param>
        /// <param name="message">The message.</param>
        public ResultMessage(ResultType messageType, string message)
        {
            MessageType = messageType;
            Message = message;
        }
    }
}
