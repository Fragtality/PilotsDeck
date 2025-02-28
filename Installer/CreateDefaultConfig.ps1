### args[0] => Solution/Base Path
### args[1] => Payload Path
### args[2] => App Binary (published) Path

$cmd = $args[2]
$dest = $args[1]
Invoke-Expression "$cmd --writeConfig $dest" | Out-Null