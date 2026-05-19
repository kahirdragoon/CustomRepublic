using HarmonyLib;
using RimWorld;
using System.Reflection;
using UnityEngine;
using Verse;

namespace CustomRepublic;
[HarmonyPatch(typeof(Page_CreateWorldParams), nameof(Page_CreateWorldParams.DoWindowContents))]
internal static class Patch_Page_CreateWorldParams_RepublicButton
{
    static void Postfix(Page_CreateWorldParams __instance, Rect rect)
    {
        if (CustomRepublicMod.Settings.buttonLocation != ButtonLocation.WorldParams)
            return;
        bool worldbuilderActive = ModsConfig.IsActive("ferny.worldbuilder");
        float buttonX = rect.xMin + 660 - (worldbuilderActive ? 90f : 0f);
        float buttonH = worldbuilderActive ? 30f : 38f;
        var buttonRect = new Rect(buttonX, rect.yMax - buttonH, 165f, buttonH);
        var factionsField = typeof(Page_CreateWorldParams).GetField("factions", BindingFlags.NonPublic | BindingFlags.Instance);

        var factions = (List<FactionDef>)factionsField.GetValue(__instance);

        if (Widgets.ButtonText(buttonRect, "CR.CustomizeRepublicButton".Translate()))
        {
            Find.WindowStack.Add(new Dialog_SelectRepublicFactions(factions));
        }
    }
}
