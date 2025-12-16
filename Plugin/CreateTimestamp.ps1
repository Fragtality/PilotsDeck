### PRE
### pwsh -ExecutionPolicy Unrestricted -file "$(ProjectDir)CreateTimestamp.ps1" $(SolutionDir) $(ProjectDir) "Installer" "Payload"

$basePath = $args[0]
$pathProject = $args[1]
if ($args[0] -eq "*Undefined*" -or $args[1] -eq "*Undefined*") {
	exit 0
}
$pathPayload = Join-Path $basePath (Join-Path $args[2] $args[3])

if ((Test-Path -Path (Join-Path $basePath "build.lck"))) {
	exit 0
}

$version = (Get-Content -Raw (Join-Path $pathProject manifest.json) | ConvertFrom-Json).Version
$timestamp = $([System.DateTime]::UtcNow.ToString("yyyy.MM.dd.HHmm"))
$build = $version + "+build" + $timestamp
Write-Host "Create version.json for $build ..."
@"
{
	"Version": "$version",
	"Timestamp": "$timestamp"
}
"@ | Out-File (Join-Path $pathPayload "version.json") -Encoding utf8NoBOM