using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MCarmada.Server;

namespace MCarmada.Commands
{
    public abstract class Command
    {
        public abstract void Execute(Player player, string[] args);
        public abstract void Help(Player player);
    }
}
