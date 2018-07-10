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
            return a.Equals(b);
        }

        public static bool operator !=(Vector2 a, Vector2 b)
        {
            return !a.Equals(b);
        }

        public override bool Equals(object b)
        {
            if (b == null) return false;
            if (b.GetType() != typeof(Vector2)) return false;
            Vector2 bv = (Vector2) b;
            return (X == bv.X && Y == bv.Y);
        }

        public override int GetHashCode()
        {
            int x = BitConverter.ToInt32(BitConverter.GetBytes(X), 0);
            int y = BitConverter.ToInt32(BitConverter.GetBytes(Y), 0);
            return x * y;
        }
    }
}
