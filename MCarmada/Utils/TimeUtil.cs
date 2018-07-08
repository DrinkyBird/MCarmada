using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MCarmada.Utils
{
    public class TimeUtil
    {
        private TimeUtil()
        {

        }

        public static double GetTimeInMs()
        {
            return (double) DateTime.Now.Ticks / (double) TimeSpan.TicksPerMillisecond;
        }
    }
}
