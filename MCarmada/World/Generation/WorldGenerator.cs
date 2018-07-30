using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MCarmada.World.Generation
{
    abstract class WorldGenerator
    {
        public static readonly Dictionary<string, WorldGenerator> Generators = new Dictionary<string, WorldGenerator>();

        static WorldGenerator()
        {
            Generators["null"] = new NullGenerator();
            Generators[""] = Generators["null"];
            Generators["classic"] = new ClassicGenerator();
            Generators["flat"] = new FlatGenerator();
            Generators["debug"] = new DebugGenerator();
            Generators["armadacraft"] = new ArmadaCraftGenerator();
            Generators["test"] = new TestGenerator();
            Generators["indev"] = new IndevGenerator();
        }

        public abstract void Generate(Level level);
    }
}
