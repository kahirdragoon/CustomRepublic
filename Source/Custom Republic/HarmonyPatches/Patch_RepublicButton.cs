using HarmonyLib;
using RimWorld;
using System.Reflection;
using UnityEngine;
using Verse;

namespace Custom_Republic;
[HarmonyPatch(typeof(Page_CreateWorldParams), nameof(Page_CreateWorldParams.DoWindowContents))]
public static class Patch_RepublicButton
{
    static void Postfix(Page_CreateWorldParams __instance, Rect rect)
    {
        var buttonRect = new Rect(rect.xMin + 660, rect.yMax - 38f, 165f, 38f);
        var factionsField = typeof(Page_CreateWorldParams).GetField("factions", BindingFlags.NonPublic | BindingFlags.Instance);

        var factions = (List<FactionDef>)factionsField.GetValue(__instance);

        if (Widgets.ButtonText(buttonRect, "CustomizeRepublic".Translate()))
        {
            Find.WindowStack.Add(new Dialog_SelectRepublicFactions(factions));
        }
    }
}
