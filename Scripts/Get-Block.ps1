Param($BlockId)

$root = "C:\Users\cashto\Documents\GitHub\icfp2019"

$json = (& python.exe .\lambda-cli.py getblockinfo $BlockId) | convertfrom-json

$json.task | Out-File -Encoding ASCII "$root\problems\block-$BlockId.desc"
$json.puzzle | Out-File -Encoding ASCII "$root\work\puzzles\block-$BlockId.desc"