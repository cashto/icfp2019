﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Solver
{
    public class State
    {
        public Board Board { get; set; }
        public int DrillTime { get; private set; }
        public int FastWheelsTime { get; private set; }
        public Point Position { get; private set; }
        public List<Point> Robot { get; private set; }
        public List<char> Boosts { get; private set; }
        public int UnpaintedCount { get; private set; }
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

        private Tuple<int, int, int, int, int, int> GetHashTuple()
        {
            return Tuple.Create(Position.X, Position.Y, Direction, UnpaintedCount, DrillTime + FastWheelsTime, BoostsCollected);
        }

        public override string ToString()
        {
            return $"Remain={UnpaintedCount} Drill={DrillTime} Wheels={FastWheelsTime} Boosts={new string(Boosts.ToArray())}\n{Board.ToString(Position)}";
        }

        private State CloneNextStep()
        {
            var newState = Clone();
            newState.FastWheelsTime = Math.Max(0, newState.FastWheelsTime - 1);
            newState.DrillTime = Math.Max(0, newState.DrillTime - 1);
            return newState;
        }

        private State Clone()
        {
            return new State()
            {
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

            foreach (var c in s)
            {
                var newState = ans.Move(c)?.Item1;
                ans = newState ?? ans;
            }

            return ans;
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

                //case 'B': // attach manip

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
            var newState = CloneNextStep();
            var ans = newState.MoveOneStep(p);

            if (ans == null || FastWheelsTime == 0)
            {
                return ans;
            }

            var ans2 = newState.MoveOneStep(p, ans.Item2);
            return ans2 ?? ans;
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
            else if (item != Board.Empty && item != Board.Mystery)
            {
                Boosts = Boosts.ToList();
                Boosts.Add(item);
                Board.Set(newPosition, Board.IsPainted(newPosition) ? Board.Empty : Board.UnpaintedEmpty, undo);
                ++BoostsCollected;
            }
            
            Paint(undo);

            return Tuple.Create(this, undo);
        }

        private void Paint(BoardUndo undo)
        {
            var initialCount = undo.Count;

            Board.Paint(Position, undo);
            foreach (var dir in Robot)
            {
                Board.Paint(Position + dir, undo);
            }

            UnpaintedCount -= undo.Count - initialCount;
        }

        private static IEnumerable<T> ParseList<T>(string s, char sep, Func<string, Tuple<T, string>> parser)
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

        private static Tuple<Point, string> ParsePoint(string s)
        {
            var match = Regex.Match(s, @"^\((\d+),(\d+)\)(.*)");
            return Tuple.Create(
                new Point(int.Parse(match.Groups[1].Value), int.Parse(match.Groups[2].Value)),
                match.Groups[3].Value);
        }
    }
}