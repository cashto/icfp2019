﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Solver
{
    public class Board
    {
        public const char Wall = '#';
        public const char Empty = 'O';
        public const char UnpaintedEmpty = 'o';
        public const char Manipulator = 'B';
        public const char FastWheels = 'F';
        public const char Drill = 'L';
        public const char Mystery = 'X';
        public const char Teleport = 'R';

        private char[,] Map { get; set; }
        private int MaxX { get; set; }
        private int MaxY { get; set; }

        public Board(
            List<Point> map,
            List<List<Point>> obstacles,
            List<Tuple<char, Point>> boosts)
        {
            MaxX = map.Max(p => p.X);
            MaxY = map.Max(p => p.Y);
            Map = new char[MaxX, MaxY];

            var segments = GetSegments(map).ToList();
            foreach (var obstacle in obstacles)
            {
                segments.AddRange(GetSegments(obstacle));
            }

            var vertSegments = segments
                .Where(segment => segment.Item1.X == segment.Item2.X)
                .Select(segment => segment.Item1.Y < segment.Item2.Y ? segment : Tuple.Create(segment.Item2, segment.Item1))
                .ToList();

            foreach (var y in Enumerable.Range(0, MaxY))
            {
                var isWall = true;
                foreach (var x in Enumerable.Range(0, MaxX))
                {
                    if (vertSegments.Any(segment =>
                        segment.Item1.X == x && y >= segment.Item1.Y && y < segment.Item2.Y))
                    {
                        isWall = !isWall;
                    }

                    Map[x, y] = isWall ? Wall : UnpaintedEmpty;
                }
            }

            foreach (var boost in boosts)
            {
                Map[boost.Item2.X, boost.Item2.Y] = char.ToLowerInvariant(boost.Item1);
            }
        }

        private Board()
        {
        }

        public Board Clone()
        {
            return new Board()
            {
                MaxX = MaxX,
                MaxY = MaxY,
                Map = (char[,]) Map.Clone()
            };
        }

        public override string ToString()
        {
            return ToString(null);
        }

        public string ToString(Point? mark)
        {
            var ans = new StringBuilder();

            foreach (var y in Enumerable.Range(-1, MaxY + 2))
            {
                foreach (var x in Enumerable.Range(-1, MaxX + 2))
                {
                    var p = new Point(x, MaxY - y - 1);
                    char c = IsInBounds(p) ? Map[p.X, p.Y] : Wall;

                    c = p.Equals(mark) ? '*' :
                        c == UnpaintedEmpty ? ' ' :
                        c == Empty ? '.' :
                        c;

                    ans.Append(c);
                }

                ans.AppendLine();
            }

            return ans.ToString();
        }

        public char Get(Point p)
        {
            return IsInBounds(p) ? char.ToUpperInvariant(Map[p.X, p.Y]) : Wall;
        }

        public void Set(Point p, char c, BoardUndo undo)
        {
            if (IsInBounds(p) && Map[p.X, p.Y] != c)
            {
                undo.points.Add(Tuple.Create(p, Map[p.X, p.Y]));
                Map[p.X, p.Y] = c;
            }
        }

        public void Undo(BoardUndo undo)
        {
            if (undo == null)
            {
                return;
            }

            var reversePoints = undo.points.ToList();
            reversePoints.Reverse();

            foreach (var i in reversePoints)
            {
                Map[i.Item1.X, i.Item1.Y] = i.Item2;
            }
        }

        public bool IsWall(Point p)
        {
            return Get(p) == Wall;
        }

        public bool IsPainted(Point p)
        {
            return IsInBounds(p) && char.IsUpper(Map[p.X, p.Y]);
        }

        public void Paint(Point p, BoardUndo undo)
        {
            Set(p, char.ToUpperInvariant(Get(p)), undo);
        }

        public bool IsInBounds(Point p)
        {
            return p.X >= 0 && p.Y >= 0 && p.X < MaxX && p.Y < MaxY;
        }

        public IEnumerable<Point> AllPoints => GetAllPoints();

        private IEnumerable<Point> GetAllPoints()
        {
            foreach (var y in Enumerable.Range(0, MaxY))
            {
                foreach (var x in Enumerable.Range(0, MaxX))
                {
                    yield return new Point(x, y);
                }
            }
        }

        private static IEnumerable<Tuple<Point, Point>> GetSegments(List<Point> p)
        {
            foreach (var i in Enumerable.Range(0, p.Count))
            {
                yield return Tuple.Create(p[i], p[(i + 1) % p.Count]);
            }
        }
    }

    public class BoardUndo
    {
        internal List<Tuple<Point, char>> points = new List<Tuple<Point, char>>();
        public int Count => points.Count;
    }
}