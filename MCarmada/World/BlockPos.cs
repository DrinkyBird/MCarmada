using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MCarmada.World
{
    public struct BlockPos
    {
        public int X;
        public int Y;
        public int Z;

        public BlockPos(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public override string ToString()
        {
            return "[" + X + ", " + Y + ", " + Z + "]";
        }

        public BlockPos Add(int n)
        {
            return new BlockPos(X + n, Y + n, Z + n);
        }

        public static BlockPos operator +(BlockPos p, int n)
        {
            return p.Add(n);
        }
    }
}
