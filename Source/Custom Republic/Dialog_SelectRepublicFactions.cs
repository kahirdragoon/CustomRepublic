using RimWorld;
using System.Linq;
using UnityEngine;
using Verse;
using VFEC.Perks;
using VFEC.Senators;
using static System.Collections.Specialized.BitVector32;

namespace Custom_Republic;
public class Dialog_SelectRepublicFactions : Window
{
    public override Vector2 InitialSize => new Vector2(1000f, 720f);

    private Vector2 factionScroll;
    private Dictionary<FactionDef, bool> selectedFactions = new();
    private Dictionary<FactionDef, PawnKindDef?> selectedFactionPawnKinds = new();
    private int selectedFactionsCount => selectedFactions.Values.Count(v => v);

    private HashSet<TechLevel> allowedTechLevels = new();
    private bool prioritizeTechprintResearch = false;
    private bool onlyTechprintResearch = false;
    private List<ResearchProjectDef> nonFinalResearchProjects = new();
    private List<ResearchProjectDef> finalReasearchProjects = new();
    private static readonly List<TechLevel> TechLevelOptions = new()
    {
        TechLevel.Animal,
        TechLevel.Neolithic,
        TechLevel.Medieval,
        TechLevel.Industrial,
        TechLevel.Spacer,
        TechLevel.Ultra,
        TechLevel.Archotech
    };
    private int availableResearchCount => nonFinalResearchProjects.Count + finalReasearchProjects.Count;

    private Vector2 perkScroll;
    private Dictionary<PerkDef, bool> selectedPerks = new();
    private int selectedPerksCount => selectedPerks.Values.Count(v => v);

    private bool distributeSenatorsEvenly = false;
    private int numOfSenatorsPerfaction = 3;
    private int availableSenatorsCount => Math.Min(availableResearchCount, selectedPerksCount);

    private GameComponent_Republic republicComp;

    public Dialog_SelectRepublicFactions(List<FactionDef> existingFactionDefs)
    {
        forcePause = true;
        doCloseX = true;
        closeOnClickedOutside = false;

        republicComp = Current.Game.GetComponent<GameComponent_Republic>()!;
        var rules = republicComp.rules;

        // --- FACTIONS ---
        foreach (var factionDef in existingFactionDefs)
        {
            if (!factionDef.isPlayer && !factionDef.hidden && !factionDef.permanentEnemy)
            {
                bool selected = rules?.selectedFactionDefs?.Contains(factionDef.defName) ?? false;
                selectedFactions[factionDef] = selected;

                PawnKindDef? pawnKind = null;
                if (rules?.pawnKindPerFaction != null &&
                    rules.pawnKindPerFaction.TryGetValue(factionDef.defName, out var pawnKindDefName))
                {
                    pawnKind = DefDatabase<PawnKindDef>.GetNamedSilentFail(pawnKindDefName);
                }

                selectedFactionPawnKinds[factionDef] = pawnKind;
            }
        }

        // --- PERKS ---
        foreach (var perk in DefDatabase<PerkDef>.AllDefsListForReading.Where(p => p.defName != "VeniVidiVici"))
        {
            bool selected = rules?.selectedPerkDefs == null || rules.selectedPerkDefs.Count == 0 || rules.selectedPerkDefs.Contains(perk.defName);
            selectedPerks[perk] = selected;
        }

        // --- TECH / RULES ---
        if (rules != null)
        {
            allowedTechLevels = new HashSet<TechLevel>(rules.allowedTechLevels ?? []);
            prioritizeTechprintResearch = rules.prioritizeTechprintResearch;
            onlyTechprintResearch = rules.onlyTechprintResearch;

            distributeSenatorsEvenly = rules.autoCalculateSenatorsPerFaction;
            if(rules.numOfSenatorsPerFaction > 0)
                numOfSenatorsPerfaction = rules.numOfSenatorsPerFaction;
        }
    }

    public override void DoWindowContents(Rect inRect)
    {
        float padding = 10f;
        float panelWidth = (inRect.width - 4 * padding) / 3f; // three panels
        float curY = 0f;
        float panelHeight = 400f; // adjust as needed

        var techPanel = new Rect(padding, curY, panelWidth, panelHeight);
        AddTechPanel(techPanel);

        var factionPanel = new Rect(techPanel.xMax + padding, curY, panelWidth, panelHeight);
        AddFactionPanel(factionPanel);

        var perkPanel = new Rect(factionPanel.xMax + padding, curY, panelWidth, panelHeight);
        AddPerkPanel(perkPanel);

        curY += panelHeight + padding;

        AddSenatorDistribution(inRect, curY);

        curY += 60f;

        AddInformationalDisplay(ref inRect, ref curY);

        if (Widgets.ButtonText(new Rect(inRect.width - 200f, inRect.height - 40f, 190f, 35f), "Accept".Translate()))
        {
            SaveSelections();
            Close();
        }
    }

    private void AddTechPanel(Rect panel)
    {
        Widgets.DrawMenuSection(panel);

        float y = panel.y + 5f;
        Widgets.Label(new Rect(panel.x + 5f, y, panel.width - 10f, 25f), "Allowed Tech Levels for Research Projects:");
        y += 30f;

        foreach (var tl in TechLevelOptions)
        {
            bool selected = allowedTechLevels.Contains(tl);
            var row = new Rect(panel.x + 20f, y, panel.width - 30f, 24f);
            Widgets.CheckboxLabeled(row, tl.ToString(), ref selected);

            if (selected) allowedTechLevels.Add(tl);
            else allowedTechLevels.Remove(tl);

            y += 28f;
        }
        y+= 10f;
        var techprintRect = new Rect(panel.x + 5f, y + 5f, panel.width - 13f, 28f);
        Widgets.CheckboxLabeled(techprintRect, "Prioritize Techprint-Locked Research", ref prioritizeTechprintResearch, disabled: onlyTechprintResearch);
        y += 28f;
        var techprinOnlytRect = new Rect(panel.x + 5f, y + 5f, panel.width - 13f, 28f);
        Widgets.CheckboxLabeled(techprinOnlytRect, "Use only Techprint-Locked Research", ref onlyTechprintResearch);
        y += 40f;

        var useAllTechLevels = allowedTechLevels.Count == 0;
        var allowedResearchProjects = DefDatabase<ResearchProjectDef>.AllDefsListForReading.Where(r => useAllTechLevels || allowedTechLevels.Contains(r.techLevel)).ToList();
        var techprintResearchProjects = allowedResearchProjects.Where(r => (prioritizeTechprintResearch || onlyTechprintResearch) && r.techprintCount > 0).ToList();
        var nonTechprintResearchProjects = onlyTechprintResearch ? [] : allowedResearchProjects.Except(techprintResearchProjects).ToList();
        finalReasearchProjects = techprintResearchProjects.Take(selectedFactions.Count).ToList();
        if (finalReasearchProjects.Count < selectedFactions.Count)
            finalReasearchProjects.AddRange(nonTechprintResearchProjects.Take(selectedFactions.Count - finalReasearchProjects.Count));
        finalReasearchProjects.ForEach(p => { techprintResearchProjects.Remove(p); nonTechprintResearchProjects.Remove(p); });
        nonFinalResearchProjects = techprintResearchProjects.Concat(nonTechprintResearchProjects).InRandomOrder().ToList();

        Widgets.Label(new Rect(panel.x + 5f, y, panel.width - 10f, 25f), $"Available Research Projects: {availableResearchCount}");
    }

    private void AddFactionPanel(Rect panel)
    {
        Widgets.DrawMenuSection(panel);

        // --- Header ---
        float headerHeight = 30f;
        Rect headerRect = new Rect(panel.x + 5f, panel.y + 5f, panel.width - 10f, 25f);
        Widgets.Label(headerRect, "Select republic factions");

        // --- Footer ---
        float footerHeight = 25f;
        Rect footerRect = new Rect(panel.x + 5f, panel.yMax - footerHeight - 5f, panel.width - 10f, footerHeight);

        // --- Scroll area rect (visible window) ---
        float scrollY = headerRect.yMax + 5f;
        float scrollHeight = panel.height - headerHeight - footerHeight - 15f;
        // Subtract header, footer, and some padding

        Rect scrollOuter = new Rect(panel.x + 5f, scrollY, panel.width - 10f, scrollHeight);

        // --- Inner scroll content rect (relative to 0,0 inside scroll) ---
        float rowHeight = 32f;
        Rect viewRect = new Rect(0, 0, scrollOuter.width - 20f, selectedFactions.Count * rowHeight);

        Widgets.BeginScrollView(scrollOuter, ref factionScroll, viewRect);

        // --- Draw rows (local coords begin at 0,0) ---
        float y = 0f;

        foreach (var faction in selectedFactions.Keys.ToList())
        {
            Rect row = new Rect(0, y, viewRect.width, rowHeight);

            // Checkbox
            bool selected = selectedFactions[faction];
            Widgets.CheckboxLabeled(new Rect(5f, y, 200f, rowHeight), faction.label, ref selected);
            selectedFactions[faction] = selected;

            // PawnKind dropdown
            var current = selectedFactionPawnKinds[faction];
            string label = current != null ? current.LabelCap : "Default";

            Rect dropRect = new Rect(215f, y + 2f, viewRect.width - 215f, rowHeight - 4f);
            if (Widgets.ButtonText(dropRect, label))
                Find.WindowStack.Add(new FloatMenu(GeneratePawnKindOptions(faction)));

            y += rowHeight;
        }

        Widgets.EndScrollView();

        // --- Footer label ---
        Widgets.Label(footerRect, $"Selected factions: {selectedFactionsCount}");
    }



    private void AddPerkPanel(Rect panel)
    {
        Widgets.DrawMenuSection(panel);

        // --- Header ---
        float headerHeight = 30f;
        Rect headerRect = new Rect(panel.x + 5f, panel.y + 5f, panel.width - 10f, 25f);
        Widgets.Label(headerRect, "Select available perks");

        // --- Footer ---
        float footerHeight = 25f;
        Rect footerRect = new Rect(panel.x + 5f, panel.yMax - footerHeight - 5f, panel.width - 10f, footerHeight);

        // --- Scroll window (visible area) ---
        float scrollY = headerRect.yMax + 5f;
        float scrollHeight = panel.height - headerHeight - footerHeight - 15f;

        Rect scrollOuter = new Rect(panel.x + 5f, scrollY, panel.width - 10f, scrollHeight);

        // --- Inner scroll content (local 0,0 coords) ---
        float rowHeight = 28f;
        Rect viewRect = new Rect(0, 0, scrollOuter.width - 20f, selectedPerks.Count * rowHeight);

        Widgets.BeginScrollView(scrollOuter, ref perkScroll, viewRect);

        // Draw rows locally inside the scroll area
        float y = 0f;

        foreach (var perk in selectedPerks.Keys.ToList())
        {
            bool selected = selectedPerks[perk];
            Rect row = new Rect(5f, y, viewRect.width - 10f, rowHeight);

            Widgets.CheckboxLabeled(row, perk.LabelCap, ref selected);
            selectedPerks[perk] = selected;

            TooltipHandler.TipRegion(row, perk.description);

            y += rowHeight;
        }

        Widgets.EndScrollView();

        // --- Footer label ---
        Widgets.Label(footerRect, $"Selected Perks: {selectedPerksCount}");
    }

    private void AddSenatorDistribution(Rect inRect, float curY)
    {
        // Checkbox: distribute evenly
        Rect checkRect = new Rect(0, curY, 600f, 30f);
        Widgets.CheckboxLabeled(checkRect, "Use Perk/ResearchProject Count to determine the number of available senators", ref distributeSenatorsEvenly);

        curY += 35f;

        bool oldState = GUI.enabled;

        if (distributeSenatorsEvenly)
            GUI.enabled = false; // Disable slider

        // Slider label
        Widgets.Label(new Rect(0, curY, 350f, 30f), "Number of senators per faction: " + numOfSenatorsPerfaction);

        Rect sliderRect = new Rect(0, curY + 25f, inRect.width - 20f, 28f);

        numOfSenatorsPerfaction = Mathf.RoundToInt(
            Widgets.HorizontalSlider(
                sliderRect,
                numOfSenatorsPerfaction,
                1,
                5,
                middleAlignment: true,
                label: null,
                roundTo: 1f
            )
        );

        GUI.enabled = oldState; // Restore state
    }


    private void AddInformationalDisplay(ref Rect inRect, ref float curY)
    {
        curY += 30f;
        if (distributeSenatorsEvenly)
        {
            Widgets.Label(new Rect(0, curY, inRect.width, 25f), $"Available number of Senators is {availableSenatorsCount}. They will be distributed evenly among selected factions. Average {(int)(availableSenatorsCount / selectedFactionsCount)} per faction (Max 5)");
            curY += 30f;
            if(availableResearchCount > selectedPerksCount)
                Widgets.Label(new Rect(0, curY, inRect.width, 25f), $"Senators limit by number of selected perks");
            else if (availableResearchCount < selectedPerksCount)
                Widgets.Label(new Rect(0, curY, inRect.width, 25f), $"Senators limit by number of available number of research projects");
            curY += 60f;
        }
        else
        {
            var totalNumberOfSenators = selectedFactionsCount * numOfSenatorsPerfaction;
            Widgets.Label(new Rect(0, curY, inRect.width, 25f), $"Total number of Senators is {totalNumberOfSenators}. Perks/Research Projects will be distributet among them.");
            curY += 30f;
            if(totalNumberOfSenators > selectedPerksCount)
                Widgets.Label(new Rect(0, curY, inRect.width, 25f), "More senators than perks. Some perks will be added multiple times bridge the gap.");
            curY += 30f;
            if (totalNumberOfSenators > availableResearchCount)
                Widgets.Label(new Rect(0, curY, inRect.width, 25f), "More senators than research projects. Some research projects will be added multiple times bridge the gap.");
            curY += 30f;
            if (totalNumberOfSenators < selectedPerksCount)
                Widgets.Label(new Rect(0, curY, inRect.width, 25f), "Less senators than perks. Not all selected perks will be available.");
            curY += 30f;
        }

        if(selectedFactionsCount > 7)
        {
            Widgets.Label(new Rect(0, curY, inRect.width, 25f), "<color=yellow>More than 7 factions selected. The perk overview dialog may not display all factions correctly.</color>");
            curY += 30f;
        }
    }

    private List<FloatMenuOption> GeneratePawnKindOptions(FactionDef factionDef)
    {
        var opts = new List<FloatMenuOption>();

        opts.Add(new FloatMenuOption("Default", () =>
        {
            selectedFactionPawnKinds[factionDef] = null;
        }));

        foreach (var kind in DefDatabase<PawnKindDef>.AllDefs)
        {
            if (kind.race?.race?.Humanlike != true) continue;
            if (kind.defaultFactionDef != factionDef) continue;

            opts.Add(new FloatMenuOption(kind.label, () =>
            {
                selectedFactionPawnKinds[factionDef] = kind;
            }));
        }

        return opts;
    }

    private void SaveSelections()
    {
        var comp = Current.Game.GetComponent<GameComponent_Republic>();
        comp.rules.onlyTechprintResearch = onlyTechprintResearch;
        comp.rules.prioritizeTechprintResearch = prioritizeTechprintResearch;
        comp.rules.allowedTechLevels = allowedTechLevels;
        comp.rules.autoCalculateSenatorsPerFaction = distributeSenatorsEvenly;
        comp.rules.numOfSenatorsPerFaction = numOfSenatorsPerfaction;
        comp.rules.selectedFactionDefs = selectedFactions.Where(kvp => kvp.Value).Select(kvp => kvp.Key.defName).ToList();
        comp.rules.pawnKindPerFaction = selectedFactionPawnKinds
            .Where(kvp => kvp.Value != null)
            .ToDictionary(kvp => kvp.Key.defName, kvp => kvp.Value?.defName);
        comp.rules.selectedPerkDefs = selectedPerks.Where(kvp => kvp.Value).Select(kvp => kvp.Key.defName).ToList();
    }
}
