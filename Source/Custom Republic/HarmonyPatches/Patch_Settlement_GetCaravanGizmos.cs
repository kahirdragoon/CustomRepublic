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
        if (GameComponent_Republic.Instance is null)
            return;
        if (CaravanVisitUtility.SettlementVisitedNow(caravan) == __instance
            && !GameComponent_Republic.Instance.state.HasFaction(__instance.Faction.def))
            list.Add(Caravan_InviteToRepublicUtility.InviteToRepublicCommand(caravan, __instance.Faction));
        __result = list;
    }
}
