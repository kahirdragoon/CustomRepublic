using RimWorld;
using UnityEngine;
using Verse;
using VFEC.Perks;
using VFEC.Senators;

namespace Custom_Republic;

public class RepublicStateFaction : IExposable
{
    public string factionDefName = string.Empty;
    public FactionDef? FactionDef;

    public int numSenators;

    public List<string> senatorPerks = new();
    public string finalPerk = string.Empty;

    public List<string> senatorResearch = new();
    public string finalResearch = string.Empty;

    public string? pawnKindDef = string.Empty;

    private FactionExtension_SenatorInfo? _senatorExtension;
    public FactionExtension_SenatorInfo? SenatorExtension => FactionExtension_SenatorInfoExtendedFactory.CreateForFaction(FactionDef);

    public void ExposeData()
    {
        Scribe_Values.Look(ref factionDefName, "factionDef");
        Scribe_Values.Look(ref numSenators, "numSenators");

        Scribe_Collections.Look(ref senatorPerks, "senatorPerks", LookMode.Value);
        Scribe_Values.Look(ref finalPerk, "finalPerk");

        Scribe_Collections.Look(ref senatorResearch, "senatorResearch", LookMode.Value);
        Scribe_Values.Look(ref finalResearch, "finalResearch");

        Scribe_Values.Look(ref pawnKindDef, "pawnKindDef");

        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            senatorPerks ??= new List<string>();
            senatorResearch ??= new List<string>();
            if(FactionDef == null && !string.IsNullOrEmpty(factionDefName))
            {
                FactionDef = DefDatabase<FactionDef>.GetNamedSilentFail(factionDefName);
            }
        }
    }
}
