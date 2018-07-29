using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MCarmada.Server;
using MCarmada.World;

namespace MCarmada.Commands
{
    class CommandWeather : Command
    {
        public override void Execute(Player player, string[] args)
        {
            if (!player.IsOp)
            {
                player.SendMessage("&cYou do not have permission to use this command.");
                return;
            }

            if (args.Length != 1)
            {
                player.SendMessage("&fSyntax: /weather <clear|rain|snow>");
                return;
            }

            string type = args[0];
            Level level = Program.Instance.Server.level;

            switch (type.ToLower())
            {
                case "clear":
                {
                    level.Weather = Level.WeatherType.Clear;

                    break;
                }

                case "rain":
                {
                    level.Weather = Level.WeatherType.Raining;

                    break;
                }

                case "snow":
                {
                    level.Weather = Level.WeatherType.Snowing;

                    break;
                }

                default:
                {
                    player.SendMessage("&fSyntax: /weather <clear|rain|snow>");
                    return;
                }
            }
        }

        public override void Help(Player player)
        {
            player.SendMessage("&fSyntax: /whitelist add <name>");
            player.SendMessage("&f        /whitelist del <name>");
            player.SendMessage("&f        /whitelist on");
            player.SendMessage("&f        /whitelist off");
        }
    }
}
