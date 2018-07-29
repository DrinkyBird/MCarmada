using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MCarmada.Network;
using MCarmada.Utils;
using MCarmada.World;

namespace MCarmada.Server
{
    partial class Player
    {
        private bool editing = false;
        private bool editClickedFirstBlock = false;
        private BlockPos editFirstBlock;
        private BlockPos editSecondBlock;

        public void StartEditor()
        {
            SendMessage("Editor now engaged. Click a block to start selecting a region.");
            editing = true;
            editClickedFirstBlock = false;
        }

        private void EditorClickBlock(int x, int y, int z)
        {
            if (!editClickedFirstBlock)
            {
                editFirstBlock = new BlockPos(x, y, z);
                editClickedFirstBlock = true;
                CreateSelectionCuboid(1, "Edit", editFirstBlock, editFirstBlock + 1, 255, 0, 255, 128);
                SendMessage("Region started at " + editFirstBlock);
            }
            else
            {
                editing = false;
                editSecondBlock = new BlockPos(x, y, z);

                if (editFirstBlock.X > editSecondBlock.X)
                {
                    int t = editSecondBlock.X;
                    editSecondBlock.X = editFirstBlock.X;
                    editFirstBlock.X = t;
                }

                if (editFirstBlock.Y > editSecondBlock.Y)
                {
                    int t = editSecondBlock.Y;
                    editSecondBlock.Y = editFirstBlock.Y;
                    editFirstBlock.Y = t;
                }

                if (editFirstBlock.Z > editSecondBlock.Z)
                {
                    int t = editSecondBlock.Z;
                    editSecondBlock.Z = editFirstBlock.Z;
                    editFirstBlock.Z = t;
                }

                CreateSelectionCuboid(1, "Edit", editFirstBlock, editSecondBlock + 1, 255, 0, 255, 128);
                SendMessage("Region ended at " + editFirstBlock);
            }
        }

        public void HandleEditCommand(string[] args)
        {
            if (args.Length == 0)
            {
                StartEditor();
                return;
            }

            string subcmd = args[0];

            switch (subcmd)
            {
                case "set":
                {
                    if (args.Length != 2)
                    {
                        return;
                    }

                    string[] split = args[1].Split(',');
                    byte[] blocks = new byte[split.Length];
                    for (var i = 0; i < split.Length; i++)
                    {
                        var s = split[i];
                        blocks[i] = byte.Parse(split[i]);
                    }

                    int num = 0;

                    double start = TimeUtil.GetTimeInMs();

                    Random rng = new Random();

                    for (int bx = editFirstBlock.X; bx < editSecondBlock.X + 1; bx++)
                    for (int by = editFirstBlock.Y; by < editSecondBlock.Y + 1; by++)
                    for (int bz = editFirstBlock.Z; bz < editSecondBlock.Z + 1; bz++)
                    {
                        Block b;

                        if (blocks.Length > 1)
                        {
                            b = (Block) blocks[rng.Next(blocks.Length)];
                        }
                        else
                        {
                            b = (Block) blocks[0];
                        }

                        if (server.level.GetBlock(bx, by, bz) == b)
                        {
                            continue;
                        }

                        server.level.SetBlock(bx, by, bz, b);
                        num++;
                    }

                    double end = TimeUtil.GetTimeInMs();
                    double delta = end - start;

                    string numf = num.ToString("N0");
                    SendMessage("&dModified " + numf + " blocks in " + delta + " ms.");

                    break;
                }
            }
        }
    }
}
