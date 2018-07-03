using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using NLog;

namespace MCarmada.Utils
{
    class LogUtils
    {
        public static Logger GetClassLogger()
        {
            var name = new StackTrace().GetFrame(1).GetMethod().ReflectedType.Name;

            return LogManager.GetLogger(name);
        }
    }
}
