using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Solver
{
    public class State
    {
        public Dictionary<Point, int> Weights { get; set; }
        public Board Board { get; set; }
        public int DrillTime { get; private set; }
        public int FastWheelsTime { get; private set; }
        public Point Position { get; private set; }
        public List<Point> Robot { get; private set; }
        public List<char> Boosts { get; private set; }
        public int UnpaintedCount { get; set; }
        public int Direction { get; private set; }
        public int BoostsCollected { get; private set; }

        private State()
        {

        }

        public State(string description)
        {
            var fields = description.Split('#');

            var map = ParseList(fields[0], ',', ParsePoint).ToList();

            Position = ParsePoint(fields[1]).Item1;

            var obstacles = fields[2].Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(i => ParseList(i, ',', ParsePoint).ToList())
                .ToList();

            var boosts = fields[3].Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(i =>
                {
                    var boost = i[0];
                    var point = ParsePoint(i.Substring(1));
                    return Tuple.Create(boost, point.Item1);
                })
                .ToList();

            Board = new Board(map, obstacles, boosts);

            Robot = new List<Point>()
            {
                new Point(1, -1),
                new Point(1,  0),
                new Point(1,  1)
            };

            Boosts = new List<char>();

            Paint(new BoardUndo());

            UnpaintedCount = Board.AllPoints.Count(p => !Board.IsWall(p) && !Board.IsPainted(p));
        }

        public override bool Equals(object obj)
        {
            var other = obj as State;
            if (other == null)
            {
                return false;
            }

            return GetHashTuple().Equals(other.GetHashTuple());
        }

        public override int GetHashCode()
        {
            return GetHashTuple().GetHashCode();
        }

        public Tuple<int, int, int, int, int, int> GetHashTuple()
        {
            return Tuple.Create(Position.X, Position.Y, Direction, UnpaintedCount, DrillTime + FastWheelsTime, BoostsCollected);
        }

        public override string ToString()
        {
            return $"Remain={UnpaintedCount} Drill={DrillTime} Wheels={FastWheelsTime} Boosts={new string(Boosts.ToArray())}";
            //return $"Remain={UnpaintedCount} Drill={DrillTime} Wheels={FastWheelsTime} Boosts={new string(Boosts.ToArray())}\n{Board.ToString(Position)}";
        }

        private State CloneNextStep()
        {
            var newState = Clone();
            newState.NextStep();
            return newState;
        }

        private void NextStep()
        {
            FastWheelsTime = Math.Max(0, FastWheelsTime - 1);
            DrillTime = Math.Max(0, DrillTime - 1);
        }

        private State Clone()
        {
            return new State()
            {
                Weights = Weights,
                Board = Board,
                DrillTime = DrillTime,
                FastWheelsTime = FastWheelsTime,
                Position = Position,
                Direction = Direction,
                Robot = Robot,
                Boosts = Boosts,
                UnpaintedCount = UnpaintedCount,
                BoostsCollected = BoostsCollected
            };
        }

        public State MultiMove(string s)
        {
            var ans = this;
            var i = 0;

            while (s != string.Empty)
            {
                ++i;
                if (i == 445)
                {
                    ;
                }

                var newState = ans.MoveOne(s);
                ans = newState.Item1;
                s = newState.Item2;
            }

            return ans;
        }

        public Tuple<State, string> MoveOne(string s)
        {
            State newState = null;

            var c = s[0];
            s = s.Substring(1);

            if (c == Board.Manipulator)
            {
                var point = ParsePoint(s);
                s = point.Item2;
                newState = AddManipulator(point.Item1)?.Item1;
            }
            else
            {
                newState = Move(c)?.Item1;
            }
            
            if (newState == null)
            {
                ;
            }

            return Tuple.Create(newState ?? this, s);
        }

        // Returns null if move is possible or state does not change
        // Mutates this.Board -- returns a BoardUndo that can revert the changes
        public Tuple<State, BoardUndo> Move(char move)
        {
            State newState;
            BoardUndo undo = null;

            switch (move)
            {
                case 'W': // up
                    return Move(new Point(0, 1));
                case 'S': // down
                    return Move(new Point(0, -1));
                case 'A': // left
                    return Move(new Point(-1, 0));
                case 'D': // right
                    return Move(new Point(1, 0));

                case 'E': // CW
                case 'Q': // CCW
                    newState = CloneNextStep();
                    newState.Robot = newState.Robot
                        .Select(p => move == 'E' ? p.RotateRight() : p.RotateLeft())
                        .ToList();
                    newState.Direction = (newState.Direction + (move == 'E' ? 1 : 3)) % 4;
                    undo = new BoardUndo();
                    newState.Paint(undo);
                    return Tuple.Create(newState, undo);

                case 'F': // fast wheels
                    if (!Boosts.Any(i => i == Board.FastWheels))
                    {
                        return null;
                    }

                    newState = CloneNextStep();
                    newState.FastWheelsTime = Math.Max(newState.FastWheelsTime + 50, 50);
                    newState.Boosts = newState.Boosts.ToList();
                    newState.Boosts.Remove(Board.FastWheels);
                    return Tuple.Create(newState, undo);

                case 'L': // drill
                    if (!Boosts.Any(i => i == Board.Drill))
                    {
                        return null;
                    }

                    newState = CloneNextStep();
                    newState.DrillTime = Math.Max(newState.DrillTime + 30, 30);
                    newState.Boosts = newState.Boosts.ToList();
                    newState.Boosts.Remove(Board.Drill);
                    return Tuple.Create(newState, undo);

                //case 'R': // teleport
            }

            return null;
        }

        private Tuple<State, BoardUndo> Move(Point p)
        {
            var newState = Clone();
            var ans = newState.MoveOneStep(p);

            if (ans != null && FastWheelsTime > 0)
            {
                ans = newState.MoveOneStep(p, ans.Item2) ?? ans;
            }

            if (ans != null)
            {
                ans.Item1.NextStep(); 
            }

            return ans;
        }

        private Tuple<State, BoardUndo> MoveOneStep(Point dir, BoardUndo undo = null)
        {
            var newPosition = Position + dir;

            var item = Board.Get(newPosition);
            if (item == Board.Wall &&
                (DrillTime == 0 || !Board.IsInBounds(newPosition)))
            {
                return null;
            }

            Position = newPosition;

            undo = undo ?? new BoardUndo();

            if (item == Board.Wall)
            {
                Board.Set(newPosition, Board.Empty, undo);
            }
            else if ("BFL".Contains(item))
            {
                Boosts = Boosts.ToList();
                Boosts.Add(item);
                Board.Set(newPosition, Board.IsPainted(newPosition) ? Board.Empty : Board.UnpaintedEmpty, undo);
                ++BoostsCollected;
            }
            
            Paint(undo);

            return Tuple.Create(this, undo);
        }

        public Tuple<State, BoardUndo> AddManipulator(Point where)
        {
            var oldBoard = Board.Clone();

            var newState = CloneNextStep();
            newState.Robot = newState.Robot.ToList();
            newState.Robot.Add(where);
            newState.Boosts = newState.Boosts.ToList();
            newState.Boosts.Remove(Board.Manipulator);

            var undo = new BoardUndo();
            newState.Paint(undo);
            Board = oldBoard;

            return Tuple.Create(newState, undo);
        }

        private void Paint(BoardUndo undo)
        {
            var initialCount = undo.Count;

            Board.Paint(Position, undo);
            foreach (var dir in Robot)
            {
                if (Blockers(dir).All(i => !Board.IsWall(Position + i)))
                {
                    Board.Paint(Position + dir, undo);
                }
            }

            UnpaintedCount -= undo.Count - initialCount;
        }

        private static readonly List<Point> EmptyPointList = new List<Point>();
        public static IEnumerable<Point> Blockers(Point p)
        {
            if (Math.Abs(p.X) < 2 && Math.Abs(p.Y) < 2)
            {
                return EmptyPointList;
            }
            else if (p.X < 0)
            {
                return Blockers(new Point(-p.X, p.Y)).Select(i => new Point(-i.X, i.Y));
            }
            else if (p.Y < 0)
            {
                return Blockers(new Point(p.X, -p.Y)).Select(i => new Point(i.X, -i.Y));
            }
            else if (p.Y > p.X)
            {
                return Blockers(new Point(p.Y, p.X)).Select(i => new Point(i.Y, i.X));
            }
            else
            {
                return BasicBlockers(p);
            }
        }

        // X > 0, Y > 0, X > Y
        private static IEnumerable<Point> BasicBlockers(Point p)
        {
            if (p.Y != 1)
            {
                throw new NotImplementedException();
            }

            for (var i = 0; i < p.X / 2; ++i)
            {
                yield return new Point(1 + i, 0);
                yield return new Point((p.X + 1) / 2 + i, 1);
            }
        }

        public static IEnumerable<T> ParseList<T>(string s, char sep, Func<string, Tuple<T, string>> parser)
        {
            while (s != string.Empty)
            {
                var result = parser(s);
                yield return result.Item1;
                s = result.Item2;
                if (s.Length > 0 && s[0] == sep)
                {
                    s = s.Substring(1);
                }
            }
        }

        public static Tuple<Point, string> ParsePoint(string s)
        {
            var match = Regex.Match(s, @"^\((-?\d+),(-?\d+)\)(.*)");
            return Tuple.Create(
                new Point(int.Parse(match.Groups[1].Value), int.Parse(match.Groups[2].Value)),
                match.Groups[3].Value);
        }
    }
}