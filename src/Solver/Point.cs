using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Solver
{ 
    public struct Point
    {
        public static readonly List<Point> AdjacentPoints = new List<Point>()
        {
            new Point(1, 0),
            new Point(0, -1),
            new Point(-1, 0),
            new Point(0, 1)
        };

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

        public static bool operator ==(Point lhs, Point rhs)
        {
            return lhs.X == rhs.X && lhs.Y == rhs.Y;
        }
        public static bool operator !=(Point lhs, Point rhs)
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

        public Point AdjacentPoint(int direction)
        {
            return this + AdjacentPoints[direction % 4];
        }

        public int X { get; set; }

        public int Y { get; set; }
    }

}
