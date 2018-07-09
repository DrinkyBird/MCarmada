using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MCarmada.Utils.Maths
{
    public struct Vector2
    {
        public float X, Y;

        public Vector2(float x, float y)
        {
            X = x;
            Y = y;
        }

        public override string ToString()
        {
            return "[" + X + ", " + Y + "]";
        }

        public static bool operator ==(Vector2 a, Vector2 b)
        {
            return (a.X == b.X && a.Y == b.Y);
        }

        public static bool operator !=(Vector2 a, Vector2 b)
        {
            return (a.X != b.X || a.Y != b.Y);
        }
    }
}
