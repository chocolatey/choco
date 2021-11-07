[CmdletBinding()]
Param(
  [Parameter(Mandatory=$True,Position=1)]
  [string]$xsl,
	
  [Parameter(Mandatory=$True)]
  [string]$xml,

  [Parameter(Mandatory=$True)]
  [string]$output
)

$xslt = New-Object System.Xml.Xsl.XslCompiledTransform;
$xslt.Load($xsl);
$xslt.Transform($xml, $output);

Write-Host "The file has been transformed."
