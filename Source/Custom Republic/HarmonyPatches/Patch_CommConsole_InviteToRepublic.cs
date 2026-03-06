using HarmonyLib;
using RimWorld;
using Verse;

namespace CustomRepublic;

[HarmonyPatch(typeof(FactionDialogMaker), nameof(FactionDialogMaker.FactionDialogFor))]
internal static class Patch_CommConsole_InviteToRepublic
{
    static void Postfix(DiaNode __result, Pawn negotiator, Faction faction)
    {
        var map = negotiator.Map;
        if (map == null || !map.IsPlayerHome || (GameComponent_Republic.Instance != null && GameComponent_Republic.Instance.state.HasFaction(faction.def)))
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
                var state = GameComponent_Republic.Instance?.state;
                state?.InviteToRepublic(faction);
            };
            diaOption.link = new DiaNode("CR_RepublicAccepted".Translate())
            {
                options = [new DiaOption((string)"OK".Translate())
                {
                    linkLateBind = FactionDialogMaker.ResetToRoot(faction, negotiator)
                }]
            };
        }
    }
}