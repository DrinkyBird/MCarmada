using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MCarmada.World.Generation.Noise
{
    class NoisePerlinIndev
    {
        private int[] a = new int[512];
        private double b;
        private double c;
        private double d;

        public NoisePerlinIndev(Random paramRandom)
        {
            this.b = (paramRandom.NextDouble() * 256.0D);
            this.c = (paramRandom.NextDouble() * 256.0D);
            this.d = (paramRandom.NextDouble() * 256.0D);
            for (int i = 0; i < 256; i++)
            {
                this.a[i] = i;
            }

            for (int i2 = 0; i2 < 256; i2++)
            {
                int j = paramRandom.Next(256 - i2) + i2;
                int k = this.a[i2];
                this.a[i2] = this.a[j];
                this.a[j] = k;
                this.a[(i2 + 256)] = this.a[i2];
            }
        }

        private double eval(double paramDouble1, double paramDouble2, double paramDouble3)
        {
            double paramDouble4 = paramDouble1 + b;
            double paramDouble5 = paramDouble2 + c;
            double paramDouble6 = paramDouble3 + d;
            paramDouble1 = (double) ((int) Math.Floor(paramDouble4) & 0xff);
            double k = (double) ((int) Math.Floor(paramDouble5) & 0xff);
            paramDouble2 = (double) ((int) Math.Floor(paramDouble6) & 0xff);
            paramDouble4 -= Math.Floor(paramDouble4);
            paramDouble5 -= Math.Floor(paramDouble5);
            paramDouble6 -= Math.Floor(paramDouble6);
            double paramDouble7 = ia(paramDouble4);
            double paramDouble8 = ia(paramDouble5);
            double paramDouble9 = ia(paramDouble6);
            double l = a[(int) paramDouble1] + k;
            paramDouble3 = a[(int) l] + paramDouble2;
            l = a[(int) l + 1] + paramDouble2;
            paramDouble1 = a[(int) paramDouble1 + 1] + k;
            k = a[(int) paramDouble1] + paramDouble2;
            paramDouble1 = a[(int) paramDouble1 + 1] + paramDouble2;
            return ic(paramDouble9,
                ic(paramDouble8,
                    ic(paramDouble7, ia(a[(int) paramDouble3], paramDouble4, paramDouble5, paramDouble6),
                        ia(a[(int) k], paramDouble4 - 1.0D, paramDouble5, paramDouble6)),
                    ic(paramDouble7, ia(a[(int) l], paramDouble4, paramDouble5 - 1.0D, paramDouble6),
                        ia(a[(int) paramDouble1], paramDouble4 - 1.0D, paramDouble5 - 1.0D, paramDouble6))),
                ic(paramDouble8,
                    ic(paramDouble7, ia(a[(int) paramDouble3 + 1], paramDouble4, paramDouble5, paramDouble6 - 1.0D),
                        ia(a[(int) k + 1], paramDouble4 - 1.0D, paramDouble5, paramDouble6 - 1.0D)),
                    ic(paramDouble7, ia(a[(int) l + 1], paramDouble4, paramDouble5 - 1.0D, paramDouble6 - 1.0D),
                        ia(a[(int) paramDouble1 + 1], paramDouble4 - 1.0D, paramDouble5 - 1.0D, paramDouble6 - 1.0D))));
        }

        private static double ia(double paramDouble)
        {
            return paramDouble * paramDouble * paramDouble * (paramDouble * (paramDouble * 6.0D - 15.0D) + 10.0D);
        }

        private static double ic(double paramDouble1, double paramDouble2, double paramDouble3)
        {
            return paramDouble2 + paramDouble1 * (paramDouble3 - paramDouble2);
        }

        private static double ia(int paramInt, double paramDouble1, double paramDouble2, double paramDouble3)
        {
            double d1 = (paramInt &= 15) < 8 ? paramDouble1 : paramDouble2;
            double d2 = (paramInt == 12) || (paramInt == 14) ? paramDouble1 :
                paramInt < 4 ? paramDouble2 : paramDouble3;
            return ((paramInt & 0x1) == 0 ? d1 : -d1) + ((paramInt & 0x2) == 0 ? d2 : -d2);
        }

        public double Noise(double paramDouble1, double paramDouble2)
        {
            return eval(paramDouble1, paramDouble2, 0.0D);
        }

        public double Noise(double paramDouble1, double paramDouble2, double paramDouble3)
        {
            return eval(paramDouble1, paramDouble2, paramDouble3);
        }
    }
}
