<img alt="Chocolatey logo" width="260" style="margin-right: 1rem;" src="https://img.chocolatey.org/logos/chocolatey.png"> <img alt="Chocolatey icon logo" width="200" src="https://img.chocolatey.org/logos/chocolatey-icon.png">

# Chocolatey CLI / Chocolatey.Lib Third Party Licenses

---

Chocolatey uses a number of 3rd party components. Their details are below.

* [Open Source License Types (Reference)](#open-source-license-types-reference)
  * [Apache v2.0 License](#apache-v20-license)
  * [BSD-3-Clause](#bsd-3-clause)
  * [MIT License](#mit-license)
* [Chocolatey Software Component License](#chocolatey-software-component-licenses)
  * [Chocolatey Open Source](#chocolatey-open-source)
* [Chocolatey CLI / Chocolatey.Lib](#chocolatey-cli--chocolateylib)
  * [Apache v2.0 License](#apache-v20-license-1)
    * [Checksum@0.3.1](#checksum031)
    * [Chocolatey.NuGet.Client@3.4.2](#chocolateynugetclient342)
    * [log4net@rel/2.0.12](#log4netrel2012)
    * [Microsoft.Web.Xdt@3.1.0](#microsoftwebxdt310)
  * [BSD-3-Clause](#bsd-3-clause-1)
    * [Rhino.Licensing@1.4.1](#rhinolicensing141)
  * [MIT License](#mit-license-1)
    * [AlphaFS@2.1.3](#alphafs213)
    * [Microsoft.Bcl.HashCode@1.1.1](#microsoftbclhashcode111)
    * [Newtonsoft.Json@13.0.1](#newtonsoftjson1301)
    * [SimpleInjector@2.8.3](#simpleinjector283)
    * [System.Reactive@rxnet-v5.0.0](#systemreactiverxnet-v500)
    * [System.Runtime.CompilerServices.Unsafe@4.5.3](#systemruntimecompilerservicesunsafe453)
    * [System.Threading.Tasks.Extensions@4.5.4](#systemthreadingtasksextensions454)
  * [Other](#other)
    * [7-Zip@24.08](#7-zip2408)
    * [Shim Generator (shimgen)@2.0.0](#shim-generator-\(shimgen\)200)

## Open Source License Types (Reference)

There are some regularly used open source license types - to reduce the sheer size of this document, we will provide a reference to them here. Each particular component will link directly to the actual license or notice file.

### Apache v2.0 License

The [Apache v2.0 License](https://www.apache.org/licenses/LICENSE-2.0) has the following terms:

```text
                              Apache License
                        Version 2.0, January 2004
                     http://www.apache.org/licenses/

TERMS AND CONDITIONS FOR USE, REPRODUCTION, AND DISTRIBUTION

1. Definitions.

  "License" shall mean the terms and conditions for use, reproduction,
  and distribution as defined by Sections 1 through 9 of this document.

  "Licensor" shall mean the copyright owner or entity authorized by
  the copyright owner that is granting the License.

  "Legal Entity" shall mean the union of the acting entity and all
  other entities that control, are controlled by, or are under common
  control with that entity. For the purposes of this definition,
  "control" means (i) the power, direct or indirect, to cause the
  direction or management of such entity, whether by contract or
  otherwise, or (ii) ownership of fifty percent (50%) or more of the
  outstanding shares, or (iii) beneficial ownership of such entity.

  "You" (or "Your") shall mean an individual or Legal Entity
  exercising permissions granted by this License.

  "Source" form shall mean the preferred form for making modifications,
  including but not limited to software source code, documentation
  source, and configuration files.

  "Object" form shall mean any form resulting from mechanical
  transformation or translation of a Source form, including but
  not limited to compiled object code, generated documentation,
  and conversions to other media types.

  "Work" shall mean the work of authorship, whether in Source or
  Object form, made available under the License, as indicated by a
  copyright notice that is included in or attached to the work
  (an example is provided in the Appendix below).

  "Derivative Works" shall mean any work, whether in Source or Object
  form, that is based on (or derived from) the Work and for which the
  editorial revisions, annotations, elaborations, or other modifications
  represent, as a whole, an original work of authorship. For the purposes
  of this License, Derivative Works shall not include works that remain
  separable from, or merely link (or bind by name) to the interfaces of,
  the Work and Derivative Works thereof.

  "Contribution" shall mean any work of authorship, including
  the original version of the Work and any modifications or additions
  to that Work or Derivative Works thereof, that is intentionally
  submitted to Licensor for inclusion in the Work by the copyright owner
  or by an individual or Legal Entity authorized to submit on behalf of
  the copyright owner. For the purposes of this definition, "submitted"
  means any form of electronic, verbal, or written communication sent
  to the Licensor or its representatives, including but not limited to
  communication on electronic mailing lists, source code control systems,
  and issue tracking systems that are managed by, or on behalf of, the
  Licensor for the purpose of discussing and improving the Work, but
  excluding communication that is conspicuously marked or otherwise
  designated in writing by the copyright owner as "Not a Contribution."

  "Contributor" shall mean Licensor and any individual or Legal Entity
  on behalf of whom a Contribution has been received by Licensor and
  subsequently incorporated within the Work.

2. Grant of Copyright License. Subject to the terms and conditions of
  this License, each Contributor hereby grants to You a perpetual,
  worldwide, non-exclusive, no-charge, royalty-free, irrevocable
  copyright license to reproduce, prepare Derivative Works of,
  publicly display, publicly perform, sublicense, and distribute the
  Work and such Derivative Works in Source or Object form.

3. Grant of Patent License. Subject to the terms and conditions of
  this License, each Contributor hereby grants to You a perpetual,
  worldwide, non-exclusive, no-charge, royalty-free, irrevocable
  (except as stated in this section) patent license to make, have made,
  use, offer to sell, sell, import, and otherwise transfer the Work,
  where such license applies only to those patent claims licensable
  by such Contributor that are necessarily infringed by their
  Contribution(s) alone or by combination of their Contribution(s)
  with the Work to which such Contribution(s) was submitted. If You
  institute patent litigation against any entity (including a
  cross-claim or counterclaim in a lawsuit) alleging that the Work
  or a Contribution incorporated within the Work constitutes direct
  or contributory patent infringement, then any patent licenses
  granted to You under this License for that Work shall terminate
  as of the date such litigation is filed.

4. Redistribution. You may reproduce and distribute copies of the
  Work or Derivative Works thereof in any medium, with or without
  modifications, and in Source or Object form, provided that You
  meet the following conditions:

  (a) You must give any other recipients of the Work or
      Derivative Works a copy of this License; and

  (b) You must cause any modified files to carry prominent notices
      stating that You changed the files; and

  (c) You must retain, in the Source form of any Derivative Works
      that You distribute, all copyright, patent, trademark, and
      attribution notices from the Source form of the Work,
      excluding those notices that do not pertain to any part of
      the Derivative Works; and

  (d) If the Work includes a "NOTICE" text file as part of its
      distribution, then any Derivative Works that You distribute must
      include a readable copy of the attribution notices contained
      within such NOTICE file, excluding those notices that do not
      pertain to any part of the Derivative Works, in at least one
      of the following places: within a NOTICE text file distributed
      as part of the Derivative Works; within the Source form or
      documentation, if provided along with the Derivative Works; or,
      within a display generated by the Derivative Works, if and
      wherever such third-party notices normally appear. The contents
      of the NOTICE file are for informational purposes only and
      do not modify the License. You may add Your own attribution
      notices within Derivative Works that You distribute, alongside
      or as an addendum to the NOTICE text from the Work, provided
      that such additional attribution notices cannot be construed
      as modifying the License.

  You may add Your own copyright statement to Your modifications and
  may provide additional or different license terms and conditions
  for use, reproduction, or distribution of Your modifications, or
  for any such Derivative Works as a whole, provided Your use,
  reproduction, and distribution of the Work otherwise complies with
  the conditions stated in this License.

5. Submission of Contributions. Unless You explicitly state otherwise,
  any Contribution intentionally submitted for inclusion in the Work
  by You to the Licensor shall be under the terms and conditions of
  this License, without any additional terms or conditions.
  Notwithstanding the above, nothing herein shall supersede or modify
  the terms of any separate license agreement you may have executed
  with Licensor regarding such Contributions.

6. Trademarks. This License does not grant permission to use the trade
  names, trademarks, service marks, or product names of the Licensor,
  except as required for reasonable and customary use in describing the
  origin of the Work and reproducing the content of the NOTICE file.

7. Disclaimer of Warranty. Unless required by applicable law or
  agreed to in writing, Licensor provides the Work (and each
  Contributor provides its Contributions) on an "AS IS" BASIS,
  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or
  implied, including, without limitation, any warranties or conditions
  of TITLE, NON-INFRINGEMENT, MERCHANTABILITY, or FITNESS FOR A
  PARTICULAR PURPOSE. You are solely responsible for determining the
  appropriateness of using or redistributing the Work and assume any
  risks associated with Your exercise of permissions under this License.

8. Limitation of Liability. In no event and under no legal theory,
  whether in tort (including negligence), contract, or otherwise,
  unless required by applicable law (such as deliberate and grossly
  negligent acts) or agreed to in writing, shall any Contributor be
  liable to You for damages, including any direct, indirect, special,
  incidental, or consequential damages of any character arising as a
  result of this License or out of the use or inability to use the
  Work (including but not limited to damages for loss of goodwill,
  work stoppage, computer failure or malfunction, or any and all
  other commercial damages or losses), even if such Contributor
  has been advised of the possibility of such damages.

9. Accepting Warranty or Additional Liability. While redistributing
  the Work or Derivative Works thereof, You may choose to offer,
  and charge a fee for, acceptance of support, warranty, indemnity,
  or other liability obligations and/or rights consistent with this
  License. However, in accepting such obligations, You may act only
  on Your own behalf and on Your sole responsibility, not on behalf
  of any other Contributor, and only if You agree to indemnify,
  defend, and hold each Contributor harmless for any liability
  incurred by, or claims asserted against, such Contributor by reason
  of your accepting any such warranty or additional liability.

END OF TERMS AND CONDITIONS
```

### BSD-3-Clause

The [BSD 3-Clause License](https://opensource.org/license/bsd-3-clause) has also been called the "New BSD License", "Revised BSD License", or "Modified BSD License." It has the following terms:

```text
Copyright <YEAR> <COPYRIGHT HOLDER>

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

1. Redistributions of source code must retain the above copyright notice,
   this list of conditions and the following disclaimer.

2. Redistributions in binary form must reproduce the above copyright notice,
   this list of conditions and the following disclaimer in the documentation
   and/or other materials provided with the distribution.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS “AS IS”
AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE
LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
POSSIBILITY OF SUCH DAMAGE.
```

### MIT License

The [MIT License](https://mit-license.org/) has the following terms:

```text
Copyright © <YEAR> <COPYRIGHT HOLDER>

Permission is hereby granted, free of charge, to any person obtaining
a copy of this software and associated documentation files (the
“Software”), to deal in the Software without restriction, including
without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to
permit persons to whom the Software is furnished to do so, subject
to the following conditions:

The above copyright notice and this permission notice shall be
included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS
BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN
ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```

## Chocolatey Software Component Licenses

### Chocolatey Open Source

Chocolatey Open Source components fall under the [Apache v2.0 license](https://www.apache.org/licenses/LICENSE-2.0).

```text
https://www.apache.org/licenses/LICENSE-2.0
```

* [Chocolatey CLI / Chocolatey.Lib](https://github.com/chocolatey/choco) - [License terms](https://github.com/chocolatey/choco/blob/master/LICENSE)

## Chocolatey CLI / Chocolatey.Lib

### Apache v2.0 License

#### Checksum@0.3.1

[Checksum](https://github.com/chocolatey/checksum) - [License terms.](https://github.com/chocolatey/checksum/blob/89a1b39cbc05624aacefae416b3e954475838ecb/LICENS)

#### Chocolatey.NuGet.Client@3.4.2

[Chocolatey.NuGet.Client](https://github.com/NuGet/NuGet.Client) [(modified)](https://github.com/chocolatey/NuGet.Client) - [License terms.](https://github.com/NuGet/NuGet.Client/blob/72f9f2b2eab28c9d91a22065c55aa7702abf7e01/LICENSE.txt)

#### log4net@rel/2.0.12

[log4net](https://github.com/apache/logging-log4net) - [License terms.](https://github.com/apache/logging-log4net/blob/dbad144815221ffe4ed85efa73134583253dc75b/LICENSE)

#### Microsoft.Web.Xdt@3.1.0

[Microsoft.Web.Xdt](https://www.nuget.org/packages/Microsoft.Web.Xdt/3.1.0) - [License terms.](https://licenses.nuget.org/Apache-2.0)

### BSD-3-Clause

#### Rhino.Licensing@1.4.1

[Rhino.Licensing](https://github.com/ayende/rhino-licensing) [(modified)](https://github.com/chocolatey/rhino-licensing) - [License terms.](https://github.com/ayende/rhino-licensing/blob/1fc90c984b0c3012465a73afae0a53492c969eb5/license.txt)

### MIT License

#### AlphaFS@2.1.3

[AlphaFS](https://github.com/alphaleonis/AlphaFS) - [License terms.](https://github.com/alphaleonis/AlphaFS/blob/c63d46894e08d5a4e993b35131051f13203c3321/LICENSE.md)

#### Microsoft.Bcl.HashCode@1.1.1

[Microsoft.Bcl.HashCode](https://github.com/dotnet/corefx) - [License terms.](https://github.com/dotnet/corefx/blob/bdaf5f50f035df0aa98bd69b400b5d1dcff6a7b0/LICENSE)

#### Newtonsoft.Json@13.0.1

[Newtonsoft.Json](https://github.com/JamesNK/Newtonsoft.Json) - [License terms.](https://github.com/JamesNK/Newtonsoft.Json/blob/ae9fe44e1323e91bcbd185ca1a14099fba7c021f/LICENSE.md)

#### SimpleInjector@2.8.3

[SimpleInjector](https://simpleinjector.org/) - [License terms.](https://github.com/simpleinjector/SimpleInjector/blob/0687195a7691363d4b4918e36b5e4d708e88253c/licence.txt)

#### System.Reactive@rxnet-v5.0.0

[System.Reactive](https://github.com/dotnet/reactive) - [License terms.](https://github.com/dotnet/reactive/blob/8a2df0b7850a373b3bad68b43b3839d1cb47eb2e/LICENSE)

#### System.Runtime.CompilerServices.Unsafe@4.5.3

[System.Runtime.CompilerServices.Unsafe](https://www.nuget.org/packages/System.Runtime.CompilerServices.Unsafe/4.5.3) - [License terms.](https://github.com/dotnet/corefx/blob/master/LICENSE.TXT)

#### System.Threading.Tasks.Extensions@4.5.4

[System.Threading.Tasks.Extensions](https://www.nuget.org/packages/System.Threading.Tasks.Extensions/4.5.4) - [License terms.](https://github.com/dotnet/corefx/blob/master/LICENSE.TXT)

### Other

#### 7-Zip@24.08

[7-Zip](https://www.7-zip.org/) - [License terms.](https://www.7-zip.org/license.txt)

```text
7-Zip Copyright (C) 1999-2025 Igor Pavlov.

  The licenses for files are:

    - 7z.dll:
         - The "GNU LGPL" as main license for most of the code
         - The "GNU LGPL" with "unRAR license restriction" for some code
         - The "BSD 3-clause License" for some code
         - The "BSD 2-clause License" for some code
    - All other files: the "GNU LGPL".

  Redistributions in binary form must reproduce related license information from this file.

  Note:
    You can use 7-Zip on any computer, including a computer in a commercial
    organization. You don't need to register or pay for 7-Zip.


GNU LGPL information
--------------------

    This library is free software; you can redistribute it and/or
    modify it under the terms of the GNU Lesser General Public
    License as published by the Free Software Foundation; either
    version 2.1 of the License, or (at your option) any later version.

    This library is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
    Lesser General Public License for more details.

    You can receive a copy of the GNU Lesser General Public License from
    http://www.gnu.org/




BSD 3-clause License in 7-Zip code
----------------------------------

  The "BSD 3-clause License" is used for the following code in 7z.dll
    1) LZFSE data decompression.
       That code was derived from the code in the "LZFSE compression library" developed by Apple Inc,
       that also uses the "BSD 3-clause License".
    2) ZSTD data decompression.
       that code was developed using original zstd decoder code as reference code.
       The original zstd decoder code was developed by Facebook Inc,
       that also uses the "BSD 3-clause License".

  Copyright (c) 2015-2016, Apple Inc. All rights reserved.
  Copyright (c) Facebook, Inc. All rights reserved.
  Copyright (c) 2023-2025 Igor Pavlov.

Text of the "BSD 3-clause License"
----------------------------------

Redistribution and use in source and binary forms, with or without modification,
are permitted provided that the following conditions are met:

1. Redistributions of source code must retain the above copyright notice, this
   list of conditions and the following disclaimer.

2. Redistributions in binary form must reproduce the above copyright notice,
   this list of conditions and the following disclaimer in the documentation
   and/or other materials provided with the distribution.

3. Neither the name of the copyright holder nor the names of its contributors may
   be used to endorse or promote products derived from this software without
   specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR
ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON
ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

---




BSD 2-clause License in 7-Zip code
----------------------------------

  The "BSD 2-clause License" is used for the XXH64 code in 7-Zip.

  XXH64 code in 7-Zip was derived from the original XXH64 code developed by Yann Collet.

  Copyright (c) 2012-2021 Yann Collet.
  Copyright (c) 2023-2025 Igor Pavlov.

Text of the "BSD 2-clause License"
----------------------------------

Redistribution and use in source and binary forms, with or without modification,
are permitted provided that the following conditions are met:

1. Redistributions of source code must retain the above copyright notice, this
   list of conditions and the following disclaimer.

2. Redistributions in binary form must reproduce the above copyright notice,
   this list of conditions and the following disclaimer in the documentation
   and/or other materials provided with the distribution.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR
ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON
ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

---




unRAR license restriction
-------------------------

The decompression engine for RAR archives was developed using source
code of unRAR program.
All copyrights to original unRAR code are owned by Alexander Roshal.

The license for original unRAR code has the following restriction:

  The unRAR sources cannot be used to re-create the RAR compression algorithm,
  which is proprietary. Distribution of modified unRAR sources in separate form
  or as a part of other software is permitted, provided that it is clearly
  stated in the documentation and source comments that the code may
  not be used to develop a RAR (WinRAR) compatible archiver.

--
```

#### Shim Generator (shimgen)@2.0.0

[Shim Generator (shimgen)](https://github.com/chocolatey/shimgen) - [License terms.](https://github.com/chocolatey/choco/blob/d25f993696b4d665ee2dc94ceb0937a2ed5698eb/src/chocolatey.resources/tools/shimgen.license.txt)

```text
Shim Generator - shimgen.exe
Copyright (C) 2017 - Present Chocolatey Software, Inc ("CHOCOLATEY")
Copyright (C) 2013 - 2017 RealDimensions Software, LLC ("RDS")
===================================================================
Grant of License
===================================================================
ATTENTION: Shim Generator ("shimgen.exe") is a closed source application with
a proprietary license and its use is strictly limited to the terms of this 
license agreement.

RealDimensions Software, LLC ("RDS") grants Chocolatey Software, Inc a revocable, 
non-exclusive license to distribute and use shimgen.exe with the official 
Chocolatey client (https://chocolatey.org). This license file must be stored in 
Chocolatey source next to shimgen.exe and distributed with every copy of 
shimgen.exe. The distribution or use of shimgen.exe outside of these terms 
without the express written permission of RDS is strictly prohibited.

While the source for shimgen.exe is closed source, the shims have reference 
source at https://github.com/chocolatey/shimgen/tree/master/shim.

===================================================================
End-User License Agreement
===================================================================
EULA - Shim Generator

IMPORTANT- READ CAREFULLY: This RealDimensions Software, LLC ("RDS") End-User License
Agreement ("EULA") is a legal agreement between you ("END USER") and RDS for all 
RDS products, controls, source code, demos, intermediate files, media, printed 
materials, and "online" or electronic documentation (collectively "SOFTWARE 
PRODUCT(S)") contained with this distribution.

RDS grants to you as an individual or entity, a personal, non-exclusive license 
to install and use the SOFTWARE PRODUCT(S) for the sole purpose of use with the 
official Chocolatey client. By installing, copying, or otherwise using the 
SOFTWARE PRODUCT(S), END USER agrees to be bound by the terms of this EULA. If 
END USER does not agree to any part of the terms of this EULA, DO NOT INSTALL, 
USE, OR EVALUATE, ANY PART, FILE OR PORTION OF THE SOFTWARE PRODUCT(S).

In no event shall RDS be liable to END USER for damages, including any direct, 
indirect, special, incidental, or consequential damages of any character arising
as a result of the use or inability to use the SOFTWARE PRODUCT(S) (including 
but not limited to damages for loss of goodwill, work stoppage, computer failure
or malfunction, or any and all other commercial damages or losses).

The liability of RDS to END USER for any reason and upon any cause of action 
related to the performance of the work under this agreement whether in tort or 
in contract or otherwise shall be limited to the amount paid by the END USER to 
RDS pursuant to this agreement or as determined by written agreement signed 
by both RDS and END USER.

ALL SOFTWARE PRODUCT(S) are licensed not sold. If you are an individual, you 
must acquire an individual license for the SOFTWARE PRODUCT(S) from RDS or its 
authorized resellers. If you are an entity, you must acquire an individual license 
for each machine running the SOFTWARE PRODUCT(S) within your organization from RDS 
or its authorized resellers. Both virtual and physical machines running the SOFTWARE 
PRODUCT(S) must be counted in the SOFTWARE PRODUCT(S) licenses quantity of the 
organization.

===================================================================
Commercial / Personal Licensing
===================================================================
Shim Generator ("shimgen.exe") is also offered under personal and commercial 
licenses. You can learn more by contacting Chocolatey at https://chocolatey.org/contact.
```