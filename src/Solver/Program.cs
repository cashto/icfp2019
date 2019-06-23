﻿using Newtonsoft.Json;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlTypes;
using System.Collections;
using System.Data;

namespace Solver
{
    public class Program
    {
        public const int MaxDepth = 2;

        static void Main(string[] args)
        {
            var debug = args.Length == 0;
            if (debug)
            {
                //args = new string[] { "puzzle", @"C:\Users\cashto\Documents\GitHub\icfp2019\work\puzzles\cashto.desc" };
                args = new string[] { @"C:\Users\cashto\Documents\GitHub\icfp2019\problems\prob-080.desc" };
            }

            if (args.Length == 2)
            {
                GenerateMap.GoMain(args[1]);
                return;
            }

            var fileName = args[0];

            var desc = File.ReadAllText(fileName);

            var state = new State(desc);

            Solve(state, debug);
        }

        public static void Solve(State state, bool debug)
        {
            var weights = new Dictionary<Point, int>();
            var middle = new Point(state.Board.MaxX / 2, state.Board.MaxY / 2);
            var startPoint = new Point();
            foreach (var p in state.Board.AllPoints)
            {
                if (!state.Board.IsWall(p) && p.Distance(middle) < startPoint.Distance(middle))
                {
                    startPoint = p;
                }
            }

            state.Board.BreadthFirstSearch(startPoint, (path) => 
                {
                    weights[path.Point] = path.Depth;
                    return false;
                });

            state.Weights = weights;

            while (state.UnpaintedCount > 0)
            {
                var planDebug = true;
                if (debug)
                {
                    Console.WriteLine();
                    Console.WriteLine(state);
                    //planDebug = Console.ReadKey().KeyChar == 'x';
                }

                var reallyUnpainted = state.Board.AllPoints.Count(p => !state.Board.IsWall(p) && !state.Board.IsPainted(p));
                if (reallyUnpainted != state.UnpaintedCount)
                {
                    ; // state.UnpaintedCount = reallyUnpainted;
                }

                var plan = Plan(state, planDebug && debug);
                foreach (var i in plan)
                {
                    Console.Out.Write(i);
                    state = state.MultiMove(i);
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

        public static List<string> Plan(State state, bool debug)
        {
            return
                BoostPlan(state) ??
                PlanC(state, debug) ??
                PlanB(state, debug);
        }

        public static List<string> PlanA(State state, bool debug)
        {
            var middle = new Point(state.Board.MaxX / 2, state.Board.MaxY / 2);
            string moves = "FLWASDQE";
            StateMetadata bestMove = null;

            var metadata = new StateMetadata()
            {
                State = state,
                OriginalState = state
            };

            var transpositionTable = new Dictionary<object, StateMetadata>() { { state.GetHashTuple(), metadata } };
            var priorityQueue = new PriorityQueue<StateMetadata>((rhs, lhs) =>
                lhs.Depth == rhs.Depth ? CompareMetadata(lhs, rhs) : lhs.Depth < rhs.Depth);

            priorityQueue.Push(metadata);

            while (!priorityQueue.IsEmpty())
            {
                var currentMetadata = priorityQueue.Pop();

                if (bestMove != null && bestMove.State.UnpaintedCount == 0 ||
                    currentMetadata.Depth >= MaxDepth)
                {
                    if (bestMove.State.UnpaintedCount == bestMove.OriginalState.UnpaintedCount)
                    {
                        return null;
                    }

                    return bestMove.ToList().Select(i => i.Move).ToList();
                }

                foreach (var move in moves.Where(m => currentMetadata.Depth == 0 || m != 'F' && m != 'L'))
                {
                    var newState = currentMetadata.State.Move(move);

                    if (newState != null)
                    {
                        var undo = newState.Item2;
                        var score = undo == null ? 0 : undo.Points.Sum(i => {
                            var s = 0;
                            newState.Item1.Weights.TryGetValue(i.Item1, out s);
                            return s;
                        });

                        var newMetadata = new StateMetadata()
                        {
                            Score = currentMetadata.Score + score,
                            State = newState.Item1,
                            OriginalState = currentMetadata.OriginalState,
                            Depth = currentMetadata.Depth + 1,
                            Move = move.ToString(),
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
                            if (transpositionTable.TryGetValue(newMetadata.State.GetHashTuple(), out oldMetadata))
                            {
                                //transpositionString = " is a transposition for " + string.Join("", oldMetadata.ToList().Select(i => i.Move));
                            }

                            if (isBetterMove)
                            {
                                Console.WriteLine($"{newMetadata}{transpositionString}{betterMoveString}");
                            }
                        }

                        if (!transpositionTable.ContainsKey(newState.Item1.GetHashTuple()))
                        {
                            newMetadata.State.Board = newMetadata.State.Board.Clone();
                            priorityQueue.Push(newMetadata);
                            transpositionTable[newMetadata.State] = null;
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

        public static List<string> PlanB(State state, bool debug)
        {
            string moves = "WASD";

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
                var currentBoard = currentMetadata.State.Board;
                var currentPosition = currentMetadata.State.Position;

                if (!currentBoard.IsWall(currentPosition) && !currentBoard.IsPainted(currentPosition))
                {
                    return currentMetadata.ToList().Select(i => i.Move).ToList();
                }

                foreach (var move in moves)
                {
                    var newState = currentMetadata.State.Move(move);

                    if (newState != null)
                    {
                        var newMetadata = new StateMetadata()
                        {
                            State = newState.Item1,
                            OriginalState = currentMetadata.OriginalState,
                            Depth = currentMetadata.Depth + 1,
                            Move = move.ToString(),
                            PreviousState = currentMetadata
                        };

                        newMetadata.State.Board.Undo(newState.Item2);

                        if (!transpositionTable.ContainsKey(newState.Item1))
                        {
                            priorityQueue.Push(newMetadata);
                            transpositionTable[newMetadata.State] = null;
                        }
                    }
                }
            }

            throw new Exception("No moves found");
        }

        public static List<string> PlanC(State state, bool debug)
        {
            state = state.Clone();
            state.Board = state.Board.Clone();

            var points = new List<Point>();
            var board = state.Board;
            Point? boostPoint = null;

            board.BreadthFirstSearch(
                state.Position,
                (path) =>
                {
                    if ("BFL".Contains(board.Get(path.Point)))
                    {
                        boostPoint = path.Point;
                        return true;
                    }

                    if (path.Depth > 4)
                    {
                        return true;
                    }

                    if (!board.IsPainted(path.Point))
                    {
                        points.Add(path.Point);
                    }

                    return false;
                });

            if (boostPoint != null)
            {
                return GeneratePath(state, boostPoint.Value).Item1;
            }

            var ans = new List<string>();
            while (points.Any())
            {
                var point = GetClosestPoint(state.Position, points);
                points.Remove(point);

                if (!state.Board.IsPainted(point))
                {
                    var result = GeneratePath(state, point);
                    ans.AddRange(result.Item1);
                    state = result.Item2;
                }
            }

            if (!ans.Any())
            {
                return null;
            }

            ans.Add("Q");
            return ans;
        }

        private static Tuple<List<string>, State> GeneratePath(State state, Point target)
        {
            var ans = new List<string>();

            while (state.Position != target)
            {
                var path = state.Board.PathFind(state.Position, target);
                var p = path[1];
                var move =
                    p.X < state.Position.X ? "A" :
                    p.X > state.Position.X ? "D" :
                    p.Y < state.Position.Y ? "S" :
                    "W";

                ans.Add(move);
                state = state.Move(move[0]).Item1;
            }

            return Tuple.Create(ans, state);
        }

        private static Point GetClosestPoint(Point target, IEnumerable<Point> points)
        {
            var ans = points.First();

            foreach (var point in points.Skip(1))
            {
                if (point.Distance(target) < ans.Distance(target))
                {
                    ans = point;
                }
            }

            return ans;
        }

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

        // Heuristics:
        //    Maximize boosts collected.
        //    Minimize nearby unpainted squares.
        //    Maximize score unpainted squares.
        //
        private static IEnumerable<int> GetMetrics(StateMetadata metadata)
        {
            yield return -metadata.State.BoostsCollected;
            //yield return metadata.NearbyUnpaintedCells.Value;
            //yield return metadata.State.UnpaintedCount;
            yield return -metadata.Score;
        }

        public static List<string> BoostPlan(State state)
        {
            if (state.Boosts.Contains(Board.Manipulator))
            {
                var y = (state.Robot.Count + 1) / 2 * (state.Robot.Count % 2 == 0 ? -1 : 1);
                var point = new Point(1, y);
                foreach (var i in Enumerable.Range(0, state.Direction))
                {
                    point = point.RotateRight();
                }

                return new List<string>() { "B" + point.ToString() };
            }

            if (state.Boosts.Contains(Board.FastWheels) && state.FastWheelsTime == 0)
            {
                //return new List<string>() { "F" };
            }

            return null;
        }
    }

    public class StateMetadata
    {
        public State State { get; set; }
        public State OriginalState { get; set; }
        public int Depth { get; set; }
        public int Score { get; set; }
        public StateMetadata PreviousState { get; set; }
        public string Move { get; set; }
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
            return $"{moves} Depth={Depth} Remain={State.UnpaintedCount} Score={Score}";
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

            foreach (var dy in Enumerable.Range(-1, 3))
            {
                foreach (var dx in Enumerable.Range(1, 3))
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
    }
}