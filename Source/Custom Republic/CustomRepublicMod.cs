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
    public CustomRepublicMod(ModContentPack content) : base(content)
    {
        var harmony = new Harmony("kahirdragoon.customrepublic");
        harmony.PatchAll();
    }
}
