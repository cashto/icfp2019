using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Solver
{
    public class GenerateMap
    {
        public static string Generate(MapSpecification spec)
        {
            var board = GenerateBoard(spec);
            var startingPoint = board.AllPoints.First(p => !board.IsWall(p));
            var edges = RemoveDuplicates(WalkEdge(board, startingPoint, 0)).ToList();

            foreach (var i in edges)
            {
                //Console.WriteLine(i);
            }

            Console.WriteLine();
            Console.WriteLine("Removing collinear points");
            Console.WriteLine();

            edges = RemoveCollinearPoints(edges).ToList();

            foreach (var i in edges)
            {
                //Console.WriteLine(i);
            }

            var map = string.Join(",", edges.Select(p => p.ToString()));
            return $"{map}#{startingPoint}##";
        }

        public static Board GenerateBoard(MapSpecification spec)
        {
            var random = new Random();

            var board = new Board(spec.Size, spec.Size);

            var fill = spec.Size * spec.Size / 5;

            var middle = new Point(spec.Size / 2, spec.Size / 2);

            foreach (var i in Enumerable.Range(0, fill))
            {
                var allowedPoints = board.AllPoints
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
        public List<Point> IncludePoints { get; set; }
        public List<Point> ExcludePoints { get; set; }

        public MapSpecification()
        {
            IncludePoints = new List<Point>();
            ExcludePoints = new List<Point>();
        }
    }
}
