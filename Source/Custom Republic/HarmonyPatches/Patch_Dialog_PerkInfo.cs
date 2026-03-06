using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Verse;
using VFEC.Senators;

namespace CustomRepublic;

[HarmonyPatch]
internal static class Patch_Dialog_PerkInfo_FactionData
{
    public static MethodBase TargetMethod()
    {
        var type = AccessTools.TypeByName("VFEC.Senators.Dialog_PerkInfo+RepublicData") ?? throw new Exception("Cannot find RepublicData type");
        var ctor = AccessTools.Constructor(type, [typeof(RepublicDef)]) ?? throw new Exception("Cannot find matching constructor");
        return ctor;
    }

    static void Postfix(object __instance)
    {
        var state = GameComponent_Republic.Instance?.state;
        if (state == null) 
            return;

        var newFactions = BuildFactionDataList(state);

        var factionsField = AccessTools.Field(__instance.GetType(), "factions");
        factionsField.SetValue(__instance, newFactions);
    }

    private static object BuildFactionDataList(RepublicState state)
    {
        var factionDataType = AccessTools.TypeByName("VFEC.Senators.Dialog_PerkInfo+FactionData");

        var listType = typeof(List<>).MakeGenericType(factionDataType);
        var list = Activator.CreateInstance(listType);

        var addMethod = listType.GetMethod("Add");

        var ctor = AccessTools.Constructor(
            factionDataType,
            [typeof(FactionDef), typeof(FactionExtension_SenatorInfo)]
        );

        foreach (var fs in state.factionStates)
        {
            var factionDef =
                DefDatabase<FactionDef>.GetNamedSilentFail(fs.factionDefName);
            if (factionDef == null) continue;

            var ext = FactionExtension_SenatorInfoExtendedFactory.CreateForFaction(factionDef, state);
            if (ext == null) continue;

            var factionData = ctor.Invoke([factionDef, ext]);
            addMethod.Invoke(list, [factionData]);
        }

        return list;
    }
}