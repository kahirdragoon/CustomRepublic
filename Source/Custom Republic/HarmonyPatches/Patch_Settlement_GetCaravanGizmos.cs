using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using Verse;

namespace CustomRepublic;

[HarmonyPatch(typeof(Settlement), nameof(Settlement.GetCaravanGizmos))]
public static class Patch_Settlement_GetCaravanGizmos
{
    static void Postfix(Settlement __instance, ref IEnumerable<Gizmo> __result, Caravan caravan)
    {
        var list = new List<Gizmo>(__result);
        if(CaravanVisitUtility.SettlementVisitedNow(caravan) == __instance)
            list.Add(Caravan_InviteToRepublicUtility.InviteToRepublicCommand(__instance.Faction));
        __result = list;
    }
}
