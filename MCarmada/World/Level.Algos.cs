using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MCarmada.World
{
    partial class Level
    {
        public bool IsSpaceForTree(int x, int y, int z, int height)
        {
            Block below = GetBlock(x, y - 1, z);
            if (below != Block.Dirt && below != Block.Grass)
            {
                return false;
            }

            Block here = GetBlock(x, y, z);

            if (here == Block.Sapling)
            {
                Blocks[GetBlockIndex(x, y, z)] = Block.Air;
            }

            for (int xx = x - 1; xx < x + 1; xx++)
                for (int yy = y; yy < y + height; yy++)
                    for (int zz = z - 1; zz < z + 1; zz++)
                    {
                        if (!IsValidBlock(xx, yy, zz))
                        {
                            return false;
                        }

                        if (GetBlock(xx, yy, zz) != 0)
                        {
                            return false;
                        }
                    }

            int canopyY = y + (height - 4);

            for (int xx = x - 2; xx < x + 2; xx++)
                for (int yy = canopyY; yy < y + height; yy++)
                    for (int zz = z - 2; zz < z + 2; zz++)
                    {
                        if (!IsValidBlock(xx, yy, zz))
                        {
                            return false;
                        }

                        if (GetBlock(xx, yy, zz) != 0)
                        {
                            return false;
                        }
                    }

            if (here == Block.Sapling)
            {
                Blocks[GetBlockIndex(x, y, z)] = Block.Sapling;
            }

            return true;
        }
            
        public void GrowTree(int x, int y, int z, int height)
        {
            SetBlock(x, y, z, Block.Log);
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
                        if (RandomBool()) SetBlock(ax, max3, az, Block.Leaves);
                        if (RandomBool()) SetBlock(ax, max2, az, Block.Leaves);
                    }
                    else
                    {
                        SetBlock(ax, max3, az, Block.Leaves);
                        SetBlock(ax, max2, az, Block.Leaves);
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
                        SetBlock(ax, max1, az, Block.Leaves);
                        SetBlock(ax, max0, az, Block.Leaves);
                    }
                    else
                    {
                        if (RandomBool()) SetBlock(ax, max1, az, Block.Leaves);
                    }
                }

            // grow trunk
            for (int yy = y; yy < max0; yy++)
            {
                SetBlock(x, yy, z, Block.Log);
            }
        }

        private bool RandomBool()
        {
            return Rng.Next(2) == 0;
        }
    }
}
