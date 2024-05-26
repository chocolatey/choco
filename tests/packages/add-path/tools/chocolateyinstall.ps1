$params = Get-PackageParameters
$path = $params["Path"]
$scope = $params["Scope"]

if (-not $path) {
	throw "You must specify the package parameter /Path"
}

if (-not $scope) {
	$scope = "User"
}

Write-Host "Adding '$path' to PATH at scope $scope"

Install-ChocolateyPath -PathToInstall $path -PathType $scope

Write-Host "$scope PATH after install: $env:PATH"