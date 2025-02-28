### PRE (Installer)
### pwsh -ExecutionPolicy Unrestricted -file "$(ProjectDir)PackageApp.ps1" $(SolutionDir) "Installer" "Payload" "APP-PROJECT" "APP-NAME" "publish"
### add version.json as Embedded Resource

#Exit inner Invocation when invoked with dotnet cli
if ($args[0] -eq "*Undefined*") {
	exit 0
}

Function UpdateAssemblyInfo{
	param ($projectDir, $assemblyField, $newValue)
	$inFile = Join-Path $projectDir "Properties\AssemblyInfo.cs"
	$outFile = Join-Path $projectDir "Properties\AssemblyInfo.out"
	Get-Content -Path $inFile | % { $_ -Replace ('\[assembly: (' + "$assemblyField" + ')\("([^"]*)"\)]'), ('[assembly: $1("' + "$newValue" + '")]') } | Out-File $outFile -Encoding utf8NoBOM
	Move-Item $outFile $inFile -Force | Out-Null
}

#Script Parameters
$basePath = $args[0]												#0 Solution Directory (all other Paths relative to that)
$pathProjectInstaller = Join-Path $basePath $args[1]				#1 Installer Project Directory
$pathPayload = Join-Path $basePath (Join-Path $args[1] $args[2])	#2 Payload Directory for Installer
$projectName = $args[3]												#3 App Project (Directory) Name
$appName = $args[4]													#4 App Binary Name
$pathPublish = Join-Path $basePath $args[5]							#5 Publish Directory of App (to pack for Installer) - relative to base
$binFile = "$appName.exe"

$zipPath = Join-Path $pathPayload "AppPackage.zip"
$binPath = Join-Path $pathPublish $binFile
$confPath = Join-Path $basePath $confFile
$pathProjectApp = Join-Path $basePath $projectName

#Get App Version
Write-Host "Read $appName Binary ..."
$version = (Get-Item $binPath).VersionInfo.FileVersion
$timestamp = (Get-Item $binPath).LastWriteTimeUtc | Get-Date -Format "yyyy.MM.dd.HHmm"
$company = (Get-Item $binPath).VersionInfo.CompanyName
$year = Get-Date -Format "yyyy"

#Version JSON
Write-Host "Create version.json ..."
@"
{
	"Version": "$version",
	"Timestamp": "$timestamp"
}
"@ | Out-File (Join-Path $pathPayload "version.json") -Encoding utf8NoBOM

#AssemblyInfo File
Write-Host "Update Installer AssemblyInfo ..."
UpdateAssemblyInfo $pathProjectInstaller "AssemblyTitle" "$appName Installer v$version ($timestamp)"
UpdateAssemblyInfo $pathProjectInstaller "AssemblyDescription" "Installer Application for $appName"
UpdateAssemblyInfo $pathProjectInstaller "AssemblyCompany" "$company"
UpdateAssemblyInfo $pathProjectInstaller "AssemblyProduct" "$appName Installer"
UpdateAssemblyInfo $pathProjectInstaller "AssemblyCopyright" "Copyright © $year"
UpdateAssemblyInfo $pathProjectInstaller "AssemblyVersion" "$version"
UpdateAssemblyInfo $pathProjectInstaller "AssemblyFileVersion" "$version"


#AppPackage ZIP
Write-Host "Zip AppPackage ..."
Remove-Item $zipPath -ErrorAction SilentlyContinue | Out-Null
& "C:\Program Files\7-Zip\7z.exe" a -tzip $zipPath ($pathPublish + "\*") | Out-Null

#Config File
Write-Host "Create default Config ..."
cd $pathProjectInstaller
.\CreateDefaultConfig.ps1 $basePath $pathPayload $binPath | Out-Null

exit 0


