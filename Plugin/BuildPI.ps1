#PRE pwsh -ExecutionPolicy Unrestricted -file "$(ProjectDir)BuildPI.ps1" $(Configuration) $(SolutionDir) $(ProjectDir)

if ($args[0] -eq "*Undefined*") {
	exit 0
}

if ($args[1] -eq "*Undefined*") {
	exit 0
}

$buildConfiguration = $args[0]
$pathBase = $args[1]
$pathProject = $args[2]

$pathOut = Join-Path $pathProject "Plugin/PI"
$pathPIsource = Join-Path $pathOut "src"
$pathPIinc = Join-Path $pathOut "inc"

function insertInclude($include) {
	Write-Host "`tincluding $include.html ..."
	$include = Join-Path $pathPIinc "$include.html"
	if (Test-Path -Path $include -PathType Leaf) {
		$include = Get-Content $include -Encoding UTF8 -Raw
	} else {
		Write-Host "The Include File does not exist!!! ($include)"
		$include = ""
	}
	
	return $include
}

$htmlFiles = Get-ChildItem -Path $pathPIsource -Filter *.html -File
foreach ($file in $htmlFiles) {
	Write-Host "Working on $($file.Name) ..."
    $content = Get-Content $file.FullName -Encoding UTF8 -Raw
	
	$regex = '%INCLUDE\[(?<filename>[^\]]+)\]%'
	$content = [regex]::Replace($content, $regex, {
		param($match)
		return insertInclude($match.Groups["filename"].Value)
	})
	
	Set-Content -Path (Join-Path $pathOut $file.Name) -Value $content -Encoding UTF8
}

exit 0