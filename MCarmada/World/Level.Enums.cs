using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using fNbt;
using MCarmada.Network;
using MCarmada.World.Generation;

namespace MCarmada.World
{
    partial class Level
    {
        public enum WeatherType : byte
        {
            Clear,
            Raining,
            Snowing
        }

        public enum ColorType
        {
            Sky, Cloud, Fog, Ambient, Diffuse
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct EnvColor
        {
            public short R;
            public short G;
            public short B;

            public EnvColor(short r = -1, short g = -1, short b = -1)
            {
                R = r;
                G = g;
                B = b;
            }

            public void Reset()
            {
                R = G = B = -1;
            }

            public void Write(Packet p)
            {
                p.Write(R).Write(G).Write(B);
            }

            public NbtCompound ToCompound(string name)
            {
                return new NbtCompound(name)
                {
                    new NbtShort("R", R),
                    new NbtShort("G", G),
                    new NbtShort("B", B)
                };
            }

            public static EnvColor CreateEmpty()
            {
                return new EnvColor(-1, -1, -1);
            }

            public static EnvColor Get(NbtCompound c, string name)
            {
                if (!c.Contains(name))
                {
                    return CreateEmpty();
                }

                var tag = c.Get<NbtCompound>(name);
                if (!tag.Contains("R") || !tag.Contains("G") || !tag.Contains("B"))
                {
                    return CreateEmpty();
                }

                return new EnvColor(
                    tag["R"].ShortValue,
                    tag["G"].ShortValue,
                    tag["B"].ShortValue
                );
            }
        }
    }
}
