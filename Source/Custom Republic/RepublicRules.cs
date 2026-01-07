using RimWorld;
using Verse;

namespace CustomRepublic;

public class RepublicRules : IExposable
{
    public int numOfSenatorsPerFaction = 3;
    public bool autoCalculateSenatorsPerFaction;

    public bool ignoreTechprintResearch;

    public List<string> selectedFactionDefs = new();
    public List<string> selectedPerkDefs = new();

    public Dictionary<string, string?> pawnKindPerFaction = new();

    public void ExposeData()
    {
        Scribe_Values.Look(ref numOfSenatorsPerFaction, "senatorsPerFaction");
        Scribe_Values.Look(ref autoCalculateSenatorsPerFaction, "autoCalculateNumberOfSenators");

        Scribe_Values.Look(ref ignoreTechprintResearch, "ignoreTechprintResearch");

        Scribe_Collections.Look(ref selectedFactionDefs, "selectedFactionDefs", LookMode.Value);

        Scribe_Collections.Look(ref pawnKindPerFaction, "pawnKindPerFaction", LookMode.Value, LookMode.Value);

        Scribe_Collections.Look(ref selectedPerkDefs, "selectedPerkDefs", LookMode.Value);

        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            selectedFactionDefs ??= new List<string>();
            selectedPerkDefs ??= new List<string>();
            pawnKindPerFaction ??= new Dictionary<string, string?>();
        }
    }
}
