using RimWorld;
using Verse;
using VFEC.Senators;
using VFEC.Perks;
using System.Collections.Generic;

namespace CustomRepublic;

public static class FactionExtension_SenatorInfoExtendedFactory
{
    private static readonly Dictionary<string, FactionExtension_SenatorInfoExtended> cache = new();

    public static FactionExtension_SenatorInfo CreateForFactionFromDef(Def def)
    {
        if (def is not FactionDef factionDef)
            return def.GetModExtension<FactionExtension_SenatorInfo>();

        return CreateForFaction(factionDef);
    }

    public static FactionExtension_SenatorInfo CreateForFaction(FactionDef? factionDef)
    {
        if (factionDef is null)
        {
            Log.Warning($"[Custom Republic] NULL Faction");
            return (FactionExtension_SenatorInfoExtended)Empty();
        }

        var state = Current.Game?.GetComponent<GameComponent_Republic>()?.state;

        return CreateForFaction(factionDef, state);
    }

    public static FactionExtension_SenatorInfo CreateForFaction(FactionDef factionDef, RepublicState? state)
    {
        if (cache.TryGetValue(factionDef.defName, out var ext))
            return ext;

        var factionState = state?.factionStates.FirstOrDefault(f => f.factionDefName == factionDef.defName);
        if (factionState is null)
        {
            Log.Warning($"[Custom Republic] No republic state for faction {factionDef.defName}");
            return default(FactionExtension_SenatorInfoExtended);
        }

        ext = new FactionExtension_SenatorInfoExtended
        {
            numSenators = factionState.numSenators,
            perkBGPath = "UI/Perks/PerkBG_WesternRepublic"
        };

        ext.senatorPerks = factionState.senatorPerks
            .Select(defName => DefDatabase<PerkDef>.GetNamedSilentFail(defName))
            .Where(p => p != null)
            .ToList();

        ext.finalPerk = DefDatabase<PerkDef>
            .GetNamedSilentFail(factionState.finalPerk);

        ext.senatorResearch = factionState.senatorResearch
            .Select(defName => DefDatabase<ResearchProjectDef>.GetNamedSilentFail(defName))
            .Where(r => r != null)
            .ToList();

        ext.finalResearch = DefDatabase<ResearchProjectDef>
            .GetNamedSilentFail(factionState.finalResearch);

        if (!string.IsNullOrEmpty(factionState.pawnKindDef))
            ext.senatorPawnKindDef = DefDatabase<PawnKindDef>.GetNamedSilentFail(factionState.pawnKindDef);

        cache.Add(factionDef.defName, ext);

        return ext;
    }

    private static FactionExtension_SenatorInfo Empty()
    {
        return new FactionExtension_SenatorInfo
        {
            numSenators = 0,
            senatorPerks = new List<PerkDef>(),
            senatorResearch = new List<ResearchProjectDef>()
        };
    }
}
