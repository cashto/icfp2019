Param($BatchName, $SliceCount)

0 .. ($SliceCount - 1) |% { Start-Job -FilePath .\Run-Slice.ps1 -ArgumentList $BatchName, $_, $SliceCount }