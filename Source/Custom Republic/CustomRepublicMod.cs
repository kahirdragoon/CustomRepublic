using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Custom_Republic;
internal class CustomRepublicMod : Mod
{
    public static Harmony Harmony = null!;

    public CustomRepublicMod(ModContentPack content) : base(content)
    {
        Harmony = new Harmony("kahirdragoon.customrepublic");      
    }
}
