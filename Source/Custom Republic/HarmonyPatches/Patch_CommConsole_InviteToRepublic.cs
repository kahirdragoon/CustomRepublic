using HarmonyLib;
using RimWorld;
using Verse;
using Verse.Noise;

namespace CustomRepublic;

[HarmonyPatch(typeof(FactionDialogMaker), nameof(FactionDialogMaker.FactionDialogFor))]
internal static class Patch_CommConsole_InviteToRepublic
{
    static void Postfix(DiaNode __result, Pawn negotiator, Faction faction)
    {
        var map = negotiator.Map;
        if (map == null || !map.IsPlayerHome || Current.Game.GetComponent<GameComponent_Republic>().state.HasFaction(faction.def))
            return;
        var text = "CR_InviteToRepublic".Translate();
        var diaOption = new DiaOption(text);
        __result.options = [diaOption, .. __result.options];
        if(faction.PlayerRelationKind != FactionRelationKind.Ally)
        {
            diaOption.Disable("MustBeAlly".Translate());
        } 
        else
        {
            diaOption.action = () =>
            {
                var state = Current.Game.GetComponent<GameComponent_Republic>().state;
                state.InviteToRepublic(faction);
            };
            diaOption.link = new DiaNode("CR_RepublicAccepted".Translate());
        }
    }
}