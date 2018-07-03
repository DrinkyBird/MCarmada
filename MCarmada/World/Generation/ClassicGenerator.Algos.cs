using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Extensions.Data;

namespace MCarmada.World.Generation
{
    // Most of this class is taken from ClassicalSharp:
    // https://github.com/UnknownShadow200/ClassicalSharp/blob/master/ClassicalSharp/Generator/NotchyGenerator.Utils.cs
    partial class ClassicGenerator
	{
	    private void FillOblateSpheroid(int centreX, int centreY, int centreZ, double radius, byte block)
	    {
	        int xStart = (int)Math.Floor(Math.Max(centreX - radius, 0));
	        int xEnd = (int)Math.Floor(Math.Min(centreX + radius, level.Width - 1));
	        int yStart = (int)Math.Floor(Math.Max(centreY - radius, 0));
	        int yEnd = (int)Math.Floor(Math.Min(centreY + radius, level.Depth - 1));
	        int zStart = (int)Math.Floor(Math.Max(centreZ - radius, 0));
	        int zEnd = (int)Math.Floor(Math.Min(centreZ + radius, level.Height - 1));

	        for (double x = xStart; x < xEnd; x++)
	        for (double y = yStart; y < yEnd; y++)
	        for (double z = zStart; z < zEnd; z++)
	        {
	            double dx = x - centreX;
	            double dy = y - centreY;
	            double dz = z - centreZ;

	            int ix = (int)x;
	            int iy = (int)y;
	            int iz = (int)z;

	            if ((dx * dx + 2 * dy * dy + dz * dz) < (radius * radius) &&
	                level.IsValidBlock(ix, iy, iz))
	            {
	                if (level.GetBlock(ix, iy, iz) == 1)
	                {
	                    level.SetBlock(ix, iy, iz, block);
	                }
	            }
	        }
	    }

	    private void FloodFill(int startIndex, byte block)
	    {
	        int oneY = level.Width * level.Height;

	        if (startIndex < 0) return; // y below map, immediately ignore
	        FastIntStack stack = new FastIntStack(4);
	        stack.Push(startIndex);

	        while (stack.Size > 0)
	        {
	            int index = stack.Pop();
	            if (index < 0 || index >= level.Blocks.Length) continue;

	            if (level.Blocks[index] != 0) continue;
	            level.Blocks[index] = block;

                int x = index % level.Width;
	            int y = index / oneY;
                int z = (index / level.Width) % level.Height;

	            if (x > 0) stack.Push(index - 1);
                if (x < level.Width - 1) stack.Push(index + 1);
                if (z > 0) stack.Push(index - level.Width);
                if (z < level.Height - 1) stack.Push(index + level.Width);
	            if (y > 0) stack.Push(index - oneY);
	        }
	    }

	    private sealed class FastIntStack
	    {
	        public int[] Values;
	        public int Size;

	        public FastIntStack(int capacity)
	        {
	            Values = new int[capacity];
	            Size = 0;
	        }

	        public int Pop()
	        {
	            return Values[--Size];
	        }

	        public void Push(int item)
	        {
	            if (Size == Values.Length)
	            {
	                int[] array = new int[Values.Length * 2];
	                Buffer.BlockCopy(Values, 0, array, 0, Size * sizeof(int));
	                Values = array;
	            }
	            Values[Size++] = item;
	        }
	    }

	    private bool IsSpaceForTree(int x, int y, int z, int height)
	    {
            for (int xx = x - 1; xx < x + 1; xx++)
            for (int yy = y; yy < y + height; yy++)
            for (int zz = z - 1; zz < z + 1; zz++)
            {
                if (!level.IsValidBlock(xx, yy, zz))
                {
                    return false;
                }

                if (level.GetBlock(xx, yy, zz) != 0)
                {
                    return false;
                }
            }

	        int canopyY = y + (height - 4);

	        for (int xx = x - 2; xx < x + 2; xx++)
	        for (int yy = canopyY; yy < y + height; yy++)
	        for (int zz = z - 2; zz < z + 2; zz++)
	        {
	            if (!level.IsValidBlock(xx, yy, zz))
	            {
	                return false;
	            }

	            if (level.GetBlock(xx, yy, zz) != 0)
	            {
	                return false;
	            }
	        }

	        return true;
	    }

	    private void GrowTree(int x, int y, int z, int height)
	    {
	        level.SetBlock(x, y, z, 17);
	        int max0 = y + height;
	        int max1 = max0 - 1;
	        int max2 = max0 - 2;
	        int max3 = max0 - 3;

            // bottom
            for (int xx = -2; xx <= 2; xx++)
            for (int zz = -2; zz <= 2; zz++)
            {
                int ax = x + xx;
                int az = z + zz;

                if (Math.Abs(xx) == 2 && Math.Abs(zz) == 2)
                {
                    if (RandomBool()) level.SetBlock(ax, max3, az, 18);
                    if (RandomBool()) level.SetBlock(ax, max2, az, 18);
                }
                else
                {
                    level.SetBlock(ax, max3, az, 18);
                    level.SetBlock(ax, max2, az, 18);
                }
            }

            // top
            for (int xx = -1; xx <= 1; xx++)
            for (int zz = -1; zz <= 1; zz++)
            {
                int ax = x + xx;
                int az = z + zz;

                if (xx == 0 || zz == 0)
                {
                    level.SetBlock(ax, max1, az, 18);
                    level.SetBlock(ax, max0, az, 18);
                }
                else
                {
                    if (RandomBool()) level.SetBlock(ax, max1, az, 18);
                } 
            }

	        // grow trunk
	        for (int yy = y; yy < max0 - 1; yy++)
	        {
	            level.SetBlock(x, yy, z, 17);
	        }
	    }

	    private bool RandomBool()
	    {
	        return level.Rng.Next(2) == 0;
	    }
	}
}
