using Newtonsoft.Json;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlTypes;
using System.Collections;
using System.Data;
using System.Runtime.InteropServices;
using System.CodeDom;
using System.IO.Pipes;

namespace Solver
{
    public class Program
    {
        public const int MaxDepth = 4;

        static void Main(string[] args)
        {
            var debug = args.Length == 0;
            if (debug)
            {
                //args = new string[] { "puzzle", @"C:\Users\cashto\Documents\GitHub\icfp2019\work\puzzles\cashto.desc" };
                args = new string[] { @"C:\Users\cashto\Documents\GitHub\icfp2019\problems\prob-211.desc" };
            }

            if (args.Length == 2 && args[1] == "puzzle")
            {
                GenerateMap.GoMain(args[1]);
                return;
            }

            var fileName = args[0];

            var desc = File.ReadAllText(fileName);

            var state = new State(desc);

            if (args.Length == 2)
            {
                foreach (var c in args[1])
                {
                    state.Boosts.Add(c);
                    state.Boosts.Add(c);
                    state.Boosts.Add(c);
                }
            }

            Solve(state, debug);
        }

        public static void Solve(State state, bool debug)
        {
            State.OriginalUnpaintedCount = state.UnpaintedCount;

            state.Priority[state.Position] = 0;
            state.Board.BreadthFirstSearch(
                state.Position,
                (path) =>
                {
                    state.Priority[path.Point] = path.Depth;
                    return false;
                });

            var moves = 0;
            while (state.UnpaintedCount > 0)
            {
                var planDebug = false;
                if (debug)
                {
                    Console.WriteLine();
                    Console.WriteLine($"{moves} {state}");
                    //Console.WriteLine(state.Board);
                    //planDebug = Console.ReadKey().KeyChar == 'x';
                }

                var plan = Plan(state, planDebug && debug);
                moves += plan.Count;
                var planString = string.Join("", plan);
                Console.Write(planString);
                state = state.MultiMove(planString);
            }
        }

        public static List<string> Plan(State state, bool debug)
        {
            return
                BoostPlan(state) ??
                PlanA(state, debug) ??
                PlanD(state, debug);
        }

        public static List<string> PlanA(State state, bool debug)
        {
            var middle = new Point(state.Board.MaxX / 2, state.Board.MaxY / 2);
            string moves = "FWASDQE";
            StateMetadata bestMove = null;

            var metadata = new StateMetadata() { State = state, Redo = new BoardUndo(), Debug = state.Board.Clone() };

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
                    if (bestMove.State.UnpaintedCount == state.UnpaintedCount)
                    {
                        return null;
                    }

                    return bestMove.ToList().Select(i => i.Move).ToList();
                }

                var undoBase = currentMetadata.State.Board.Redo(currentMetadata.Redo);
                //currentMetadata.State.Board.Compare(currentMetadata.Debug);

                foreach (var move in moves.Where(m => currentMetadata.Depth == 0 || m != 'F' && m != 'L'))
                {
                    var afterMove = currentMetadata.State.Move(move);

                    if (afterMove != null)
                    {
                        var newState = afterMove.Item1;
                        var undoMove = afterMove.Item2;

                        var newPoints = undoMove == null ? 0 : undoMove.Points.Sum(p => 1 + GetWallCount(state.Board, p.Item1));

                        var newMetadata = new StateMetadata()
                        {
                            Score = currentMetadata.Score + newPoints,
                            State = newState,
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
                            if (isBetterMove)
                            {
                                Console.WriteLine($"{newMetadata}{betterMoveString}");
                            }
                        }

                        if (!transpositionTable.ContainsKey(newState.GetHashTuple()))
                        {
                            //newMetadata.Debug = state.Board.Clone();
                            newMetadata.Redo = new BoardUndo();
                            newMetadata.Redo.Points.AddRange(currentMetadata.Redo.Points);
                            newMetadata.Redo.Points.AddRange(state.Board.GetRedo(undoMove).Points);
                            priorityQueue.Push(newMetadata);
                            transpositionTable[newMetadata.State] = null;
                        }

                        currentMetadata.State.Board.Undo(undoMove);
                    }
                }

                currentMetadata.State.Board.Undo(undoBase);
            }

            throw new Exception("No moves found");
        }

        private static int GetWallCount(Board board, Point point)
        {
            return point.AdjacentPoints().Count(p => board.IsWall(p));
        }

        public static List<string> PlanB(State state, bool debug, Func<StateMetadata, bool> terminatingCondition)
        {
            string moves = "WASD";
            var transpositionTable = new HashSet<Point>() { state.Position };
            var priorityQueue = new PriorityQueue<StateMetadata>((rhs, lhs) =>
                lhs.Depth == rhs.Depth ? CompareMetadata(lhs, rhs) : lhs.Depth < rhs.Depth);

            var initialState = new StateMetadata() { State = state };
            priorityQueue.Push(initialState);

            while (!priorityQueue.IsEmpty())
            {
                var currentMetadata = priorityQueue.Pop();
                var currentBoard = currentMetadata.State.Board;
                var currentPosition = currentMetadata.State.Position;

                if (terminatingCondition(currentMetadata))
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
                            Depth = currentMetadata.Depth + 1,
                            Move = move.ToString(),
                            PreviousState = currentMetadata
                        };

                        newMetadata.State.Board.Undo(newState.Item2);

                        if (!transpositionTable.Contains(newState.Item1.Position))
                        {
                            priorityQueue.Push(newMetadata);
                            transpositionTable.Add(newMetadata.State.Position);
                        }
                    }
                }
            }

            return null;
        }

        public static List<string> PlanC(State state, bool debug)
        {
            var board = state.Board.Clone();
            var ans = PlanCImpl(state, debug);
            state.Board = board;
            return ans;
        }

        public static List<string> PlanCImpl(State state, bool debug)
        {
            var ans = new List<string>();

            var planA = PlanA(state, debug);
            if (planA != null)
            {
                ans.AddRange(planA);
                state = state.MultiMove(string.Join("", planA));
            }

            var sections = new List<HashSet<Point>>();
            foreach (var point in state.Board.AllPoints.Where(p => !state.Board.IsWall(p)))
            {
                if (!state.Board.IsPainted(point) && 
                    !sections.Any(section => section.Contains(point)))
                {
                    var newSection = new HashSet<Point>() { point };
                    state.Board.BreadthFirstSearch(point,
                        (path) => { newSection.Add(path.Point); return false; },
                        (path) => state.Board.IsWall(path.Point) || state.Board.IsPainted(path.Point));
                    sections.Add(newSection);
                }
            }

            var smallestSection = sections
                .OrderBy(section => section.Count)
                .ThenBy(section => state.Position.Distance(section.First()))
                .FirstOrDefault();
            if (sections.Count > 1)
            {
                var planB = PlanB(state, debug, (meta) => smallestSection.Contains(meta.State.Position));
                if (planB != null)
                {
                    ans.AddRange(planB);
                }
            }

            return ans.Any() ? ans : null;
        }

        public static List<string> PlanD(State state, bool debug)
        {
            var board = state.Board.Clone();

            var ans = PlanB(state, debug, (meta) => !meta.State.Board.IsWall(meta.State.Position) && !meta.State.Board.IsPainted(meta.State.Position));
            var newState = state.MultiMove(string.Join("", ans));
            state.Board = board;

            if (state.Boosts.Contains(Board.Drill))
            {
                var move = $"L";
                var altState = state.MultiMove(move);

                var searchSucceeded = true;
                var altPath = PlanB(altState, debug, (meta) => {
                    if (meta.Depth == ans.Count)
                    {
                        searchSucceeded = false;
                        return true;
                    }
                    return meta.State.Position == newState.Position;
                });

                if (altPath != null && searchSucceeded && altPath.Count + 10 < ans.Count)
                {
                    ans = new List<string>() { move };
                    ans.AddRange(altPath);
                }
            }

            foreach (var tele in state.Teleports)
            {
                var move = $"T{tele}";
                var altState = state.MultiMove(move);
                var searchSucceeded = true;

                var altPath = PlanB(altState, debug, (meta) => {
                    if (meta.Depth == ans.Count)
                    {
                        searchSucceeded = false;
                        return true;
                    }
                    return meta.State.Position == newState.Position;
                });

                if (altPath != null && searchSucceeded && altPath.Count + 1 < ans.Count)
                {
                    ans = new List<string>() { move };
                    ans.AddRange(altPath);
                }
            }

            return ans;
        }

        private static bool IsAbandoned(Board board, Point p)
        {
            int count = 0;
            return board.BreadthFirstSearch(
                p,
                (path) => ++count == 5,
                (path) => board.IsPainted(path.Point)) == null;
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
        //    Maximize score unpainted squares.
        //
        private static IEnumerable<int> GetMetrics(StateMetadata metadata)
        {
            yield return -metadata.State.BoostsCollected;
            yield return -metadata.Score;
        }

        public static List<string> BoostPlan(State state)
        {
            // Add manipulator if we got one.
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

            // Find any manipulators or teleports within a certain range
            var foundBoost = false;
            var plan = PlanB(state, false, (meta) =>
            {
                if ("BR".Contains(meta.State.Board.Get(meta.State.Position)))
                {
                    foundBoost = true;
                }

                return foundBoost || meta.Depth == 20;
            });

            if (foundBoost)
            {
                return plan;
            }

            // Maybe place a teleport?
            var maxX = state.Board.MaxX;
            var maxY = state.Board.MaxY;
            var position = state.Position;
            var n = state.Teleports.Count + 1;
            var d = state.Teleports.Count + 3;
            var size = new Point(n * maxX / d, n * maxY / d);
            var p1 = new Point((maxX - size.X) / 2, (maxY - size.Y) / 2);
            var p2 = p1 + size;

            var inCenter =
                position.X >= p1.X && position.X < p2.X &&
                position.Y >= p1.Y && position.Y < p2.Y;

            if (inCenter &&
                state.Boosts.Contains(Board.Teleport) &&
                state.Teleports.All(tele => position.Distance(tele) > 1 * (maxX + maxY) / 6))
            {
                return new List<string>() { "R" };
            }

            return null;
        }
    }

    public class StateMetadata
    {
        public State State { get; set; }
        public int Depth { get; set; }
        public int Score { get; set; }
        public StateMetadata PreviousState { get; set; }
        public string Move { get; set; }
        public BoardUndo Redo { get; set; }
        public Board Debug { get; set; }

        public StateMetadata()
        {
        }

        public override string ToString()
        {
            var moves = string.Join("", ToList().Select(i => i.Move));
            return $"{moves} Depth={Depth} Remain={State.UnpaintedCount} Score={Score}";
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