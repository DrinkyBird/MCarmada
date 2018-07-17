using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using fNbt;

namespace MCarmada.World
{
    public partial class Level
    {
        internal struct SavedPlayer
        {
            public string Name;
            public float X, Y, Z, Yaw, Pitch;
            public float ClickDistance;

            public NbtCompound ToCompound()
            {
                var p = new NbtCompound(Name);
                p.Add(new NbtFloat("X", X));
                p.Add(new NbtFloat("Y", Y));
                p.Add(new NbtFloat("Z", Z));
                p.Add(new NbtFloat("Yaw", Yaw));
                p.Add(new NbtFloat("Pitch", Pitch));
                p.Add(new NbtFloat("ClickDistance", ClickDistance));

                return p;
            }
        }

        internal List<SavedPlayer> savedPlayers = new List<SavedPlayer>();

    }
}
