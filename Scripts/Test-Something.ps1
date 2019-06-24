Param($BlockId, $Submit = $false)

$root = "C:\Users\cashto\Documents\GitHub\icfp2019"
cd "$root\work"

./Get-Block $BlockID

write-host "About to submit: $Submit"
if ($Submit)
{
	write-host "submitting"
	.\Submit-Block.ps1 $BlockId
}
#& ./lambda-cli.py submit $BlockID "$root\solutions\block-$BlockId.sol" "$root\work\puzzlesolutions\block-$BlockId.desc"