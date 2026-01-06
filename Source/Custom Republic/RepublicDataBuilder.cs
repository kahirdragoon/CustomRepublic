using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;
using VFEC.Senators;

namespace Custom_Republic;

public static class RepublicDataBuilder
{
    private static readonly Type RepublicDataType = AccessTools.Inner(typeof(Dialog_PerkInfo), "RepublicData");
    private static readonly FieldInfo FactionsField = AccessTools.Field(RepublicDataType, "factions");

    public static List<object> BuildFromState(RepublicState state)
    {
        var list = new List<object>();
        var republicDef = DefDatabase<RepublicDef>.GetNamed("VFEC_Republic");
        var republicData = Activator.CreateInstance(RepublicDataType, republicDef);
        var factionData = state.factionStates
            .Select(f => FactionDataBuilder.Build(f.factionDefName, state))
            .Where(fd => fd != null)
            .ToList();
        FactionsField.SetValue(republicData, factionData);
        list.Add(republicData);
        return list;
    }
}
