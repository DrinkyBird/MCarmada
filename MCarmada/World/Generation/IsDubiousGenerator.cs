using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MCarmada.World.Generation.Noise;

namespace MCarmada.World.Generation
{
    class IsDubiousGenerator : WorldGenerator
    {
        private Level level;
        private OpenSimplex continents;
        private OpenSimplex simplex1;
        private OpenSimplex simplex2;
        private OctaveNoise simplex3;
        private OpenSimplex simplex4;
        private OpenSimplex cave2;

        private int[] heightmap;

        public IsDubiousGenerator()
        {

        }

        public override void Generate(Level level)
        {
            this.level = level;

            simplex3 = new OctaveNoise(6, level.Rng);
            continents = new OpenSimplex(128, 4.0F, 4.0F, 48, level.Seed * 23);
            simplex1 = new OpenSimplex(128, 4.0F, 2.0F, 24, level.Seed * 4);
            simplex2 = new OpenSimplex(128, 4.0F, 2.0F, 24, level.Seed * 5);
            simplex4 = new OpenSimplex(128, 2.0F, 1.0F, 12, level.Seed * 33);
            cave2 = new OpenSimplex(128, 4.0F, 2.0F, 24, level.Seed * 43);

            GenerateHeightmap();
            PlaceHeightmap();
        }

        private void GenerateHeightmap()
        {
            heightmap = new int[level.Width * level.Height];

            for (int x = 0; x < level.Width; x++)
            for (int z = 0; z < level.Height; z++)
            {
                int offset = level.Depth / 2;

                double cl = continents.noise(x, z) - 2;

                offset += (int)cl;

                int height;
                double v1 = simplex1.noise(x * 1.3f, z * 1.3f) / 3;
                double v2 = simplex2.noise(x * 1.3f, z * 1.3f) / 5 + 6;

                double vf = Math.Max(v1, v2);

                if (simplex3.Compute(x, z) / 8 > 0)
                {
                    vf = v1;
                }

                vf += simplex4.noise(x, z) / 2.0f;

                height = (int)vf + offset;

                heightmap[x + z * level.Width] = height;
            }
        }

        private void PlaceHeightmap()
        {
            for (int x = 0; x < level.Width; x++)
            for (int z = 0; z < level.Height; z++)
            {
                int height = heightmap[x + z * level.Width];
                int waterLevel = 64;

                for (int y = 1; y < 128; y++)
                {
                    Block t = Block.Air;

                    if (waterLevel >= y && y >= height)
                    {
                        t = Block.Water;
                    }
                    else if (y >= height)
                    {
                        t = Block.Air;
                    }
                    else if (y == height - 1)
                    {
                        t = height <= waterLevel + 2 ? Block.Sand : Block.Grass;
                    }
                    else if (y > height - 5)
                    {
                        t = Block.Dirt;
                    }
                    else
                    {
                        t = Block.Stone;
                    }

                    if (t != Block.Air)
                    {
                        level.SetBlock(x, y, z, t);
                    }
                }

                level.SetBlock(x, 0, z, Block.Bedrock);
            }
        }
    }
}
