using Custom_Republic;
using HarmonyLib;
using UnityEngine;
using Verse;
using VFEC.Senators;

//[HarmonyPatch(typeof(SenatorUIUtility), nameof(SenatorUIUtility.DoPerkButton))]
public static class Patch_DoPerkButton
{
    public static bool Prefix()
    {
        var comp = Current.Game?.GetComponent<GameComponent_Republic>();
        if (comp?.state?.factionStates?.Any() == true)
        {
            if (Widgets.ButtonText(new Rect(0, 10f, 120f, 30f), "VFEC.UI.ViewPerks".Translate()))
                Find.WindowStack.Add(new Dialog_PerkInfo());
            return false;
        }

        return true;
    }
}
