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
                state.AddFaction(faction.def);
            } 
        };

        return commandAction;
    }
}
