$currentBlock = .\Get-CurrentBlock.ps1

while ($true)
{
	Start-Sleep 5
	$newBlock = .\Get-CurrentBlock.ps1
	if ($newBlock -ne $currentBlock)
	{
		Write-Host "Solving block $newBlock"
		Start-Job ".\Solve-Block.ps1" -ArgumentList $newBlock, $true
		$currentBlock = $newBlock
	}
}