using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MCarmada.Utils
{
    public class FixedPoint
    {
        private FixedPoint() {}

        public static short ToFixedPoint(float value)
        {
            return (short) (value * 32.0f);
        }

        public static float ToFloatingPoint(short value)
        {
            return (float) value / 32.0f;
        }
    }
}
