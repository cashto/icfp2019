Param($batchName, $slice, $sliceNumber)

$root = "C:\Users\cashto\Documents\GitHub\icfp2019"
cd "$root\work"

./get-slice .\$batchName.txt $slice $sliceNumber |% { ./Run-Solver $_ }