using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace CustomRepublic;

[StaticConstructorOnStartup]
internal class Caravan_InviteToRepublicUtility
{
    private static readonly Texture2D InviteToRepublicCommandTex = ContentFinder<Texture2D>.Get("UI/Commands/Trade");

    public static Command InviteToRepublicCommand(Caravan caravan, Faction faction)
    {
        Command_Action commandAction = new()
        {
            defaultLabel = "CR.CommandInviteToRepublic".Translate(),
            defaultDesc = "CR.CommandInviteToRepublicDesc".Translate(),
            icon = InviteToRepublicCommandTex,
            action = () => {
                var state = GameComponent_Republic.Instance?.state;
                state?.InviteToRepublic(faction);
            }
        };
        if(faction.RelationWith(Find.FactionManager.OfPlayer).kind < FactionRelationKind.Ally)
        {
            commandAction.Disable("CR.MustBeAlly".Translate());
        }
        else if(faction == Faction.OfEmpire && (caravan.PawnsListForReading.All(p => p.royalty == null || !p.royalty.HasPermit(CustomRepublicDefOf.InviteToRepublic, faction))))
        {
            commandAction.Disable("CR.NeedPermit".Translate());
        }
        else
        {
            var state = GameComponent_Republic.Instance?.state;
            if(state != null && state.HasFaction(faction.def))
            {
                commandAction.Disable("CR.CommandInviteToRepublicFailAlreadyInRepublic".Translate());
            }
        }

        return commandAction;
    }
}
