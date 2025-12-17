using Verse;

namespace Custom_Republic;
public class RepublicState : IExposable
{
    public HashSet<string> selectedFactions = new();

    public Dictionary<string, int> senatorsPerFaction = new();

    public Dictionary<string, List<string>> perksPerFaction = new();
    public Dictionary<string, string> finalPerkPerFaction = new();

    public Dictionary<string, List<string>> researchPerFaction = new();
    public Dictionary<string, string> finalResearchPerFaction = new();

    public void ExposeData()
    {
        Scribe_Collections.Look(ref selectedFactions, "selectedFactions", LookMode.Value);

        Scribe_Collections.Look(ref senatorsPerFaction, "senatorsPerFaction", LookMode.Value, LookMode.Value);

        Scribe_Collections.Look(ref perksPerFaction, "perksPerFaction", LookMode.Value, LookMode.Value);
        Scribe_Collections.Look(ref finalPerkPerFaction, "finalPerkPerFaction", LookMode.Value, LookMode.Value);

        Scribe_Collections.Look(ref researchPerFaction, "researchPerFaction", LookMode.Value, LookMode.Value);
        Scribe_Collections.Look(ref finalResearchPerFaction, "finalResearchPerFaction", LookMode.Value, LookMode.Value);
    }
}
