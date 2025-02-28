### PRE
### pwsh -ExecutionPolicy Unrestricted -file "$(ProjectDir)..\NuPreBuild.ps1" $(SolutionDir) $(ProjectDir) "PROJECT" PACKAGES...

$basePath = $args[0]
$projectDir = $args[1]
if ($args[0] -eq "*Undefined*" -or -not $args[0]) {
	cd $projectDir
	cd ..
	$basePath = (pwd).Path
}

$packageName = $args[2]
$pathRepo = "..\CFIT\PackageRepo"
$nugetCli = Join-Path $basePath "nuget.exe"

$packCfg = "packages.config"
Function GetInstalledVersion{
	param ($package)
	if ((Test-Path -Path $packCfg)) {
		return (([xml](Get-Content $packCfg)).packages.ChildNodes | Where-Object id -like $package).version
	} else {
		$regex = (dotnet list package | Select-String -Pattern ($package + '\s+\S+\s+(\S+)'))
		if ($regex.Matches -and $regex.Matches.length -gt 0 -and $regex.Matches[0].Groups.length -gt 1) {
			return $regex.Matches[0].Groups[1].Value
		} else {
			return ""
		}
	}
}

Function UpdatePackage{
	param ($package)
	if ((Test-Path -Path $packCfg)) {
		Invoke-Expression "$nugetCli update $packCfg -Id $package -Source $pathRepo -NonInteractive -Verbosity quiet"
	} else {
		Invoke-Expression "dotnet add package $package" | Out-Null
	}
}

cd $basePath
cd $pathRepo
$pathRepo = (pwd).Path

Write-Host "Checking NuGet Dependencies for $packageName ..."
cd $projectDir
$count = 0
for ($index = 3; $index -lt $args.length; $index++) {
	$package = $args[$index]
	$packageVersion = GetInstalledVersion($package)
	$latestFile = (ls $pathRepo | Where-Object Name -like "$package*" | Sort-Object LastWriteTime)[-1].Name
	$latestVersion = (echo $latestFile | Select-String -Pattern '[^0-9]*(\d+\.\d+\.\d+\.\d+)\.nupkg').Matches[0].Groups[1].Value
	if ($latestVersion -and $packageVersion -and $latestVersion -ne $packageVersion) {
		Write-Host " => Updating '$package': $packageVersion => $latestVersion"
		UpdatePackage($package)
		$count++
	}	
}

if ($count -gt 0) {
	if (-not (Test-Path -Path $packCfg)) {
		dotnet restore --verbosity quiet
	}
}
exit 0