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

        public static int GetUnixTime()
        {
            return (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
        }
    }
}
