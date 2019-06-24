Param($BlockId, $Submit = $false)

$root = "C:\Users\cashto\Documents\GitHub\icfp2019"
cd "$root\work"

./Get-Block $BlockID

$job1 = Start-Job -FilePath "./Run-Solver.ps1" -ArgumentList "block-$BlockId"

$job2 = Start-Job -FilePath "./Run-PuzzleSolver.ps1" -ArgumentList "block-$BlockId"

$done = Wait-Job $job1
Remove-Job -Id $job1.Id

$done = Wait-Job $job2
Remove-Job -Id $job2.Id

write-host "About to submit: $Submit"
if ($Submit)
{
	write-host "submitting"
	.\Submit-Block.ps1 $BlockId
}
#& ./lambda-cli.py submit $BlockID "$root\solutions\block-$BlockId.sol" "$root\work\puzzlesolutions\block-$BlockId.desc"