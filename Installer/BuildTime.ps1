$buildTime = 'namespace Installer { public static class Build { public static string Timestamp = "' + (Get-Date -Format 'yyyyMMddHHmm') + '"; }}' 
echo $buildTime > ($args[0] + "BuildTimestamp.cs")