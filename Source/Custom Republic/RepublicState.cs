using RimWorld;
using Verse;
using VFEC.Perks;
using VFEC.Senators;

namespace Custom_Republic;

public class RepublicState : IExposable
{
    public List<RepublicStateFaction> factionStates = new();
    public bool United => customRepublicDef != null ? GameComponent_PerkManager.Instance.ActivePerks.Contains(customRepublicDef.perk) : false;
    private RepublicDef customRepublicDef = DefDatabase<RepublicDef>.AllDefs.FirstOrDefault();
    private Dictionary<string, FactionDef> factionCache = new();

    public bool HasFaction(FactionDef factionDef)
    {
        return factionStates.Exists(f => f.factionDefName == factionDef.defName);
    }

    public List<FactionDef> GetFactionDefs(RepublicDef republicDef)
    {
        if(republicDef != customRepublicDef)
            return republicDef.parts;

        var defs = new List<FactionDef>();

        factionStates.ForEach(factionState =>
        {
            if (factionCache.TryGetValue(factionState.factionDefName, out var factionDef))
                defs.Add(factionDef);
            else
            {
                factionDef = DefDatabase<FactionDef>.GetNamedSilentFail(factionState.factionDefName);
                if (factionDef != null)
                {
                    factionCache[factionState.factionDefName] = factionDef;
                    defs.Add(factionDef);
                }
            }
        });
        return defs;
    }

    public void Deconstruct(out List<RepublicStateFaction> factions, out bool united)
    {
        factions = factionStates;
        united = United;
    }

    public void ExposeData()
    {
        Scribe_Collections.Look(ref factionStates, "factionStates", LookMode.Deep);

        if (Scribe.mode == LoadSaveMode.PostLoadInit)
            factionStates ??= new List<RepublicStateFaction>();
    }
}
