using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MCarmada.Server;
using MCarmada.World;

namespace MCarmada.Commands
{
    class CommandEnv : Command
    {
        public override void Execute(Player player, string[] args)
        {
            if (!player.IsOp)
            {
                player.SendMessage("&cYou do not have permission to use this command.");
                return;
            }

            Server.Server server = Program.Instance.Server;
            Level level = server.level;

            if (args.Length < 1)
            {
                Help(player);
                return;
            }

            string subcmd = args[0].ToLower();

            switch (subcmd)
            {
                case "color":
                case "colour":
                {
                    if (args.Length < 5)
                    {
                        Help(player);
                        return;
                    }

                    string target = args[1].ToLower();
                    short r, g, b;

                    try
                    {
                        r = short.Parse(args[2]);
                        g = short.Parse(args[3]);
                        b = short.Parse(args[4]);
                    }
                    catch (Exception)
                    {
                        player.SendMessage("&cColour values formatted incorrectly");
                        return;
                    }

                    switch (target)
                    {
                        case "sky":
                        {
                            level.SkyColour.R = r;
                            level.SkyColour.G = g;
                            level.SkyColour.B = b;
                            break;
                        }

                        case "cloud":
                        {
                            level.CloudColour.R = r;
                            level.CloudColour.G = g;
                            level.CloudColour.B = b;
                            break;
                        }

                        case "fog":
                        {
                            level.FogColour.R = r;
                            level.FogColour.G = g;
                            level.FogColour.B = b;
                            break;
                        }

                        case "ambient":
                        {
                            level.AmbientColour.R = r;
                            level.AmbientColour.G = g;
                            level.AmbientColour.B = b;
                            break;
                        }

                        case "diffuse":
                        {
                            level.DiffuseColour.R = r;
                            level.DiffuseColour.G = g;
                            level.DiffuseColour.B = b;
                            break;
                        }

                        default:
                        {
                            Help(player);
                            return;
                        }
                    }

                    level.InformEveryoneOfEnvironment();

                    break;
                }
            }
        }

        public override void Help(Player player)
        {
            player.SendMessage("&fSyntax: /env colour <sky|cloud|fog|ambient|diffuse> <r> <g> <b>");
        }
    }
}
