using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MCarmada.Utils.Maths
{
    public class MathsUtil
    {
        public static double Radd(double degrees)
        {
            return degrees * (Math.PI / 180.0);
        }

        public static float Radf(float degrees)
        {
            return degrees * ((float) Math.PI / 180.0f);
        }

        public static double Degd(double radians)
        {
            return radians * (180.0 / Math.PI);
        }

        public static float Degf(float radians)
        {
            return radians * (180.0f / (float) Math.PI);
        }
    }
}
