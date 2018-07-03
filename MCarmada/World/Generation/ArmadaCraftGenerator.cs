using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MCarmada.Utils;
using MCarmada.World.Generation.Noise;
using NLog;

namespace MCarmada.World.Generation
{
    class ArmadaCraftGenerator : WorldGenerator
    {
        private static readonly int SAND_OFFSET = 3;

        private OpenSimplex elevation1;
        private OpenSimplex elevation2;
        private OpenSimplex roughness;
        private OpenSimplex detail;

        private Logger logger = LogUtils.GetClassLogger();

        public override void Generate(Level level)
        {
            logger.Info("Generating heightmap");

            elevation1 = new OpenSimplex(level.Depth, 1.0, 5.0, 24, level.Seed * 4);
            elevation2 = new OpenSimplex(level.Depth, 1.0, 5.5, Math.Pow(2, 5), level.Seed * 8);
            roughness = new OpenSimplex(level.Depth, 0.5, 0.5, Math.Pow(2, 1), level.Seed * 16);
            detail = new OpenSimplex(level.Depth, 0.5, 1.0, Math.Pow(2, 2), level.Seed * 32);

            int offset = level.Depth / 2;
            offset += (level.Depth / (level.Width * level.Height)) * 100 * 100;

            List<BlockPos> flowerPoses = new List<BlockPos>();
            List<BlockPos> treePoses = new List<BlockPos>();

            for (int x = 0; x < level.Width; x++)
            for (int z = 0; z < level.Height; z++)
            {
                double elevValue = Math.Max(elevation1.noise(x, z),
                    elevation2.noise((double) x / 4.0, (double) z / 4.0) - (offset / 32.0));
                double roughValue = roughness.noise(x, z);
                double detailValue = detail.noise(x, z);

                int h;

                if (elevValue < -1)
                {
                    h = (int) (offset + (elevValue + (roughValue * 1.0)));
                }
                else
                {
                    h = (int)(offset + ((elevValue * 0.75) + (roughValue * 0.5) * (detailValue / 4)));
                }

                for (int y = h; y > 0; y--)
                {
                    byte next = 0;

                    if (y <= ((level.Depth / 2)) && y > (level.Depth / 2) - SAND_OFFSET &&
                        level.GetBlock(x, y + 1, z) == 0)
                    {
                        next = 12;
                    } 
                    else if (y == h)
                    {
                        int plantChance = level.Rng.Next(5000);

                        if (plantChance <= 40)
                        {
                            flowerPoses.Add(new BlockPos(x, y + 1, z));
                        }
                        else
                        {
                            treePoses.Add(new BlockPos(x, y + 1, z));
                        }
                    }
                    else if (h - y <= 2 + level.Rng.Next(2))
                    {
                        next = 2;
                    }
                    else if (y <= 3 && level.Rng.NextDouble() < 0.25)
                    {
                        next = 7;
                    }
                    else
                    {
                        next = 1;
                    }

                    level.SetBlock(x, y, z, next);
                }

                level.SetBlock(x, 0, z, 7);
            }
        }
    }
}
