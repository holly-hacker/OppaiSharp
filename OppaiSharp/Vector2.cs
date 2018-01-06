using System;

namespace OppaiSharp
{
    public struct Vector2
    {
        public double X, Y;

        public double Length => Math.Sqrt(X * X + Y * Y);

        public Vector2(double v) => X = Y = v;

        public Vector2(double x, double y)
        {
            X = x;
            Y = y;
        }

        public Vector2(Vector2 v)
        {
            X = v.X;
            Y = v.Y;
        }

        public static Vector2 operator -(Vector2 a, Vector2 b) => new Vector2(a.X - b.X, a.Y - b.Y);
        public static Vector2 operator *(Vector2 a, double b) => new Vector2(a.X * b, a.Y * b);

        public override string ToString() => $"({X}, {Y})";
    }
}
