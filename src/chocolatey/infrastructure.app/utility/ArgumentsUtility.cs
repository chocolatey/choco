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

namespace chocolatey.infrastructure.app.utility
{
    //todo: maybe find a better name/location for this

    public static class ArgumentsUtility
    {
        public static bool arguments_contain_sensitive_information(string commandArguments)
        {
            //todo:this check is naive, we should switch to regex
            //this picks up cases where arguments are passed with '-' and '--'
            return commandArguments.contains("-install-arguments-sensitive")
             || commandArguments.contains("-package-parameters-sensitive")
             || commandArguments.contains("apikey ")
             || commandArguments.contains("config ")
             || commandArguments.contains("push ") // push can be passed w/out parameters, it's fine to log it then
             || commandArguments.contains("-p ")
             || commandArguments.contains("-p=")
             || commandArguments.contains("-password")
             || commandArguments.contains("-cp ")
             || commandArguments.contains("-cp=")
             || commandArguments.contains("-certpassword")
             || commandArguments.contains("-k ")
             || commandArguments.contains("-k=")
             || commandArguments.contains("-key ")
             || commandArguments.contains("-key=")
             || commandArguments.contains("-apikey")
             || commandArguments.contains("-api-key")
             || commandArguments.contains("-apikey")
             || commandArguments.contains("-api-key")
            ;
        }
    }
}