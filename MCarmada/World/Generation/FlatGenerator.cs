using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MCarmada.World.Generation
{
    class FlatGenerator : WorldGenerator
    {
        public override void Generate(Level level)
        {
            int top = level.Depth / 2;

            for (int x = 0; x < level.Width; x++)
            for (int y = 0; y < top; y++)
            for (int z = 0; z < level.Height; z++)
            {
                Block next = Block.Air;

                if (y == top - 1)
                {
                    next = Block.Grass;
                }
                else if (y >= top - 6)
                {
                    next = Block.Dirt;
                }
                else
                {
                    next = Block.Stone;
                }

                level.SetBlock(x, y, z, next);
            }

        }
    }
}
