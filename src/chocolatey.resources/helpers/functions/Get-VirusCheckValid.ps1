# Copyright 2011 - Present RealDimensions Software, LLC & original authors/contributors from https://github.com/chocolatey/chocolatey
# 
# Licensed under the Apache License, Version 2.0 (the "License");
# you may not use this file except in compliance with the License.
# You may obtain a copy of the License at
# 
#     http://www.apache.org/licenses/LICENSE-2.0
# 
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.

function Get-VirusCheckValid {
param(
  [string] $location,
  [string] $file = ''
)
  Write-Debug "Running 'Get-VirusCheckValid' with location:`'$location`', file: `'$file`'";

  Write-Debug "Right now there is no virus checking built in."
  #if ($settings:virusCheck) {

  #}

  #if virus check is invalid, throw here
}
