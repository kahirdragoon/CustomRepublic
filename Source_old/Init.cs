using HarmonyLib;
using System.Linq;
using Verse;

namespace InsectRepublic
{
    [StaticConstructorOnStartup]
    public class Init
    {
        static Init()
        {
            var harmony = new Harmony("Insect Republics");
            harmony.PatchAll();
        }
    }
}