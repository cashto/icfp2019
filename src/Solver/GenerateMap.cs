using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Solver
{
    public class GenerateMap
    {
        public static void GoMain(string fileName)
        {
            var spec = MapSpecification.FromString(Regex.Replace(File.ReadAllText(fileName), @"\r?\n?", ""));
            Console.WriteLine(Generate(spec));
        }

        public static string Generate(MapSpecification spec)
        {
            var board = GenerateBoard(spec);

            board = ConnectObstacles(board, spec);

            var startingPoint = board.AllPoints.First(p => !board.IsWall(p));
            var edges = RemoveDuplicates(WalkEdge(board, startingPoint, 0)).ToList();
            edges = RemoveCollinearPoints(edges).ToList();

            Validate(edges, spec);

            var map = string.Join(",", edges.Select(p => p.ToString()));
            var boosts = GenerateBoosts(board, spec);

            return $"{map}#{startingPoint}##{boosts}";
        }

        private static void Validate(List<Point> edges, MapSpecification spec)
        {
            if (edges.Count > spec.MaxVertexes)
            {
                throw new Exception("Solution has too many edges");
            }

            if (edges.Count < spec.MinVertexes)
            {
                throw new Exception("Solution has too few edges");
            }

            var newBoard = new Board(edges, new List<List<Point>>(), new List<Tuple<char, Point>>());
            if (spec.IncludePoints.Any(p => newBoard.IsWall(p)))
            {
                throw new Exception("Include point is a wall");
            }

            if (spec.ExcludePoints.Any(p => !newBoard.IsWall(p)))
            {
                throw new Exception("Exclude point is not a wall");
            }
        }

        public static Board ConnectObstacles(Board board, MapSpecification spec)
        {
            var startingPoint = board.AllPoints.First(p => !board.IsWall(p));
            var edges = RemoveDuplicates(WalkEdge(board, startingPoint, 0)).ToList();
            edges = RemoveCollinearPoints(edges).ToList();
            var newBoard = new Board(edges, new List<List<Point>>(), new List<Tuple<char, Point>>());

            foreach (var p in board.AllPoints)
            {
                if (board.IsWall(p) && !newBoard.IsWall(p))
                {
                    var pathToWall = newBoard.PathFindToWall(p, spec.IncludePoints);
                    if (pathToWall != null)
                    {
                        foreach (var p2 in pathToWall)
                        {
                            newBoard.Set(p2, Board.Wall, new BoardUndo());
                        }
                    }
                }
            }

            return newBoard;
        }

        public static Board GenerateBoard(MapSpecification spec)
        {
            var random = new Random();

            var board = new Board(spec.Size, spec.Size);

            var fill = 100;//  spec.Size * spec.Size / 3;

            var middle = new Point(spec.Size / 2, spec.Size / 2);

            foreach (var p in spec.ExcludePoints)
            {
                board.Set(p, Board.Wall, new BoardUndo());
            }

            foreach (var i in Enumerable.Range(0, fill))
            {
                var allowedPoints = board.AllPoints
                    .Where(p => !spec.IncludePoints.Contains(p))
                    .Where(p => !board.IsWall(p) && Point.AdjacentPoints.Any(dir => board.IsWall(p + dir)))
                    .ToList();

                while (allowedPoints.Any())
                {
                    var index = random.Next() % allowedPoints.Count;
                    var chosenPoint = allowedPoints[index];

                    var undo = new BoardUndo();
                    board.Set(chosenPoint, Board.Wall, undo);

                    if (Point.AdjacentPoints.All(dir =>
                        board.IsWall(chosenPoint + dir) || board.PathFind(chosenPoint + dir, middle) != null))
                    {
                        break;
                    }

                    board.Undo(undo);
                    allowedPoints.RemoveAt(index);
                }

                if (!allowedPoints.Any())
                {
                    break;
                }
            }

            return board;
        }

        private static string GenerateBoosts(Board board, MapSpecification spec)
        {
            var boosts = new List<string>();

            boosts.AddRange(GenerateBoosts(board, spec.ManipulatorCount, Board.Manipulator));
            boosts.AddRange(GenerateBoosts(board, spec.FastWheelsCount, Board.FastWheels));
            boosts.AddRange(GenerateBoosts(board, spec.DrillCount, Board.Drill));
            boosts.AddRange(GenerateBoosts(board, spec.TeleportCount, Board.Teleport));
            boosts.AddRange(GenerateBoosts(board, spec.CloneCount, Board.ClonePoint));
            boosts.AddRange(GenerateBoosts(board, spec.MysteryCount, Board.Mystery));

            return string.Join(";", boosts);
        }

        private static IEnumerable<string> GenerateBoosts(Board board, int count, char c)
        {
            var random = new Random();

            foreach (var i in Enumerable.Range(0, count))
            {
                while (true)
                {
                    var p = new Point(random.Next() % board.MaxX, random.Next() % board.MaxY);
                    if (board.Get(p) == Board.Empty)
                    {
                        board.Set(p, c, new BoardUndo());
                        yield return $"{c}{p}";
                        break;
                    }
                }
            }
        }

        private static readonly List<Point> Corners = new List<Point>()
        {
            new Point(0, 0),
            new Point(0, 1),
            new Point(1, 1),
            new Point(1, 0)
        };

        private static IEnumerable<Point> WalkEdge(
            Board board,
            Point startingPoint,
            int startingDirection)
        {
            var point = startingPoint;
            var direction = startingDirection;

            do
            {
                yield return point + Corners[direction];

                var right = point.AdjacentPoint(direction + 1);
                var forward = point.AdjacentPoint(direction);
                if (!board.IsWall(right))
                {
                    direction = (direction + 1) % 4;
                    point = right;
                }
                else if (board.IsWall(forward))
                {
                    direction = (direction + 3) % 4;
                }
                else
                {
                    point = forward;
                }
            } while (point != startingPoint || direction != startingDirection);
        }

        private static IEnumerable<Point> RemoveDuplicates(IEnumerable<Point> points)
        {
            var prev = points.Last();

            foreach (var point in points)
            {
                if (point != prev)
                {
                    yield return point;
                }

                prev = point;
            }
        }

        private static IEnumerable<Point> RemoveCollinearPoints(List<Point> points)
        {
            foreach (var i in Enumerable.Range(0, points.Count))
            {
                var last = points[(i - 1 + points.Count) % points.Count];
                var current = points[i];
                var next = points[(i + 1) % points.Count];

                var isCollinear =
                    last.X == current.X && current.X == next.X ||
                    last.Y == current.Y && current.Y == next.Y;

                if (!isCollinear)
                {
                    yield return current;
                }
            }
        }
    }

    public class MapSpecification
    {
        public int BlockId { get; set; }
        public int Epoch { get; set; }
        public int Size { get; set; }
        public int MinVertexes { get; set; }
        public int MaxVertexes { get; set; }
        public int ManipulatorCount { get; set; }
        public int FastWheelsCount { get; set; }
        public int DrillCount { get; set; }
        public int TeleportCount { get; set; }
        public int CloneCount { get; set; }
        public int MysteryCount { get; set; }
        public HashSet<Point> IncludePoints { get; set; }
        public HashSet<Point> ExcludePoints { get; set; }

        public MapSpecification()
        {
            IncludePoints = new HashSet<Point>();
            ExcludePoints = new HashSet<Point>();
        }

        public static MapSpecification FromString(string s)
        {
            var fields = s.Split('#');
            var fields0 = fields[0].Split(',');

            return new MapSpecification()
            {
                BlockId = int.Parse(fields0[0]),
                Epoch = int.Parse(fields0[1]),
                Size = int.Parse(fields0[2]),
                MinVertexes = int.Parse(fields0[3]),
                MaxVertexes = int.Parse(fields0[4]),
                ManipulatorCount = int.Parse(fields0[5]),
                FastWheelsCount = int.Parse(fields0[6]),
                DrillCount = int.Parse(fields0[7]),
                TeleportCount = int.Parse(fields0[8]),
                CloneCount = int.Parse(fields0[9]),
                MysteryCount = int.Parse(fields0[10]),
                IncludePoints = new HashSet<Point>(State.ParseList<Point>(fields[1], ',', State.ParsePoint)),
                ExcludePoints = new HashSet<Point>(State.ParseList<Point>(fields[2], ',', State.ParsePoint))
            };
        }
    }
}
