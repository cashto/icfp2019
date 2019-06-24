Param(
	$File, $Slice, $SliceCount
)

$i = 0
gc $File |% {
	if ($i % $SliceCount -eq $Slice) {
		$_
	}
	$i = $i + 1
}