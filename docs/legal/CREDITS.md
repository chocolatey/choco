<!-- TOC -->

- [Committers & Contributors](#committers--contributors)
  - [Committers](#committers)
  - [Chocolatey Community Team](#chocolatey-community-team)
  - [Contributors](#contributors)
  - [Other Contributors](#other-contributors)
- [Third Party Licenses - Development](#third-party-licenses---development)
- [Third Party Licenses - Runtime](#third-party-licenses---runtime)
  - [7-Zip](#7-zip)
  - [AlphaFS](#alphafs)
  - [Checksum](#checksum)
  - [log4net](#log4net)
  - [Microsoft.Web.Xdt](#microsoftwebxdt)
  - [NuGet.Core (modified)](#nugetcore-modified)
  - [Rhino.Licensing (modified)](#rhinolicensing-modified)
  - [Rx (Reactive Extensions)](#rx-reactive-extensions)
  - [Shim Generator (shimgen)](#shim-generator-shimgen)
  - [SimpleInjector](#simpleinjector)

<!-- /TOC -->

## Committers & Contributors
Chocolatey has been the the thoughts, ideas, and work of a large community. While [Rob](https://github.com/ferventcoder) heads up direction and plays a primary role in development, there are several people that have really been a part of making Chocolatey what it is today.


### Committers
These are the committers to Chocolatey/Choco repositories:

 * [Core Development Team](https://github.com/orgs/chocolatey/teams/developers)
   * [Rob Reynolds](https://github.com/ferventcoder) - Creator of Chocolatey, committer, vision, direction, community feed moderator
   * [Gary Ewan Park](https://github.com/gep13) - Committer, Chocolatey GUI, community feed moderator
   * [Matt Wrock](https://github.com/mwrock) - Committer, Creator of BoxStarter, community feed moderator
   * [Rich Siegel](https://github.com/rismoney) - Committer, Creator of Puppet provider
   * [Richard Simpson](https://github.com/RichiCoder1) - created and maintains the new Chocolatey GUI

### Chocolatey Community Team
The Chocolatey Community Team includes the committers and adds these fine folks:

* [Community Package Repository Moderation Team](https://github.com/orgs/chocolatey/teams/community-moderators)
* [Chocolatey Core Community Maintainers Team](https://github.com/orgs/chocolatey/teams/community-maintainers)

### Contributors
 * [choco.exe](https://github.com/chocolatey/choco/graphs/contributors)
 * [Original Chocolatey - POSH choco](https://github.com/chocolatey/chocolatey/graphs/contributors)
 * [Community Package Repository / Chocolatey.org](https://github.com/chocolatey/chocolatey.org/graphs/contributors)

### Other Contributors
**NOTE: NEEDS UPDATED**

 * Nekresh (https://github.com/nekresh) - Contributing code and ideas on direction
 * Chris Ortman (https://github.com/chrisortman) - package contributions and thoughts on where to take it
 * Svein Arne Ackenhausen (https://github.com/acken) - suggestions and thoughts on features and packages
 * Marcel Hoyer - suggestions on making this stuff work without administrative access to a machine
 * Jason Jarrett (https://github.com/staxmanade) - contributing code and ideas

## Third Party Licenses - Development
Choco is built, tested and analyzed with the following fantastic frameworks (in no particular order):

 * ILMerge
 * UppercuT (NAnt)
 * PublishedApplications
 * NuGet.exe

Choco is tested and analyzed with the following rockstar frameworks (in no particular order):

 * bdddoc
 * NUnit
 * Moq
 * TinySpec
 * Should
 * OpenCover
 * ReportGenerator

We would like to credit other super sweet tools/frameworks that aid in the development of choco:

 * ReSharper
 * NuGet Framework

## Third Party Licenses - Runtime
Chocolatey open source uses a number of 3rd party components. Their details are below (order is alphabetical).

<!-- TOC -->

- [7-Zip](#7-zip)
- [AlphaFS](#alphafs)
- [Checksum](#checksum)
- [log4net](#log4net)
- [Microsoft.Web.Xdt](#microsoftwebxdt)
- [NuGet.Core (modified)](#nugetcore-modified)
- [Rhino.Licensing (modified)](#rhinolicensing-modified)
- [Rx (Reactive Extensions)](#rx-reactive-extensions)
- [Shim Generator (shimgen)](#shim-generator-shimgen)
- [SimpleInjector](#simpleinjector)

<!-- /TOC -->

### 7-Zip
Chocolatey uses [7-Zip](http://www.7-zip.org/) for uncompressing archives. [License terms](http://www.7-zip.org/license.txt):

~~~
  7-Zip

  License for use and distribution
  --------------------------------

  7-Zip Copyright (C) 1999-2016 Igor Pavlov.

  Licenses for files are:

    1) 7z.dll: GNU LGPL + unRAR restriction
    2) All other files:  GNU LGPL

  The GNU LGPL + unRAR restriction means that you must follow both
  GNU LGPL rules and unRAR restriction rules.


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


  unRAR restriction
  -----------------

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
  Igor Pavlov

~~~

### AlphaFS
Chocolatey uses [AlphaFS](https://github.com/alphaleonis/AlphaFS) for long file paths. [License terms](https://github.com/alphaleonis/AlphaFS/blob/7e597b58a5109ee820766a176ffa489c1411b6aa/LICENSE.md):

~~~
  The MIT License (MIT)
  =====================

  Copyright (c) 2008-2016 Peter Palotas, Jeffrey Jangli, Alexandr Normuradov

  Permission is hereby granted, free of charge, to any person obtaining a copy of this
  software and associated documentation files (the "Software"), to deal in the Software
  without restriction, including without limitation the rights to use, copy, modify, merge,
  publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to
  whom the Software is furnished to do so, subject to the following conditions:

  The above copyright notice and this permission notice shall be included in all copies or
  substantial portions of the Software.

  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING
  BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
  NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
  DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
~~~

### Checksum
Chocolatey uses [Checksum](https://github.com/ferventcoder/checksum) to determine checksums. [License terms](https://github.com/ferventcoder/checksum/blob/e6f5645610c7bc15084b48f69d4cdb056106f956/LICENSE):

~~~
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

~~~

### log4net
Chocolatey uses [log4net](http://logging.apache.org/log4net/) for logging. [License terms](http://logging.apache.org/log4net/license.html):

~~~
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
~~~

### Microsoft.Web.Xdt
Chocolatey uses [Microsoft.Web.Xdt](https://www.nuget.org/packages/Microsoft.Web.xdt) to perform Xml Document Transformation. It is also a requirement of NuGet.Core. [License terms](https://www.microsoft.com/web/webpi/eula/microsoft_web_xmltransform.htm):

~~~
  MICROSOFT SOFTWARE LICENSE TERMS

  MICROSOFT.WEB.XDT

  These license terms are an agreement between Microsoft Corporation (or based on where you live, one of its affiliates) and you. Please read them. They apply to the software named above, which includes the media on which you received it, if any. The terms also apply to any Microsoft

  ·         updates,

  ·         supplements,

  ·         Internet-based services, and

  ·         support services

  for this software, unless other terms accompany those items. If so, those terms apply.

  By using the software, you accept these terms. If you do not accept them, do not use the software.

  If you comply with these license terms, you have the perpetual rights below.

  1.    INSTALLATION AND USE RIGHTS. You may install and use any number of copies of the software on your devices.

  2.    ADDITIONAL LICENSING REQUIREMENTS AND/OR USE RIGHTS.

  a.    Distributable Code. The software contains code that you are permitted to distribute in programs you develop if you comply with the terms below.

  i.  Right to Use and Distribute.  You may copy and distribute the object code form of Microsoft.Web.XmlTransform.dll file.

  ·         Third Party Distribution. You may permit distributors of your programs to copy and distribute the Distributable Code as part of those programs.

  ii.Distribution Requirements. For any Distributable Code you distribute, you must

  ·         add significant primary functionality to it in your programs;

  ·         require distributors and external end users to agree to terms that protect it at least as much as this agreement;

  ·         display your valid copyright notice on your programs; and

  ·         indemnify, defend, and hold harmless Microsoft from any claims, including attorneys’ fees, related to the distribution or use of your programs.

  iii.   Distribution Restrictions. You may not

  ·         alter any copyright, trademark or patent notice in the Distributable Code;

  ·         use Microsoft’s trademarks in your programs’ names or in a way that suggests your programs come from or are endorsed by Microsoft;

  ·         distribute Distributable Code to run on a platform other than the Windows platform;

  ·         include Distributable Code in malicious, deceptive or unlawful programs; or

  ·         modify or distribute the source code of any Distributable Code so that any part of it becomes subject to an Excluded License. An Excluded License is one that requires, as a condition of use, modification or distribution, that

  ·         the code be disclosed or distributed in source code form; or

  ·         others have the right to modify it.

  3.    SCOPE OF LICENSE. The software is licensed, not sold. This agreement only gives you some rights to use the software. Microsoft reserves all other rights. Unless applicable law gives you more rights despite this limitation, you may use the software only as expressly permitted in this agreement. In doing so, you must comply with any technical limitations in the software that only allow you to use it in certain ways. You may not

  ·         work around any technical limitations in the software;

  ·         reverse engineer, decompile or disassemble the software, except and only to the extent that applicable law expressly permits, despite this limitation;

  ·         make more copies of the software than specified in this agreement or allowed by applicable law, despite this limitation;

  ·         publish the software for others to copy;

  ·         rent, lease or lend the software; or

  ·         transfer the software or this agreement to any third party.

  4.    BACKUP COPY. You may make one backup copy of the software. You may use it only to reinstall the software.

  5.    DOCUMENTATION. Any person that has valid access to your computer or internal network may copy and use the documentation for your internal, reference purposes.

  6.    EXPORT RESTRICTIONS. The software is subject to United States export laws and regulations. You must comply with all domestic and international export laws and regulations that apply to the software. These laws include restrictions on destinations, end users and end use. For additional information, see www.microsoft.com/exporting.

  7.    SUPPORT SERVICES. Because this software is “as is,” we may not provide support services for it.

  8.    ENTIRE AGREEMENT. This agreement, and the terms for supplements, updates, Internet-based services and support services that you use, are the entire agreement for the software and support services.

  9.    APPLICABLE LAW.

  a.    United States. If you acquired the software in the United States, Washington state law governs the interpretation of this agreement and applies to claims for breach of it, regardless of conflict of laws principles. The laws of the state where you live govern all other claims, including claims under state consumer protection laws, unfair competition laws, and in tort.

  b.    Outside the United States. If you acquired the software in any other country, the laws of that country apply.

  10.  LEGAL EFFECT. This agreement describes certain legal rights. You may have other rights under the laws of your country. You may also have rights with respect to the party from whom you acquired the software. This agreement does not change your rights under the laws of your country if the laws of your country do not permit it to do so.

  11.  DISCLAIMER OF WARRANTY. The software is licensed “as-is.” You bear the risk of using it. Microsoft gives no express warranties, guarantees or conditions. You may have additional consumer rights or statutory guarantees under your local laws which this agreement cannot change. To the extent permitted under your local laws, Microsoft excludes the implied warranties of merchantability, fitness for a particular purpose and non-infringement.

  FOR AUSTRALIA – You have statutory guarantees under the Australian Consumer Law and nothing in these terms is intended to affect those rights.

  12.  LIMITATION ON AND EXCLUSION OF REMEDIES AND DAMAGES. You can recover from Microsoft and its suppliers only direct damages up to U.S. $5.00. You cannot recover any other damages, including consequential, lost profits, special, indirect or incidental damages.

  This limitation applies to

  ·         anything related to the software, services, content (including code) on third party Internet sites, or third party programs; and

  ·         claims for breach of contract, breach of warranty, guarantee or condition, strict liability, negligence, or other tort to the extent permitted by applicable law.

  It also applies even if Microsoft knew or should have known about the possibility of the damages. The above limitation or exclusion may not apply to you because your country may not allow the exclusion or limitation of incidental, consequential or other damages.
~~~

### NuGet.Core (modified)
Chocolatey uses [NuGet.Core](https://github.com/NuGet/NuGet2) [(modified)](https://github.com/chocolatey/nuget-chocolatey) to work with packaging. [License terms](https://github.com/NuGet/NuGet2/blob/c3d1027a51b31fd0c41e9abbe90810cf1c924c9f/COPYRIGHT.txt):

~~~
  Copyright 2010-2014 Outercurve Foundation

  Licensed under the Apache License, Version 2.0 (the "License");
  you may not use this file except in compliance with the License.
  You may obtain a copy of the License at

     http://www.apache.org/licenses/LICENSE-2.0

  Unless required by applicable law or agreed to in writing, software
  distributed under the License is distributed on an "AS IS" BASIS,
  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
  See the License for the specific language governing permissions and
  limitations under the License.
~~~

### Rhino.Licensing (modified)
Chocolatey uses [Rhino.Licensing](https://github.com/ayende/rhino-licensing) [(modified)](https://github.com/ferventcoder/rhino-licensing) to work with licensing. [License terms](https://github.com/ayende/rhino-licensing/blob/1fc90c984b0c3012465a73afae0a53492c969eb5/license.txt):

~~~
  Copyright (c) 2005 - 2009 Ayende Rahien (ayende@ayende.com)
  All rights reserved.

  Redistribution and use in source and binary forms, with or without modification,
  are permitted provided that the following conditions are met:

      * Redistributions of source code must retain the above copyright notice,
      this list of conditions and the following disclaimer.
      * Redistributions in binary form must reproduce the above copyright notice,
      this list of conditions and the following disclaimer in the documentation
      and/or other materials provided with the distribution.
      * Neither the name of Ayende Rahien nor the names of its
      contributors may be used to endorse or promote products derived from this
      software without specific prior written permission.

  THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
  ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
  WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
  DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE
  FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
  DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
  SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
  CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
  OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF
  THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
~~~

### Rx (Reactive Extensions)
Chocolatey uses [Rx](http://reactivex.io/) for schedules and internal messaging. [License terms](https://github.com/Reactive-Extensions/Rx.NET/blob/5003248b99f8bf4afc2d4f7570b5789cedda9155/LICENSE):

~~~
  Copyright (c) .NET Foundation and Contributors
  All Rights Reserved

  Licensed under the Apache License, Version 2.0 (the "License"); you
  may not use this file except in compliance with the License. You may
  obtain a copy of the License at

  http://www.apache.org/licenses/LICENSE-2.0

  Unless required by applicable law or agreed to in writing, software
  distributed under the License is distributed on an "AS IS" BASIS,
  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or
  implied. See the License for the specific language governing permissions
  and limitations under the License.
~~~

### Shim Generator (shimgen)
Chocolatey uses [shimgen](https://github.com/chocolatey/shimgen) to generate shim executables that call the original binaries. [License terms](https://github.com/chocolatey/choco/blob/782a1cd228df548661e6c4eb5bb49b347025f85a/src/chocolatey.resources/tools/shimgen.license.txt):

~~~
  Shim Generator - shimgen.exe
  Copyright (C) 2013 - 2017 RealDimensions Software, LLC ("RDS")
  ===================================================================
  Grant of License
  ===================================================================
  You may use Shim Generator ("shimgen.exe") only with the official Chocolatey
  client. The use of shimgen.exe for any other reason is strictly prohibited.

  If you would like to use this software for any other reason, you must obtain a
  personal or commercial license to do so. To do that you must contact RDS at
  ferventcoder.com.

  This software is not free to distribute apart from the Chocolatey client. If you
  would like to distribute this software outside of use through Chocolatey, you
  must receive written permission from the software owner.

  ===================================================================
  End-User License Agreement
  ===================================================================
  EULA - Shim Generator

  IMPORTANT- READ CAREFULLY: This RealDimensions Software ("RDS") End-User License
  Agreement ("EULA") is a legal agreement between you ("END USER") and RDS for all
  RDS products, controls, source code, demos, intermediate files, media, printed
  materials, and "online" or electronic documentation ("SOFTWARE PRODUCT(S)")
  contained with this distribution.

  RDS grants to END USER as an individual, a personal, nonexclusive license to
  install and use the SOFTWARE PRODUCT(S) for the sole purpose of use with the
  official Chocolatey client. By installing, copying, or otherwise using the
  SOFTWARE PRODUCT(S), END USER agrees to be bound by the terms of this EULA. If
  END USER does not agree to any part of the terms of this EULA, DO NOT INSTALL,
  USE, OR EVALUATE, ANY PART, FILE OR PORTION OF THE SOFTWARE PRODUCT(S).

  ALL SOFTWARE PRODUCT(S) are licensed not sold. If END USER is an individual,
  END USER must acquire an individual license for the SOFTWARE PRODUCT(S) from RDS
  or its authorized resellers. If END USER is an entity, END USER must acquire an
  individual license for each machine running the SOFTWARE PRODUCT(S) within your
  organization from RDS or its authorized resellers. Both Virtual and Physical
  Machines running the SOFTWARE PRODUCT(S) must be counted in the SOFTWARE
  PRODUCT(S) licenses quantity of the organization.

  ===================================================================
  Commercial / Personal Licensing
  ===================================================================
  Shim Generator (shimgen.exe) is also offered under personal and commercial
  licenses. You can learn more about this option by contacting RDS at
  http://ferventcoder.com
~~~

### SimpleInjector
Chocolatey uses [SimpleInjector](https://simpleinjector.org/) for IoC containers. [License Terms](https://github.com/simpleinjector/SimpleInjector/blob/0687195a7691363d4b4918e36b5e4d708e88253c/licence.txt):

~~~
  Copyright (c) 2013 - 2017 Simple Injector Contributors

  Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated
  documentation files (the "Software"), to deal in the Software without restriction, including without
  limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the
  Software, and to permit persons to whom the Software is furnished to do so, subject to the following
  conditions:

  The above copyright notice and this permission notice shall be included in all copies or substantial portions
  of the Software.

  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED
  TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL
  THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF
  CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
  DEALINGS IN THE SOFTWARE.
~~~
