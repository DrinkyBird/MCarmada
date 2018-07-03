using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MCarmada.World.Generation
{
    abstract class WorldGenerator
    {
        public abstract void Generate(Level level);
    }
}
