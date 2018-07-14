using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MCarmada.Server;

namespace MCarmada.Commands
{
    class CommandExtensions : Command
    {
        public override void Execute(Player player, string[] args)
        {
            foreach (var cpeExtension in player.Extensions)
            {
                player.SendMessage(cpeExtension.Name);
            }
        }

        public override void Help(Player player)
        {
            throw new NotImplementedException();
        }
    }
}
