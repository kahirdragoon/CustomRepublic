using RimWorld;
using Verse;

namespace Custom_Republic;

public class RepublicRules : IExposable
{
    public int numOfSenatorsPerFaction;
    public bool distributeSenatorsEvenly;

    public HashSet<TechLevel> allowedTechLevels = new();
    public bool prioritizeTechprintResearch;
    public bool onlyTechprintResearch;

    public List<string> selectedFactionDefs = new();
    public List<string> selectedPerkDefs = new();

    public Dictionary<string, string?> pawnKindPerFaction = new();

    public void ExposeData()
    {
        Scribe_Values.Look(ref numOfSenatorsPerFaction, "senatorsPerFaction");
        Scribe_Values.Look(ref distributeSenatorsEvenly, "distributeSenatorsEvenly");
        Scribe_Values.Look(ref prioritizeTechprintResearch, "prioritizeTechprintResearch");

        Scribe_Collections.Look(ref allowedTechLevels, "allowedTechLevels", LookMode.Value);
        Scribe_Collections.Look(ref selectedFactionDefs, "selectedFactionDefs", LookMode.Value);
        Scribe_Collections.Look(ref selectedPerkDefs, "selectedPerkDefs", LookMode.Value);
        Scribe_Collections.Look(ref pawnKindPerFaction, "pawnKindPerFaction", LookMode.Value, LookMode.Value);
    }
}
