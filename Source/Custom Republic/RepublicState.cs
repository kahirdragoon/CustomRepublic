using RimWorld;
using Verse;
using VFEC.Perks;
using VFEC.Senators;

namespace CustomRepublic;

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

    public bool HasFaction(FactionDef factionDef)
    {
        return factionStates.Exists(f => f.factionDefName == factionDef.defName);
    }

    public void AddFaction(FactionDef factionDef)
    {
        if (factionDef == null)
            return;
        if (!HasFaction(factionDef))
        {
            var numberofSenators = factionStates.Count > 0 ? factionStates[0].numSenators : 3;
            factionStates.Add(RepublicStateBuilder.BuildFactionState(factionDef, numberofSenators));
        }
    }

    public void ExposeData()
    {
        Scribe_Collections.Look(ref factionStates, "factionStates", LookMode.Deep);

        if (Scribe.mode == LoadSaveMode.PostLoadInit)
            factionStates ??= new List<RepublicStateFaction>();
    }
}
