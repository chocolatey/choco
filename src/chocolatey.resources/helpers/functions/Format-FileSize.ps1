# Copyright © 2011 - Present RealDimensions Software, LLC
# 
# Licensed under the Apache License, Version 2.0 (the "License");
# you may not use this file except in compliance with the License.
# 
# You may obtain a copy of the License at
# 
#   http://www.apache.org/licenses/LICENSE-2.0
# 
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.

Function Format-FileSize() {
    Param ([double]$size)
    Foreach ($unit in @('B', 'KB', 'MB', 'GB', 'TB', 'PB', 'EB', 'ZB')) {
        If ($size -lt 1024) {
            return [string]::Format("{0:0.##} {1}", $size, $unit)
        }
        $size /= 1024
    }
    return [string]::Format("{0:0.##} YB", $size)
}