using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MCarmada.Network
{
    internal interface ILevelCompressor : IDisposable
    {
        void Process();
        bool IsComplete();
    }
}
