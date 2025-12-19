using Custom_Republic;
using HarmonyLib;
using RimWorld;
using Verse;
using VFEC.Senators;

//[HarmonyPatch(typeof(SenatorUIUtility), nameof(SenatorUIUtility.ShouldHaveSenators))]
//public static class Patch_ShouldHaveSenators
//{
//    static bool Prefix(Faction faction, ref bool __result)
//    {
//        var comp = Current.Game?.GetComponent<GameComponent_Republic>();
//        if (comp?.state == null)
//            return true;

//        __result =
//            comp.state.HasFaction(faction.def) &&
//            !faction.Hidden &&
//            !faction.temporary;

//        return false;
//    }
//}
