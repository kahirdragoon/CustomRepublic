using HarmonyLib;
using RimWorld.QuestGen;
using System.Reflection;
using Verse;
using VFEC.Senators;

namespace CustomRepublic;

// VFEC forces the senator as "asker" in QuestNode_GetPawn.RunInt, but its TestRunInt patch still checks the senator
// against the node's filters (e.g. minTechLevel Industrial). Branching nodes like QuestNode_RandomNode then skip every
// branch for low tech factions, leaving slate vars like dutyDescription unset and the quest description unresolvable.
// Make TestRun agree with what RunInt will actually do.
[HarmonyPatch(typeof(QuestNode_GetPawn), "TestRunInt")]
internal static class Patch_QuestNode_GetPawn_SenatorAsker
{
    private static readonly FieldInfo SenatorInfoField = AccessTools.Field(typeof(SenatorQuests), "info");

    public static void Postfix(QuestNode_GetPawn __instance, Slate slate, ref bool __result)
    {
        if (__result)
            return;

        var info = SenatorInfoField.GetValue(null) as SenatorQuests.SenatorInfoWithFaction?;
        var senator = info?.Info?.Pawn;
        if (senator == null)
            return;

        if (__instance.storeAs.GetValue(slate) != "asker" || __instance.mustHaveRoyalTitleInCurrentFaction.GetValue(slate))
            return;

        slate.Set("asker", senator);
        __result = true;
    }
}
