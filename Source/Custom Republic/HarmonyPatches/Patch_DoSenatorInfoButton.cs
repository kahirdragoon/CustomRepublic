using Custom_Republic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using VFEC.Senators;

[HarmonyPatch(typeof(SenatorUIUtility), nameof(SenatorUIUtility.DoSenatorInfoButton))]
public static class Patch_DoSenatorInfoButton
{
    static bool Prefix(Faction faction, ref Rect fillRect, float rowY)
    {
        var comp = Current.Game?.GetComponent<GameComponent_Republic>();
        if (comp?.state == null)
            return true;

        if (!comp.state.HasFaction(faction.def))
            return true;

        fillRect.width -= 130f;

        if (Widgets.ButtonText(new Rect(fillRect.width + 5f, rowY + 25f, 120f, 30f), "VFEC.UI.ViewSenators".Translate()))
        {
            WorldComponent_Senators.Instance.CheckInit();

            var ext = RepublicExtensionFactory.CreateForFaction(faction.def, comp.state);

            Find.WindowStack.Add(new Dialog_SenatorInfo(ext, WorldComponent_Senators.Instance.SenatorInfo[faction], false)
            {
                Faction = faction
            });
        }

        return false;
    }
}
