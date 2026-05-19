using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace CustomRepublic;

[HarmonyPatch(typeof(Page_SelectStartingSite), "DoCustomBottomButtons")]
internal static class Patch_Page_SelectStartingSite_RepublicButton
{
    static bool Prefix(Page_SelectStartingSite __instance)
    {
        if (CustomRepublicMod.Settings.buttonLocation != ButtonLocation.StartingSite)
            return true;

        var butSizeField = typeof(Page).GetField("BottomButSize", BindingFlags.NonPublic | BindingFlags.Static);
        Vector2 butSize = butSizeField != null ? (Vector2)butSizeField.GetValue(null) : new Vector2(150f, 38f);

        const int totalButtons = 5;
        int num2 = totalButtons < 3 || UI.screenWidth >= 540f + totalButtons * (butSize.x + 10f) ? 1 : 2;
        int num3 = Mathf.CeilToInt((float)totalButtons / num2);

        float width = butSize.x * num3 + 10f * (num3 + 1);
        float height = num2 * butSize.y + 10f * (num2 + 1);
        Rect rect = new Rect((UI.screenWidth - width) / 2f, UI.screenHeight - height - 4f, width, height);

        WorldInspectPane pane = Find.WindowStack.WindowOfType<WorldInspectPane>();
        if (pane != null && rect.x < InspectPaneUtility.PaneWidthFor(pane) + 4f)
            rect.x = InspectPaneUtility.PaneWidthFor(pane) + 4f;

        Widgets.DrawWindowBackground(rect);

        float curX = rect.xMin + 10f;
        float curY = rect.yMin + 10f;
        Text.Font = GameFont.Small;

        // Back
        if ((Widgets.ButtonText(new Rect(curX, curY, butSize.x, butSize.y), "Back".Translate()) || KeyBindingDefOf.Cancel.KeyDownEvent)
            && Traverse.Create(__instance).Method("CanDoBack").GetValue<bool>())
            Traverse.Create(__instance).Method("DoBack").GetValue();
        curX += butSize.x + 10f;

        // Random site
        if (Widgets.ButtonText(new Rect(curX, curY, butSize.x, butSize.y), "SelectRandomSite".Translate()))
        {
            SoundDefOf.Click.PlayOneShotOnCamera();
            Find.WorldInterface.SelectedTile = !ModsConfig.OdysseyActive || !Rand.Bool
                ? TileFinder.RandomStartingTile()
                : TileFinder.RandomSettlementTileFor(
                    (PlanetLayer)Find.WorldGrid.Surface,
                    Faction.OfPlayer, true,
                    (Predicate<PlanetTile>)(tile => tile.Tile.Landmark != null));
            Find.WorldCameraDriver.JumpTo(Find.WorldGrid.GetTileCenter(Find.WorldInterface.SelectedTile));
        }
        curX += butSize.x + 10f;

        if (num2 == 2)
        {
            curX = rect.xMin + 10f;
            curY += butSize.y + 10f;
        }

        // World factions
        if (Widgets.ButtonText(new Rect(curX, curY, butSize.x, butSize.y), "WorldFactionsTab".Translate()))
            Find.WindowStack.Add(new Dialog_FactionDuringLanding());
        curX += butSize.x + 10f;

        // Customize Republic
        if (Widgets.ButtonText(new Rect(curX, curY, butSize.x, butSize.y), "CR.CustomizeRepublicButton".Translate()))
        {
            var factionDefs = Find.World.factionManager.AllFactionsListForReading
                .Select(f => f.def)
                .ToList();
            Find.WindowStack.Add(new Dialog_SelectRepublicFactions(factionDefs));
        }
        curX += butSize.x + 10f;

        // Next
        if (Widgets.ButtonText(new Rect(curX, curY, butSize.x, butSize.y), "Next".Translate())
            && Traverse.Create(__instance).Method("CanDoNext").GetValue<bool>())
            Traverse.Create(__instance).Method("DoNext").GetValue();

        GenUI.AbsorbClicksInRect(rect);

        return false;
    }
}
