using HarmonyLib;
using RimWorld;
using System;
using Verse;
using VFEC.Senators;

namespace Custom_Republic
{
    public static class FactionDataBuilder
    {
        private static readonly Type FactionDataType = AccessTools.Inner(typeof(Dialog_PerkInfo), "FactionData");

        public static object? Build(string factionDefName, RepublicState state)
        {
            var factionDef = DefDatabase<FactionDef>.GetNamed(factionDefName, false);
            var faction = Find.FactionManager.FirstFactionOfDef(factionDef);
            if (faction == null)
                return null;

            var ext = FactionExtension_SenatorInfoExtendedFactory.CreateForFaction(factionDef, state);
            if (ext == null)
                return null;

            return Activator.CreateInstance(
                FactionDataType,
                factionDef,
                ext
            );
        }
    }
}
