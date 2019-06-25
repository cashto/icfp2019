# Dedication

*"They got an awful lot of coffee in ~~Brazil~~ the ICFP contest"*
 
As a veteran of more than ten years participating in the ICFP contest, I can attest that the key to a good performance is the background music.  Initially the TV was set to the dance/techno music station, but after a short hour or two the relentless UNCE UNCE UNCE became unbearable.  Contemporary music, on the other hand, filled me with a raging need to change the station every three songs or so.  It was finally the Frank Sinatra station, of all things, which managed to keep me sane throughout the ups and downs of the contest. So this year's submission is dedicated to you, the one and only, Old Blue Eyes.

# Description 

The contest problem was to program a glorified Roomba.  Teams submitted traces of their solutions to 300 contest maps; shorter traces earned more points.  Each map contained a variety of powerups:

* Manipulator arm: add the ability to paint an extra square every move.
* Fast wheels: ability to move two squares per move for the next 50 moves.
* Drill: ability to move through walls for the next 30 moves.
* Teleport: ability to drop a teleport at the current location, and come back to it later on in a single move, as many times as desired.
* Clone: ability to create a new robot at specially designated spawn points.

Also during day two and three, there was a mini-contest where teams could earn virtual coins by solving maps posted online.  Each map would be available for for between 15 and 30 minutes (depending on how many submissions it received from other teams).  Teams were also required to propose a new map along with their submission; one of these maps would be chosen for the next round. 

Coins earned in this mini-contest could be used to buy additional powerups for the main contest, with unused coins would be added to the final score.

# Algorithm

* BoostPlan
    * Is there a manipulator powerup in hand?  If so, attach it either to the left or the right of the existing manipulator arms, always forming a straight line.
    * Is there a manipulator or teleport powerup within 20 moves?  If so, head straight for it.
    * Is there a teleport manipulator in hand, we are near the center of the board, and at least half a board away from another teleport?  If so, place it.
* Plan A
    * Otherwise, search all sequences of 4 moves (up, down, left, right, rotate, add fast wheels) and pick the sequence that maximizes number of powerups collected, followed by maximizing the number of cells painted.
        * This generally resulted in Fast Wheels powerups being played shortly after being picked up.   
        * Cells are weighted by number of walls surrounding them, so a corner cell is worth 4 points whereas a cell in the open is worth one.
    * Does this path paint any cells?  If so, play the sequence.
* Plan B
    * Otherwise, search all sequences of moves (up, down, left, right) and pick the one that first arrives at an unpainted cell.
    * Can this cell be reached at least ten moves faster by first using a drill powerup?  If so, use it.
    * Can this cell be reached any faster via a teleport?  If so, use it.
    * Otherwise, play a waiting move. (This was in response to the last bug discovered during the contest, that the last cell may not be reachable until fast wheels wear off).

# Known Issues

* I didn't have time to implement cloning, at all. 
* In Plan B, Old Blue Eyes had to physically occupy the unpainted cell, not just paint it.  Likely it would be enough to switch back to Plan A a few moves before reaching the cell.
* Also in Plan B, drills are only used to reach the cell identified by the initial search -- even if there are closer cells that could be reached by the drill.

# Results

The performance in the lightning round was mediocre. Old Blue Eyes was only able to solve 136 problems -- basically, all the maps up to 100 x 100 in size.  However even on some of these maps, Old Blue Eyes would run out of memory and crash.

Because this is a functional programming contest, my natural tendency is to program in the functional style -- for the Move() function to return a new state, rather than to mutate the current state.  This year I started off not doing that, since maps were up to 400 x 400 in size and so ain't nobody got time to copy 160KB of data each move when only a few cells are changing, especially when you end up putting a few thousand of these things in a priority queue for best-first or breadth-first search -- pretty soon you're talking real memory.

So State.Move() would return a new State, but both States (the initial and derived one) would point to the same Board.  Move() would also return an "undo list" which could be used to revert the state. It was tricky to keep everything straight in this model, and for a while I wondered if I had made the right tradeoff -- especially initially when I used Board.Clone() liberally. But towards the end I was able to rip out these Clone() calls, resulting in a massive improvement in performance.

Old Blue Eues was able to solve all of the problems in the full round.  It took about 25 minutes to solve all the problems on an 8-core VM.  I also repeated the solution with an extra teleport or manipulator powerup at the beginning.

* Extra teleport: improved 238 problems, average improvement 1.8%, best improvement 11.8% (prob-16).
* Extra manipulator: improved 268 problems, average improvement 5.2%. best improvement 19.5% (prob-070).

I used the 97,763 coins mined during the contest to purchase 80 manipulator arms and one teleport (for prob-076, which was the only problem where it was more effective to use a teleport (7.4% improvement) rather than an extra manipulator arm (2%)).

Because the fundamental algorithm was greedy, Old Blue Eyes ended up being as much a hedonist as his namesake.  He would happily leave cells in difficult-to-reach corners behind in exchange for hungrily gobbling up easy squares out in open space.  This later required extensive retracing of steps to return and fill in the gaps later on.

I wasted a lot of time on day 3 trying to improve on this -- time which, in retrospect, would be far better invested in implementing cloning.  Instead, various abortive "Plan C" attempts were made to reign in Old Blue Eye's excesses, but without much success.  The tradeoff in initial efficiency just wasn't worth it, and in any case the doubling-back was not always a waste -- some long corridors anyways required going back the same way the robot came in.  Eventually the only change that seemed to make an improvement was to weight corner cells a bit higher than squares in open space, but it still didn't fix the issue entirely.

The leaderboard had Old Blue Eyes at #114th place (of 194) when it froze three hours before the end of the contest -- but that was because I had briefly submitted an incomplete submission and which only contained 1/3rd of the problems, and that was the submission that got reflected in the leaderboard. Earlier in the day I was hovering in the 40s.
