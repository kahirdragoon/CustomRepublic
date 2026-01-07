using CustomRepublic;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using VFEC;
using VFEC.Perks;
using VFEC.Senators;
using static UnityEngine.UI.CanvasScaler;

namespace CustomRepublic;

[HarmonyPatch(typeof(WorldComponent_Senators), nameof(WorldComponent_Senators.GainFavorOf))]
internal class Patch_WorldComponent_Senators_GainFavorOf
{
    private static readonly FieldInfo United = AccessTools.Field(typeof(WorldComponent_Senators), "united");

    public static bool Prefix(WorldComponent_Senators __instance, Pawn pawn, Faction faction)
    {
        var united = United.GetValue(__instance) as HashSet<Faction>;
        if (united == null)
        {
            Log.ErrorOnce("[Custom Republic] United of WorldComponent_Senators is null or not found", 12345678);
            return true;
        }
        var republic = Current.Game.GetComponent<GameComponent_Republic>()?.state;
        if (republic == null)
        {
            Log.ErrorOnce("[Custom Republic] RepublicState is null in GainFavorOf patch", 87654321);
            return true;
        }

        var info = __instance.InfoFor(pawn, faction);
        info.Favored = true;
        var ext = faction.def.GetModExtension<FactionExtension_SenatorInfo>();
        var perk = ext.senatorPerks[__instance.SenatorInfo[faction].IndexOf(info)];
        var research = ext.senatorResearch[__instance.SenatorInfo[faction].IndexOf(info)];
        var letterLabel = "VFEC.Letters.SenatorJoins".Translate(pawn.Name.ToStringFull);
        var letterDesc = "VFEC.Letters.SenatorJoins.Desc".Translate(pawn.Name.ToStringFull, faction.Name, perk.LabelCap);
        GameComponent_PerkManager.Instance.AddPerk(perk);
        if (!research.IsFinished)
        {
            Find.ResearchManager.FinishProject(research, false, pawn);
            letterDesc += " ";
            letterDesc += "VFEC.Letters.SenatorJoins.Desc.Research".Translate(research.LabelCap);
        }

        if (__instance.SenatorInfo[faction].All(i => i.Favored))
        {
            faction.TryAffectGoodwillWith(Faction.OfPlayer, 1000, reason: VFEC_DefOf.VFEC_GainedFavor);
            __instance.Permanent[faction] = true;
            var finalPerk = ext.finalPerk;
            var finalResearch = ext.finalResearch;
            GameComponent_PerkManager.Instance.AddPerk(finalPerk);
            letterDesc += " ";
            letterDesc += "VFEC.Letters.SenatorJoins.Desc.All".Translate(faction.Name, finalPerk.LabelCap);
            if (!finalResearch.IsFinished)
            {
                Find.ResearchManager.FinishProject(finalResearch, false, pawn);
                letterDesc += " ";
                letterDesc += "VFEC.Letters.SenatorJoins.Desc.All.Research".Translate(ext.numSenators, finalResearch.LabelCap);
            }

            if (faction.ideos is not null && Faction.OfPlayer.ideos is not null) faction.ideos.SetPrimary(Faction.OfPlayer.ideos.PrimaryIdeo);

            
            if(republic.HasFaction(faction.def) && republic.United)
            {
                GameComponent_PerkManager.Instance.AddPerk(republic.customRepublicDef.perk);
                Find.LetterStack.ReceiveLetter(republic.customRepublicDef.letterLabel,
                    republic.customRepublicDef.letterText + "\n" + "VFEC.PerkUnlocked".Translate(republic.customRepublicDef.perk.LabelCap), 
                    LetterDefOf.PositiveEvent);
                foreach (var factionState in republic.factionStates)
                    united.Add(Find.FactionManager.FirstFactionOfDef(factionState.factionDef));
            }

            var cachedMat = AccessTools.FieldRefAccess<Settlement, Material>("cachedMat");
            foreach (var settlement in Find.WorldObjects.Settlements.Where(settlement => settlement.Faction == faction))
                cachedMat(settlement) = null;
        }

        pawn.SetFaction(Faction.OfPlayer);
        if (pawn.ideo is not null && Faction.OfPlayer.ideos is not null)
            pawn.ideo.SetIdeo(Faction.OfPlayer.ideos.PrimaryIdeo);

        var parms = new IncidentParms { target = Find.Maps.Where(m => m.IsPlayerHome && m.Tile.LayerDef.isSpace is false).RandomElement(), spawnCenter = IntVec3.Invalid };
        PawnsArrivalModeDefOf.EdgeWalkIn.Worker.TryResolveRaidSpawnCenter(parms);
        PawnsArrivalModeDefOf.EdgeWalkIn.Worker.Arrive(new List<Pawn> { pawn }, parms);

        Find.LetterStack.ReceiveLetter(letterLabel, letterDesc, LetterDefOf.PositiveEvent, pawn, faction, info.Quest);

        return false;
    }
}