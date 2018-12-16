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

namespace chocolatey.infrastructure.app.templates
{
    public class ChocolateyVerificationFileTemplate
    {
        public static string Template = @"
Note: Include this file if including binaries you have the right to distribute. 
Otherwise delete. this file. If you are the software author, you can change this
mention you are the author of the software.

===DELETE ABOVE THIS LINE AND THIS LINE===

VERIFICATION
Verification is intended to assist the Chocolatey moderators and community
in verifying that this package's contents are trustworthy.
 
<Include details of how to verify checksum contents>
<If software vendor, explain that here - checksum verification instructions are optional>";
    }
}
