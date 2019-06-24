Param($BlockId)

$solution = "$root\solutions\block-$BlockId.sol";
$task = "$root\work\puzzlesolutions\block-$BlockId.desc";

if (-not (Test-Path $solution) -or (Get-Item $solution).Length -eq 0)
{
	throw "Solution is missing"
	return;
}

if (-not (Test-Path $task) -or (Get-Item $task).Length -eq 0)
{
	throw "Task is missing"
	return;
}

& python.exe ./lambda-cli.py submit $BlockID $solution $task