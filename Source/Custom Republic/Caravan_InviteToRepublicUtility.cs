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

    public static Command InviteToRepublicCommand(Faction faction)
    {
        Command_Action commandAction = new Command_Action
        {
            defaultLabel = "CR.CommandInviteToRepublic".Translate(),
            defaultDesc = "CR.CommandInviteToRepublicDesc".Translate(),
            icon = InviteToRepublicCommandTex,
            action = () =>
            {
                var state = Current.Game.GetComponent<GameComponent_Republic>().state;
                if (state == null)
                    return;

                if (!state.HasFaction(faction.def))
                {
                    state.AddFaction(faction.def);

                    var letterLabel = "CR.LetterFactionJoinedLabel".Translate(faction.Name);
                    var letterText = "CR.LetterFactionJoinedDesc".Translate(faction.Name);
                    Find.LetterStack.ReceiveLetter(letterLabel, letterText, LetterDefOf.PositiveEvent);
                }
            } 
        };
        if(faction.RelationWith(Find.FactionManager.OfPlayer).kind < FactionRelationKind.Ally)
        {
            commandAction.Disable("CR.CommandInviteToRepublicFailNotAlly".Translate());
        }
        else
        {
            var state = Current.Game.GetComponent<GameComponent_Republic>().state;
            if(state.HasFaction(faction.def))
            {
                commandAction.Disable("CR.CommandInviteToRepublicFailAlreadyInRepublic".Translate());
            }
        }

        return commandAction;
    }
}
