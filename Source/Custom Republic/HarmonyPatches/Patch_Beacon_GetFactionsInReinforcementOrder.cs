using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using VFEC;
using VFEC.Buildings;
using VFEC.Senators;

namespace CustomRepublic;
[HarmonyPatch(typeof(Beacon), "GetFactionsInReinforcementOrder")]
internal static class Patch_Beacon_GetFactionsInReinforcementOrder
{
    public static bool Prefix(ref IEnumerable<Faction> __result)
    {
        var factionStates = GameComponent_Republic.Instance?.state?.factionStates;
        if(factionStates is null || factionStates.Count == 0)
            return true;

        var factions = new List<Faction>();

        foreach (var factionState in factionStates.InRandomOrder())
        {
            var faction = Find.FactionManager.FirstFactionOfDef(factionState.factionDef);
            if (faction != null)
                factions.Add(faction);
        }

        foreach (var f in Find.FactionManager.GetFactions().Where(f => !factions.Contains(f)).InRandomOrder())
            factions.Add(f);

        __result = factions.AsEnumerable();

        return false;
    }
}
