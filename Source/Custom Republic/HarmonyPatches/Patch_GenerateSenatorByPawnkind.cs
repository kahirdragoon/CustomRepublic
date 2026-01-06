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
using VFEC.Senators;

namespace CustomRepublic;
[HarmonyPatch(typeof(WorldComponent_Senators), nameof(WorldComponent_Senators.GenerateSenator))]
internal static class Patch_GenerateSenatorByPawnkind
{
    public static bool Prefix(ref Pawn __result, WorldComponent_Senators __instance, Faction faction)
    {
        if (faction?.def is null || __instance?.world?.worldPawns is null)
            return true;
        var senatorPawnKindDef = faction.def.GetModExtension<FactionExtension_SenatorInfoExtended>()?.senatorPawnKindDef ?? VFEC_DefOf.VFEC_RepublicSenator;
        var pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(senatorPawnKindDef, faction, forceGenerateNewPawn: true));
        __instance.world.worldPawns.PassToWorld(pawn, PawnDiscardDecideMode.KeepForever);
        __result = pawn;
        return false;
    }
}
