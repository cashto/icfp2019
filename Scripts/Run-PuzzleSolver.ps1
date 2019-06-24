Param(
	[string] $probId
)

$root = "C:\Users\cashto\Documents\GitHub\icfp2019"
$solver = "$root\src\Solver\bin\Release\Solver.exe"
$solution = "$root\work\puzzlesolutions\$probId.desc"

$result = & $solver puzzle "$root\work\puzzles\$probId.desc" 

$result | Out-File -Encoding ASCII $solution

