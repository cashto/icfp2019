del best_solutions\*

$day2_probs = gc ".\day2_probs.txt"
$b_probs = gc ".\b_probs.txt"
$r_probs = gc ".\r_probs.txt"

copy "finalsolutions\*.sol" best_solutions
$day2_probs |% { copy "day2solutions\$_.sol" best_solutions }
$b_probs |% { copy "b_solutions\$_.sol" best_solutions; copy b.buy "best_solutions\$_.buy" }
$r_probs |% { copy "r_solutions\$_.sol" best_solutions; copy r.buy "best_solutions\$_.buy" }

