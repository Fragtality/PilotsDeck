# pwsh -ExecutionPolicy Unrestricted -file "$(ProjectDir)BuildPlugin.ps1" $(Configuration) $(SolutionDir) $(ProjectDir)

######### CONFIG
$cfgDeploy = $false
$cfgDeployAutoStart = $true
$cfgCleanDeploy = $false # !!!
$cfgCleanLog = $false
$cfgUpdatePI = $true
$cfgUpdateImages = $true
$cfgResetConfig = $false # !!!
$cfgSkipManagerDebug = $true
$cfgBuildInstaller = $true

if ($args[0] -eq "*Undefined*") {
	exit 0
}

if ($args[1] -eq "*Undefined*") {
	exit 0
}

$buildConfiguration = $args[0]
$pathBase = $args[1]
$pathProject = $args[2]
$streamDeckExePath = "C:\Program Files\Elgato\StreamDeck\StreamDeck.exe"
$msBuildDir = "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\amd64"

try {
	$pathProfileManager = Join-Path $pathBase "ProfileManager"

	cd $pathProject
	$pluginManifest = Get-Content -Raw (Join-Path $pathProject manifest.json) | ConvertFrom-Json
	$pluginVersion = $pluginManifest.Version
	$pluginUUID = $pluginManifest.UUID
	$pathPublish = Join-Path $pathBase "Releases\$pluginUUID.sdPlugin"
	$pathPlugin = Join-Path ($env:APPDATA) "Elgato\StreamDeck\Plugins\$pluginUUID.sdPlugin"
	$pathInstallerPayload = Join-Path $pathBase "Installer\Payload"

	######### BUILD
	## Create Lock
	Write-Host $pathBase
	cd $pathBase
	if (-not (Test-Path -Path "build.lck")) {
		"lock" | Out-File -File "build.lck"
	}
	else {
		Write-Host "Lock active - sure?"							 
		exit 0
	}

	if (-not $buildConfiguration) {
		$buildConfiguration = "Release"
	}
	Write-Host ("Build Configuration: '$buildConfiguration'")

	## Plugin Build
	Write-Host "dotnet publish for Plugin (v$pluginVersion) ..."
	Remove-Item -Recurse -Force -Path ($pathPublish + "\*")
	cd $pathProject
	dotnet publish -p:PublishProfile=PubProfile$buildConfiguration -p:Version="$pluginVersion" -c $buildConfiguration --verbosity quiet

	## ProfileManager Build
	if (($buildConfiguration -eq "Debug" -and -not $cfgSkipManagerDebug) -or $buildConfiguration -eq "Release") {
		Write-Host "msbuild for ProfileManager ..."
		cd $msBuildDir
		.\msbuild.exe (Join-Path $pathBase "\PilotsDeck.sln") /t:ProfileManager:rebuild /p:Configuration="Release" /p:Platform=x64 /p:BuildProjectReferences=false -verbosity:quiet
		Write-Host "dotnet publish for ProfileManager ..."
		cd $pathProfileManager
		dotnet publish -p:PublishProfile=FolderProfile -p:Version="$pluginVersion" -c $buildConfiguration --verbosity quiet
	}
	Remove-Item -Recurse -Force -Path ($pathPublish + "\*.pdb")


	######### DEPLOY
	## Stop StreamDeck
	if ($cfgDeploy) {
		Write-Host "Stopping StreamDeck and Plugin ..."
		Get-Process -Name ("StreamDeck", "PilotsDeck") -ErrorAction SilentlyContinue | Stop-Process –force -ErrorAction SilentlyContinue
		Sleep(4)
	}

	## Clean Deploy
	if ($cfgDeploy -and $cfgCleanDeploy) {
		Write-Host "Removing old Plugin Files ..."

		New-Item -Type Directory -Path $pathPlugin -ErrorAction SilentlyContinue | Out-Null
		Remove-Item -Recurse -Force -Path ($pathPlugin + "\*") | Out-Null
		Copy-Item -Path ($pathPublish + "\*") -Destination $pathPlugin -Recurse -Force | Out-Null
	}
	## Update Deploy
	elseif ($cfgDeploy) {
		Write-Host "Copy new Binaries ..."
		Copy-Item -Path ($pathPublish + "\*") -Destination $pathPlugin -Force | Out-Null

		$config = Join-Path $pathPlugin "PluginConfig.json"
		$colorstore = Join-Path $pathPlugin "ColorStore.json"

		if ($cfgResetConfig) {
			Write-Host "Resetting Configuration ..."
			if ((Test-Path $config)) {
				Remove-Item -Path $config -Force -ErrorAction SilentlyContinue | Out-Null
			}
			if ((Test-Path $colorstore)) {
				Remove-Item -Path $colorstore -Force -ErrorAction SilentlyContinue | Out-Null
			}
		}

		if ($cfgUpdatePI) {
			Write-Host "Updating PIs ..."
			Copy-Item -Path ($pathPublish + "\Plugin\*") -Destination ($pathPlugin + "\Plugin\") -Force -Recurse | Out-Null
		}
		if ($cfgUpdateImages) {
			Write-Host "Updating Images ..."
			Copy-Item -Path ($pathPublish + "\Images\*") -Destination ($pathPlugin + "\Images\") -Force -Recurse | Out-Null
		}

		if ($cfgDeploy -and $cfgCleanLog) {
			Write-Host "Removing Logs ..."				   
			New-Item -Type Directory -Path $pathPlugin -ErrorAction SilentlyContinue | Out-Null																		 
			Remove-Item -Recurse -Force -Path ($pathPlugin + "\log\*") | Out-Null	
		}
	}

	## Start StreamDeck
	if ($cfgDeploy -and $cfgDeployAutoStart) {
		Write-Host "Restarting Stream Deck ..."
		Start-Process $streamDeckExePath
	}


	######### INSTALLER
	if ($cfgBuildInstaller -and $buildConfiguration -eq "Release") {
		Write-Host "msbuild for Installer ..."
		cd $msBuildDir
		.\msbuild.exe (Join-Path $pathBase "\PilotsDeck.sln") /t:Installer:rebuild /p:Configuration="Release" /p:Platform=x64 /p:BuildProjectReferences=false -verbosity:minimal
		if ($cfgDeploy) {
			Write-Host "Copy version.json ..."
			Copy-Item -Path (Join-Path $pathInstallerPayload "version.json") -Destination $pathPlugin -Force -ErrorAction SilentlyContinue | Out-Null
		}
	}

	## Remove lock
	cd $pathBase
	if ((Test-Path -Path "build.lck")) {
		Remove-Item -Path "build.lck"
	}

	Write-Host "SUCCESS: Build complete!"
	exit 0
}
catch {
	Write-Host "FAILED: Exception in BuildPlugin.ps1!"
	cd $pathBase
	Remove-Item -Path "build.lck"
	exit -1
}