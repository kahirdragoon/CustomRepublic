using RimWorld;
using Verse;
using VFEC.Perks;
using VFEC.Senators;

namespace Custom_Republic;

public class RepublicState : IExposable
{
    public List<RepublicStateFaction> factionStates = new();
    public bool United
    {
        get
        {
            if (WorldComponent_Senators.Instance == null || Find.FactionManager == null || customRepublicDef == null)
                return false;

            var activeFactions = factionStates
                    .Select(factionState => Find.FactionManager.FirstFactionOfDef(factionState.FactionDef))
                    .Where(faction => faction != null)
                    .ToList();

            return activeFactions.Any() &&
                   activeFactions.All(faction => WorldComponent_Senators.Instance.Permanent.TryGetValue(faction, out var perm) && perm);
        }
    }
    public RepublicDef customRepublicDef = DefDatabase<RepublicDef>.AllDefs.FirstOrDefault(r => r.defName == "VFEC_Republic");
    private Dictionary<string, FactionDef> factionCache = new();
    private readonly List<FactionDef> republicParts = new();

   public bool HasFaction(FactionDef factionDef)
    {
        return factionStates.Exists(f => f.factionDefName == factionDef.defName);
    }

    public List<FactionDef> GetFactionDefs(RepublicDef republicDef)
    {
        Log.Warning("[Custom Republic] GetFactionDefs");
        if (republicDef == null)
        {
            Log.Warning("[Custom Republic] RepublicDef is null, returning empty list");
            return new List<FactionDef>();
        }
        if (customRepublicDef == null)
        {
            Log.Warning("[Custom Republic] CustomRepublicDef is null, returning empty list");
            return [];
        }
        if (republicDef.defName != customRepublicDef.defName)
        {
            Log.Warning("[Custom Republic] RepublicDef is not custom republic, returning empty list");
            return [];
        }

        Log.Warning("[Custom Republic] Getting faction defs for republic: " + republicDef.defName);

        if (republicParts.Count <= 0)
        {
            Log.Message("[Custom Republic] Building republic parts list");
            factionStates.ForEach(factionState =>
            {
                if (factionCache.TryGetValue(factionState.factionDefName, out var factionDef))
                    republicParts.Add(factionDef);
                else
                {
                    factionDef = DefDatabase<FactionDef>.GetNamedSilentFail(factionState.factionDefName);
                    if (factionDef != null)
                    {
                        factionCache[factionState.factionDefName] = factionDef;
                        republicParts.Add(factionDef);
                    }
                }
            });
        }

        return republicParts;
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
