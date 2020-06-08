﻿// Copyright © 2017 - 2018 Chocolatey Software, Inc
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
    public class ChocolateyTodoTemplate
    {
        public static string Template =
            @"TODO

1. Determine Package Use: 

 Organization? Internal Use? - You are not subject to distribution 
  rights when you keep everything internal. Put the binaries directly 
  into the tools directory (as long as total nupkg size is under 1GB). 
  When bigger, look to use from a share or download binaries from an 
  internal location. Embedded binaries makes for the most reliable use 
  of Chocolatey. Use `$fileLocation` (`$file`/`$file64`) and 
  `Install-ChocolateyInstallPackage`/`Get-ChocolateyUnzip` in
  tools\chocolateyInstall.ps1.

  You can also choose to download from internal urls, see the next 
  section, but ignore whether you have distribution rights or not, it 
  doesn't apply. Under no circumstances should download from the 
  internet, it is completely unreliable. See 
  https://chocolatey.org/docs/community-packages-disclaimer#organizations
  to understand the limitations of a publicly available repository.

 Community Repository? 
  Have Distribution Rights?
    If you are the software vendor OR the software EXPLICITLY allows
    redistribution and the total nupkg size will be under 200MB, you 
    have the option to embed the binaries directly into the package to 
    provide the most reliable install experience. Put the binaries 
    directly into the tools folder, use `$fileLocation` (`$file`/ 
    `$file64`) and `Install-ChocolateyInstallPackage`/
    `Get-ChocolateyUnzip` in tools\chocolateyInstall.ps1. Additionally,
    fill out the LICENSE and VERIFICATION file (see 3 below and those
    files for specifics).

    NOTE: You can choose to download binaries at runtime, but be sure 
     the download location will remain stable. See the next section.

  Do Not Have Distribution Rights?
    - Note: Packages built this way cannot be 100% reliable, but it's a
      constraint of publicly available packages and there is little 
      that can be done to change that. See
      https://chocolatey.org/docs/community-packages-disclaimer#organizations
      to better understand the limitations of a publicly available 
      repository.
    Download Location is Publicly Available?
      You will need to download the runtime files from their official 
      location at runtime. Use `$url`/`$url64` and 
      `Install-ChocolateyPackage`/`Install-ChocolateyZipPackage` in
      tools\chocolateyInstall.ps1.
    Download Location is Not Publicly Available?
      Stop here, you can't push this to the community repository. You 
      can ask the vendor for permission to embed, then include a PDF of 
      that signed permission directly in the package. Otherwise you 
      will need to seek alternate locations to non-publicly host the 
      package.
    Download Location Is Same For All Versions?
      You still need to point to those urls, but you may wish to set up
      something like Automatic Updater (AU) so that when a new version
      of the software becomes available, the new package version 
      automatically gets pushed up to the community repository. See
      https://chocolatey.org/docs/automatic-packages#automatic-updater-au

2. Determine Package Type:

- Installer Package - contains an installer (everything in template is 
  geared towards this type of package)
- Zip Package - downloads or embeds and unpacks archives, may unpack 
  and run an installer using `Install-ChocolateyInstallPackage` as a 
  secondary step.
- Portable Package - Contains runtime binaries (or unpacks them as a 
  zip package) - cannot require administrative permissions to install 
  or use
- Config Package - sets config like files, registry keys, etc
- Extension Package - Packages that add PowerShell functions to 
  Chocolatey - https://chocolatey.org/docs/how-to-create-extensions
- Template Package - Packages that add templates like this for `choco
  new -t=name` - https://chocolatey.org/docs/how-to-create-custom-package-templates
- Other - there are other types of packages as well, these are the main
  package types seen in the wild

3. Fill out the package contents: 

- tools\chocolateyBeforeModify.ps1 - remove if you have no processes 
  or services to shut down before upgrade/uninstall
- tools\LICENSE.txt / tools\VERIFICATION.txt - Remove if you are not 
  embedding binaries. Keep and fill out if you are embedding binaries 
  in the package AND pushing to the community repository, even if you 
  are the author of software. The file becomes easier to fill out 
  (does not require changes each version) if you are the software 
  vendor. If you are building packages for internal use (organization,
  etc), you don't need these files as you are not subject to
  distribution rights internally.
- tools\chocolateyUninstall.ps1 - remove if autouninstaller can 
  automatically uninstall and you have nothing additional to do during 
  uninstall
- Readme.txt - delete this file once you have read over and used 
  anything you've needed from here
- nuspec - fill this out, then clean out all the comments (you may wish
  to leave the headers for the package vs software metadata)
- tools\chocolateyInstall.ps1 - instructions in next section.

4. ChocolateyInstall.ps1:

- For embedded binaries - use `$fileLocation` (`$file`/`$file64`) and 
  `Install-ChocolateyInstallPackage`/ `Get-ChocolateyUnzip`.
- Downloading binaries at runtime - use `$url`/`$url64` and 
  `Install-ChocolateyPackage` / `Install-ChocolateyZipPackage`.
- Other needs (creating files, setting registry keys), use regular 
  PowerShell to do so or see if there is a function already defined:
  https://chocolatey.org/docs/helpers-reference
- There may also be functions available in extension packages, see
  https://chocolatey.org/packages?q=id%3A.extension for examples and
  availability.
- Clean out the comments and sections you are not using.

5. Test the package to ensure install/uninstall work appropriately. 
 There is a test environment you can use for this - 
 https://github.com/chocolatey/chocolatey-test-environment

6. Learn more about Chocolatey packaging - go through the workshop at
 https://github.com/ferventcoder/chocolatey-workshop
 You will learn about
 - General packaging
 - Customizing package behavior at runtime (package parameters)
 - Extension packages
 - Custom packaging templates
 - Setting up an internal Chocolatey.Server repository
 - Adding and using internal repositories
 - Reporting
 - Advanced packaging techniques when installers are not friendly to
   automation

7. Delete this file.
";
    }
}
