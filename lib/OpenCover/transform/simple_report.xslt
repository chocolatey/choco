<?xml version="1.0" encoding="UTF-8"?>
<!-- 
This report was provided by pawan52tiwari (https://github.com/pawan52tiwari)
see https://github.com/sawilde/opencover/issues/93

sample usage:
powershell -noexit -file transform.ps1 -xsl simple_report.xslt -xml ..\results\opencovertests.xml -output ..\results\simple_output.html
-->
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" >
  <xsl:output method="html"/>
  <xsl:variable name="covered.lines" select="count(/CoverageSession/Modules/Module/Classes/Class/Methods/Method/SequencePoints/SequencePoint[@vc > 0])" />
  <xsl:variable name="uncovered.lines" select="count(/CoverageSession/Modules/Module/Classes/Class/Methods/Method/SequencePoints/SequencePoint[@vc = 0])" />
  <xsl:variable name="coverable.Lines" select="count(/CoverageSession/Modules/Module/Classes/Class/Methods/Method/SequencePoints/SequencePoint)" />
  <xsl:template match="/">
  <html><body>
    <h2 class="sectionheader">Code Coverage Report</h2>
    <table class="overview">
      <colgroup>
        <col width="130" />
        <col />
      </colgroup>
      <tr>
        <td class="sectionheader">
          Generated on:
        </td>
        <td>
          <xsl:value-of select="/@date"/>
        </td>
      </tr>
      <tr>
        <td class="sectionheader">
          Parser:
        </td>
        <td>
          Pawan Tiwari's Parser
        </td>
      </tr>
      <tr>
        <td class="sectionheader">
          Assemblies:
        </td>
        <td>
          <xsl:value-of select="count(/CoverageSession/Modules/Module/ModuleName)"></xsl:value-of>
        </td>
      </tr>
      <tr>
        <td class="sectionheader">
          Files:
        </td>
        <td>
          <xsl:value-of select="count(/CoverageSession/Modules/Module/Files/File)"/>
        </td>
      </tr>
      <tr>
        <td class="sectionheader">
          Coverage:
        </td>
        <td>
          <xsl:value-of select="$covered.lines div ($uncovered.lines + $covered.lines) * 100"/>%
        </td>
      </tr>
      <tr>
        <td class="sectionheader">
          Covered lines:
        </td>
        <td>
          <xsl:value-of select="$covered.lines"/>
        </td>
      </tr>
      <tr>
        <td class="sectionheader">
          UnCovered lines:
        </td>
        <td>
          <xsl:value-of select="$uncovered.lines"/>
        </td>
      </tr>
      <tr>
        <td class="sectionheader">
          Coverable lines:
        </td>
        <td>
          <xsl:value-of select="$coverable.Lines"/>
        </td>
      </tr>
      <tr>
        <td class="sectionheader">
          Total lines:
        </td>
        <td>
          Yet To be discovered

        </td>
      </tr>
    </table>
    <h2 class="sectionheader">
      Assemblies
    </h2>
    <p class="toggleClasses">
      <a id="collapseAllClasses" style="text-decoration: none;color:red;font-size:10px" href="#">Collapse all classes</a> | <a id="expandAllClasses" style="text-decoration: none;color:red;font-size:10px"
                href="#">Expand all classes</a>
    </p>
    <table class="overview">
      <colgroup>
        <col />
        <col width="60" />
        <col width="105" />
      </colgroup>
      <xsl:for-each select="/CoverageSession/Modules/Module">
        <xsl:sort select="ModuleName" order="ascending"/>
        <xsl:sort select="ModuleName"/>
        <xsl:variable name="ModulenameVariable" select="ModuleName"></xsl:variable>
        <xsl:variable name="FileLocationLink" select="."></xsl:variable>
        <tr class="expanded">
          <th>
            <a href="#" class="toggleClassesInAssembly" style="text-decoration: none;color:red;font-size:10px" title="Collapse/Expand classes"></a>
            <xsl:value-of select="ModuleName"/>
            <a href="#" class="toggleAssemblyDetails" style="text-decoration: none;color:red;font-size:10px" title="Show details of assembly">Details</a>
            <div class="detailspopup">
              <table class="overview">
                <colgroup>
                  <col width="130" />
                  <col />
                </colgroup>
                <tr>
                  <td class="sectionheader">
                    Classes:
                  </td>
                  <td>
                    <xsl:value-of select="count(Classes/Class/FullName[not(contains(text(),'&lt;'))])"></xsl:value-of>
                  </td>
                </tr>
                <tr>
                  <td class="sectionheader">
                    Covered lines:
                  </td>
                  <td>
                    <xsl:value-of  select="count(Classes/Class/Methods/Method/SequencePoints/SequencePoint[@vc > 0])"></xsl:value-of>
                  </td>
                </tr>
                <tr>
                  <td class="sectionheader">
                    Coverable lines:
                  </td>
                  <td>
                    <xsl:value-of select="count(Classes/Class/Methods/Method/SequencePoints/SequencePoint)" />
                  </td>
                </tr>
                <tr>
                  <td class="sectionheader">
                    Coverage:
                  </td>
                  <td>
                    <xsl:choose>
                      <xsl:when test="(count(Classes/Class/Methods/Method/SequencePoints/SequencePoint[@vc = 0]) + count(Classes/Class/Methods/Method/SequencePoints/SequencePoint[@vc > 0])) &gt; 0">
                        <xsl:value-of select="count(Classes/Class/Methods/Method/SequencePoints/SequencePoint[@vc > 0]) div (count(Classes/Class/Methods/Method/SequencePoints/SequencePoint[@vc = 0]) + count(Classes/Class/Methods/Method/SequencePoints/SequencePoint[@vc > 0])) * 100"/>%
                      </xsl:when>
                      <xsl:otherwise>
                        0
                      </xsl:otherwise>
                    </xsl:choose>
                  </td>
                </tr>
              </table>
            </div>
          </th>
          <th title="LineCoverage">
            <xsl:if test="(Classes/Class/Methods/Method)">
              <xsl:value-of select="round(count(Classes/Class/Methods/Method/SequencePoints/SequencePoint[@vc > 0]) div (count(Classes/Class/Methods/Method/SequencePoints/SequencePoint[@vc = 0]) + count(Classes/Class/Methods/Method/SequencePoints/SequencePoint[@vc > 0])) * 100)"/>%
            </xsl:if>
          </th>
          <td>
            <xsl:variable name="width" select="count(Classes/Class/Methods/Method/SequencePoints/SequencePoint[@vc > 0]) div (count(Classes/Class/Methods/Method/SequencePoints/SequencePoint[@vc = 0]) + count(Classes/Class/Methods/Method/SequencePoints/SequencePoint[@vc > 0])) * 100"></xsl:variable>
            <table class="coverage">
              <tr>
                <td class="green" style="width: {$width}px;">
                  &#160;
                </td>
                <td class="red" style="width: {100-$width}px;">
                  &#160;
                </td>
              </tr>
            </table>
          </td>
        </tr>
        <xsl:for-each select="Classes/Class">
          <xsl:if test="FullName[not(contains(text(),'&lt;'))]">
            <tr class="classrow">
              <td align="center">
                <h3 class="sectionheader">
                  Class Name:<xsl:value-of select="FullName"></xsl:value-of>
                </h3>
              </td>
              <td title="LineCoverage">
                <xsl:value-of select="round(count(Methods/Method/SequencePoints/SequencePoint[@vc > 0]) div (count(Methods/Method/SequencePoints/SequencePoint[@vc = 0]) + count(Methods/Method/SequencePoints/SequencePoint[@vc > 0])) * 100)"/>%
              </td>
              <td>
                <table class="coverage">
                  <tr width="100px">
                    <xsl:variable name="Line.CoveragerClass" select="round(count(Methods/Method/SequencePoints/SequencePoint[@vc > 0]) div (count(Methods/Method/SequencePoints/SequencePoint[@vc = 0]) + count(Methods/Method/SequencePoints/SequencePoint[@vc > 0])) * 100)"></xsl:variable>
                    <td class="green" style="width: {$Line.CoveragerClass +9}px;">
                      &#160;
                    </td>
                    <td class="red" style="width: {100- $Line.CoveragerClass}px;">
                      &#160;
                    </td>
                  </tr>
                </table>
              </td>
            </tr>
            <tr class="classrow">
              <td colspan="3">
                <table class="overview">
                  <colgroup>
                    <col width="130" />
                    <col />
                  </colgroup>
                  <tr>
                    <td class="sectionheader">
                      Class:
                    </td>
                    <td>
                      <xsl:value-of select="FullName"></xsl:value-of>
                    </td>
                  </tr>
                  <tr>
                    <td class="sectionheader">
                      Assembly:
                    </td>
                    <td>
                      <xsl:value-of select="$ModulenameVariable"></xsl:value-of>
                    </td>
                  </tr>
                  <tr>
                    <td class="sectionheader">
                      File(s):
                    </td>
                    <td>
                      <xsl:variable name="CounterForFile" select="position()"></xsl:variable>
                      <xsl:value-of select="$FileLocationLink/Files/File[$CounterForFile]/@fullPath"/>
                      <xsl:variable name="FilePathVariable" select="//Files/File[@uid=($CounterForFile -1)]/@fullPath"></xsl:variable>
                      <a href="file:///{$FilePathVariable}">
                        <!--<xsl:value-of select="$FilePathVariable"></xsl:value-of>-->
                      </a>
                    </td>
                  </tr>
                  <tr>
                    <td class="sectionheader">
                      Coverage:
                    </td>
                    <td>
                      <xsl:variable name="Covered.lines" select="count(Methods/Method/SequencePoints/SequencePoint[@vc > 0])"></xsl:variable>
                      <xsl:value-of select="count(Methods/Method/SequencePoints/SequencePoint[@vc > 0]) div (count(Methods/Method/SequencePoints/SequencePoint[@vc = 0]) + count(Methods/Method/SequencePoints/SequencePoint[@vc > 0])) * 100"/>%
                    </td>
                  </tr>
                  <tr>
                    <td class="sectionheader">
                      Covered lines:
                    </td>
                    <td>
                      <xsl:value-of  select="count(Methods/Method/SequencePoints/SequencePoint[@vc > 0])"></xsl:value-of>
                    </td>
                  </tr>
                  <tr>
                    <td class="sectionheader">
                      Coverable lines:
                    </td>
                    <td>
                      <xsl:value-of  select="count(Methods/Method/SequencePoints/SequencePoint)"></xsl:value-of>
                    </td>
                  </tr>
                  <tr>
                    <td class="sectionheader">
                      Total lines:
                    </td>
                    <td>
                      51
                    </td>
                  </tr>
                </table>
              </td>
            </tr>
            <tr class="classrow">
              <td colspan="3">
                <table class="overview">
                  <tr>
                    <td class="sectionheader">
                      Method
                    </td>
                    <td class="sectionheader">
                      Cyclomatic Complexity
                    </td>
                    <td class="sectionheader">
                      Sequence Coverage
                    </td>
                    <td class="sectionheader">
                      Branch Coverage
                    </td>
                    <td class="sectionheader">
                      Static Method
                    </td>
                  </tr>
                  <xsl:for-each select="Methods/Method[@isConstructor='false']">
                    <tr>
                      <td>
                        <xsl:variable name="indexvariable" select="string-length(substring-before(Name, '::'))"/>
                        <xsl:value-of disable-output-escaping="yes" select="substring(Name,$indexvariable +3)"></xsl:value-of>
                      </td>
                      <td>
                        <xsl:value-of disable-output-escaping="yes" select="@cyclomaticComplexity"></xsl:value-of>
                      </td>
                      <td>
                        <xsl:value-of disable-output-escaping="yes" select="@sequenceCoverage"></xsl:value-of>
                      </td>
                      <td>
                        <xsl:value-of disable-output-escaping="yes" select="@branchCoverage"></xsl:value-of>
                      </td>
                      <td>
                        <xsl:value-of disable-output-escaping="yes" select="@isStatic"></xsl:value-of>
                      </td>
                    </tr>
                  </xsl:for-each>
                </table>
              </td>
            </tr>
          </xsl:if>
        </xsl:for-each>
      </xsl:for-each>
    </table>
	</body></html>
  </xsl:template>
</xsl:stylesheet>
