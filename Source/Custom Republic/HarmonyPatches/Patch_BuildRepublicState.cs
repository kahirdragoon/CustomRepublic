using HarmonyLib;
using RimWorld;
using Verse;

namespace Custom_Republic;

//[HarmonyPatch(typeof(Page_SelectStartingSite), nameof(Page_CreateWorldParams.PreOpen))]
static class Patch_BuildRepublicState
{
    static void Postfix()
    {
        RepublicStateBuilder.BuildFromRules();
    }
}