### POST (Installer)
### pwsh -ExecutionPolicy Unrestricted -file "$(ProjectDir)ExportInstaller.ps1" $(SolutionDir) $(TargetDir) $(TargetFileName) "<APP>"

#Exit inner Invocation when invoked with dotnet cli
if ($args[0] -eq "*Undefined*") {
	exit 0
}

#Script Parameters
$pathBase = $args[0]						#0 Solution Directory
$binOutDir = $args[1]						#1 Absolute Path to Output Directory
$binOutFile = $args[2]						#2 Installer Binary Filename
$binOutPath = Join-Path $args[1] $args[2]
$appName = $args[3]							#3 Application Name to use in Filename
$suffix = "latest"							#4 (Optional) Suffix to add to the File (default 'latest')
if ([bool]$args[4]) {
	$suffix = $args[4]
}

Write-Host "Exporting $appName Installer ..."
Copy-Item $binOutPath (Join-Path $pathBase "$appName-Installer-$suffix.exe") -Force | Out-Null

