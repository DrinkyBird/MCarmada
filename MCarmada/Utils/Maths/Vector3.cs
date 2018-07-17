using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;
using System.Text;

namespace MCarmada.Utils.Maths
{
    public struct Vector3
    {
        public float X, Y, Z;

        public Vector3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public override string ToString()
        {
            return "[" + X + ", " + Y + ", " + Z + "]";
        }

        public static bool operator ==(Vector3 a, Vector3 b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(Vector3 a, Vector3 b)
        {
            return !a.Equals(b);
        }

        public override bool Equals(object b)
        {
            if (b == null) return false;
            if (b.GetType() != typeof(Vector3)) return false;
            Vector3 bv = (Vector3)b;
            return (X == bv.X && Y == bv.Y && Z == bv.Z);
        }

        public override int GetHashCode()
        {
            int x = BitConverter.ToInt32(BitConverter.GetBytes(X), 0);
            int y = BitConverter.ToInt32(BitConverter.GetBytes(Y), 0);
            int z = BitConverter.ToInt32(BitConverter.GetBytes(Z), 0);
            return x * y * z;
        }

        public static float Distance(Vector3 a, Vector3 b)
        {
            float dx = b.X - a.X;
            float dy = b.Y - a.Y;
            float dz = b.Z - a.Z;

            float distance = (float) Math.Sqrt(dx * dx + dy * dy + dz * dz);
            return distance;
        }
    }
}
