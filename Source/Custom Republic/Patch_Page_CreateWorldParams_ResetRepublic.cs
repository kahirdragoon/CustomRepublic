using HarmonyLib;
using RimWorld;
using Verse;

namespace Custom_Republic;

[HarmonyPatch(typeof(Page_CreateWorldParams), nameof(Page_CreateWorldParams.PreOpen))]
public static class Patch_ResetRepublicOnWorldGen
{
    static void Prefix()
    {
        var game = Current.Game;
        if (game == null) return;

        var comp = game.GetComponent<GameComponent_Republic>();
        comp?.ResetForNewGame();
    }
}
