using Newtonsoft.Json;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Solver
{
    public class Program
    {
        public const int MaxDepth = 6;

        static void Main(string[] args)
        {
            var fileName = args[0];

            var desc = File.ReadAllText(fileName);

            var state = new State(desc);

            Solve(state, debug: false);
        }

        public static void Solve(State state, bool debug)
        {
            while (state.UnpaintedCount > 0)
            {
                var planDebug = false;
                if (debug)
                {
                    Console.WriteLine();
                    Console.WriteLine(state);
                    planDebug = Console.ReadKey().KeyChar == 'x';
                }

                var plan = Plan(state, planDebug);
                foreach (var i in plan.Take(Math.Max(MaxDepth, plan.Count) - 2))
                {
                    Console.Out.Write(i.Move);
                    state = state.Move(i.Move).Item1;
                }
            }
        }

        static void Play(State state)
        { 
            while (true)
            {
                Console.WriteLine();
                Console.Write(state);

                char ch = char.ToUpperInvariant(Console.ReadKey().KeyChar);
                if (ch == '\n')
                {
                    return;
                }

                state = state.MultiMove(ch.ToString());
            }
        }

        public static List<StateMetadata> Plan(State state, bool debug)
        {
            string moves = "FLWASDQE";
            StateMetadata bestMove = null;

            var metadata = new StateMetadata()
            {
                State = state,
                OriginalState = state
            };

            var transpositionTable = new Dictionary<State, StateMetadata>() { { state, metadata } };
            var priorityQueue = new PriorityQueue<StateMetadata>((rhs, lhs) =>
                lhs.Depth == rhs.Depth ? CompareMetadata(lhs, rhs) : lhs.Depth < rhs.Depth);

            priorityQueue.Push(metadata);

            while (!priorityQueue.IsEmpty())
            {
                var currentMetadata = priorityQueue.Pop();

                if (bestMove != null && bestMove.State.UnpaintedCount == 0 ||
                    currentMetadata.Depth >= MaxDepth && bestMove.State.UnpaintedCount < state.UnpaintedCount)
                {
                    return bestMove.ToList();
                }

                foreach (var move in moves.Where(m => currentMetadata.Depth == 0 || m != 'F' && m != 'L'))
                {
                    var newState = currentMetadata.State.Move(move);

                    if (newState != null)
                    {
                        var newMetadata = new StateMetadata()
                        {
                            State = newState.Item1,
                            OriginalState = currentMetadata.OriginalState,
                            Depth = currentMetadata.Depth + 1,
                            Move = move,
                            PreviousState = currentMetadata
                        };

                        var isBetterMove = bestMove == null || CompareMetadata(newMetadata, bestMove);
                        if (isBetterMove)
                        {
                            bestMove = newMetadata;
                        }

                        if (debug)
                        {
                            var betterMoveString = isBetterMove ? " *" : "";

                            var transpositionString = "";
                            StateMetadata oldMetadata = null;
                            if (transpositionTable.TryGetValue(newMetadata.State, out oldMetadata))
                            {
                                transpositionString = " is a transposition for " + string.Join("", oldMetadata.ToList().Select(i => i.Move));
                            }

                            if (isBetterMove)
                            {
                                Console.WriteLine($"{newMetadata}{transpositionString}{betterMoveString}");
                            }
                        }

                        if (!transpositionTable.ContainsKey(newState.Item1))
                        {
                            newMetadata.State.Board = newMetadata.State.Board.Clone();
                            priorityQueue.Push(newMetadata);
                            transpositionTable[newMetadata.State] = newMetadata;
                        }
                    }

                    if (newState != null)
                    {
                        currentMetadata.State.Board.Undo(newState.Item2);
                    }
                }
            }

            throw new Exception("No moves found");
        }

        // Heuristics:
        //    Maximize boosts collected.
        //    Minimize nearby unpainted squares.
        //    Minimize unpainted squares.
        //
        private static bool CompareMetadata(StateMetadata lhs, StateMetadata rhs)
        {
            foreach (var metric in GetMetrics(lhs).Zip(GetMetrics(rhs), (l, r) => l - r))
            {
                if (metric != 0)
                {
                    return metric < 0;
                }
            }

            return false;
        }

        private static IEnumerable<int> GetMetrics(StateMetadata metadata)
        {
            yield return -metadata.State.BoostsCollected;
            yield return metadata.NearbyUnpaintedCells.Value;
            yield return metadata.State.UnpaintedCount;
        }
    }

    public class StateMetadata
    {
        public State State { get; set; }
        public State OriginalState { get; set; }
        public int Depth { get; set; }
        public StateMetadata PreviousState { get; set; }
        public char Move { get; set; }
        public Lazy<int> ClosestUnpaintedCellDistance { get; private set; }
        public Lazy<int> NearbyUnpaintedCells { get; private set; }

        public StateMetadata()
        {
            ClosestUnpaintedCellDistance = new Lazy<int>(CalculateDistance);
            NearbyUnpaintedCells = new Lazy<int>(CalculateNearbyUnpaintedCells);
        }

        public override string ToString()
        {
            var moves = string.Join("", ToList().Select(i => i.Move));
            return $"{moves} Depth={Depth} Remain={State.UnpaintedCount} Nearby={NearbyUnpaintedCells.Value}";
        }

        private int CalculateDistance()
        {
            var bestDistance = -1;
            var board = State.Board;
            var allUnpaintedPoints = board.AllPoints.Where(p => !board.IsWall(p) && !board.IsPainted(p));

            foreach (var p in allUnpaintedPoints)
            {
                var d = State.Position.Distance(p);
                if (bestDistance < 0 || d < bestDistance)
                {
                    bestDistance = d;
                }
            }

            return bestDistance;
        }

        private int CalculateNearbyUnpaintedCells()
        {
            var paintedCells = 0;
            var board = State.Board;

            foreach (var dy in Enumerable.Range(-3, 7))
            {
                foreach (var dx in Enumerable.Range(-3, 7))
                {
                    var p = OriginalState.Position + new Point(dx, dy);
                    if (!board.IsWall(p) && !board.IsPainted(p))
                    {
                        ++paintedCells;
                    }
                }
            }

            return paintedCells;
        }

        public List<StateMetadata> ToList()
        {
            var ans = new List<StateMetadata>();

            var p = this;
            while (p.PreviousState != null)
            {
                ans.Add(p);
                p = p.PreviousState;
            }

            ans.Reverse();
            return ans;
        }

        public char GetMove()
        {
            return ToList().First().Move;
        }
    }

    public struct Point
    {
        public Point(int x, int y)
        {
            X = x;
            Y = y;
        }

        public static Point operator +(Point lhs, Point rhs)
        {
            return new Point(lhs.X + rhs.X, lhs.Y + rhs.Y);
        }

        public static Point operator -(Point lhs, Point rhs)
        {
            return new Point(lhs.X - rhs.X, lhs.Y - rhs.Y);
        }

        public override string ToString()
        {
            return $"({X},{Y})";
        }

        public override bool Equals(object obj)
        {
            if (obj is Point)
            {
                var other = (Point)obj;
                return this == other;
            }

            return false;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public static bool operator== (Point lhs, Point rhs)
        {
            return lhs.X == rhs.X && lhs.Y == rhs.Y;
        }
        public static bool operator!=(Point lhs, Point rhs)
        {
            return !(lhs == rhs);
        }

        public Point RotateRight()
        {
            return new Point(Y, -X);
        }

        public Point RotateLeft()
        {
            return new Point(-Y, X);
        }

        public int Distance(Point other)
        {
            var d = this - other;
            return Math.Abs(d.X) + Math.Abs(d.Y);
        }

        public int X { get; set; }

        public int Y { get; set; }
    }
}