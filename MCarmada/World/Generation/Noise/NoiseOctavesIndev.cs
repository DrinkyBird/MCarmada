using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MCarmada.World.Generation.Noise
{
    class NoiseOctavesIndev
    {
        private NoisePerlinIndev[] field_1192_a;
        private int field_1191_b;

        public NoiseOctavesIndev(Random random, int i)
        {
            field_1191_b = i;
            field_1192_a = new NoisePerlinIndev[i];
            for(int j = 0; j < i; j++)
            {
                field_1192_a[j] = new NoisePerlinIndev(random);
            }
        }

        public double Noise(double paramDouble1, double paramDouble2)
        {
            double d1 = 0.0D;
            double d2 = 1.0D;
            for (int i = 0; i < this.field_1191_b; i++)
            {
                d1 += this.field_1192_a[i].Noise(paramDouble1 / d2, paramDouble2 / d2) * d2;
                d2 *= 2.0D;
            }
            return d1;
        }

        public double Noise(double paramDouble1, double paramDouble2, double paramDouble3)
        {
            double d1 = 0.0D;
            double d2 = 1.0D;
            for (int i = 0; i < this.field_1191_b; i++)
            {
                d1 += this.field_1192_a[i].Noise(paramDouble1 / d2, 0.0D / d2, paramDouble3 / d2) * d2;
                d2 *= 2.0D;
            }
            return d1;
        }	
    }
}
