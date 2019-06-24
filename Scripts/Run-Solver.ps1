Param(
	[string] $probId
)

$root = "C:\Users\cashto\Documents\GitHub\icfp2019"
$solver = "$root\src\Solver\bin\Release\Solver.exe"
$solution = "$root\solutions\$probId.sol"

$result = & $solver "$root\problems\$probId.desc" R

$result | Out-File -Encoding ASCII $solution

