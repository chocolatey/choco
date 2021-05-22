# Copyright © 2018 Chocolatey Software, Inc.
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


# For internal use by Install-ChocolateyPath and Uninstall-ChocolateyPath.

function Parse-EnvPathList([string] $rawPathVariableValue) {
  # Using regex (for performance) which correctly splits at each semicolon unless the semicolon is inside double quotes.
  # Unlike semicolons, quotes are not allowed inside paths so there is thankfully no need to unescape them.
  # (Verified using Windows 10’s environment variable editor.)
  # Blank path entries are preserved, such as those caused by a trailing semicolon.
  # This enables reserializing without gratuitous reformatting.
  $paths = $rawPathVariableValue -split '(?<=\G(?:[^;"]|"[^"]*")*);'

  # Remove quotes from each path if they are present
  for ($i = 0; $i -lt $paths.Length; $i++) {
    $path = $paths[$i]
    if ($path.Length -ge 2 -and $path.StartsWith('"', [StringComparison]::Ordinal) -and $path.EndsWith('"', [StringComparison]::Ordinal)) {
      $paths[$i] = $path.Substring(1, $path.Length - 2)
    }
  }

  return $paths
}

function Format-EnvPathList([string[]] $paths) {
  # Don’t mutate the original (externally visible if the argument is not type-coerced),
  # but don’t clone if mutation is unnecessary.
  $createdDefensiveCopy = $false

  # Add quotes to each path if necessary
  for ($i = 0; $i -lt $paths.Length; $i++) {
    $path = $paths[$i]
    if ($path -ne $null -and $path.Contains(';')) {
      if (-not $createdDefensiveCopy) {
        $createdDefensiveCopy = $true
        $paths = $paths.Clone()
      }
      $paths[$i] = '"' + $path + '"'
    }
  }

  return $paths -join ';'
}

function IndexOf-EnvPath([System.Collections.Generic.List[string]] $list, [string] $value) {
  $list.FindIndex({
    $value.Equals($args[0], [StringComparison]::OrdinalIgnoreCase)
  })
}
