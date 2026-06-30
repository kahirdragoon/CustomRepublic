using RimWorld;
using System.Linq;
using UnityEngine;
using Verse;
using VFEC.Perks;
using VFEC.Senators;
using static System.Collections.Specialized.BitVector32;

namespace CustomRepublic;
public class Dialog_SelectRepublicFactions : Window
{
    public override Vector2 InitialSize => new(1000f, 720f);

    private Vector2 factionScroll;
    private readonly Dictionary<FactionDef, bool> selectedFactions = [];
    private readonly Dictionary<FactionDef, PawnKindDef?> selectedFactionPawnKinds = [];
    private int SelectedFactionsCount => selectedFactions.Values.Count(v => v);
    private readonly List<FactionDef> factionDefsWithExtension = [];

    private bool ignoreTechprintResearch = true;
    private bool useDummyResearch = false;
   
    private Vector2 perkScroll;
    private readonly Dictionary<PerkDef, bool> selectedPerks = [];
    private int SelectedPerksCount => selectedPerks.Values.Count(v => v);

    private int numOfSenatorsPerfaction = 3;

    private const float padding = 10f;

    public Dialog_SelectRepublicFactions(List<FactionDef> existingFactionDefs)
    {
        forcePause = true;
        doCloseX = true;
        closeOnClickedOutside = false;

        var rules = GameComponent_Republic.Instance?.rules;

        // --- FACTIONS ---
        foreach (var factionDef in existingFactionDefs)
        {
            if (!factionDef.isPlayer && !factionDef.hidden && !factionDef.permanentEnemy)
            {
                if(factionDef.HasModExtension<FactionExtension_SenatorInfoExtended>())
                    factionDefsWithExtension.Add(factionDef);

                bool selected = rules?.selectedFactionDefs?.Contains(factionDef.defName) ?? false;
                selectedFactions[factionDef] = selected;

                PawnKindDef? pawnKind = null;
                if (rules?.pawnKindPerFaction != null &&
                    rules.pawnKindPerFaction.TryGetValue(factionDef.defName, out var pawnKindDefName) &&
                    !string.IsNullOrEmpty(pawnKindDefName))
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

        // --- RULES ---
        if (rules != null)
        {
            ignoreTechprintResearch = rules.ignoreTechprintResearch;
            useDummyResearch = rules.useDummyResearch;

            if (rules.numOfSenatorsPerFaction > 0)
                numOfSenatorsPerfaction = rules.numOfSenatorsPerFaction;
        }
    }

    public override void DoWindowContents(Rect inRect)
    {
        float panelWidth = (inRect.width - 4 * padding) / 3f; // three panels
        float curY = 0f;
        float panelHeight = 400f; // adjust as needed


        var factionPanel = new Rect(padding, curY, panelWidth * 2, panelHeight);
        AddFactionPanel(factionPanel);

        var perkPanel = new Rect(factionPanel.xMax + padding, curY, panelWidth, panelHeight);
        AddPerkPanel(perkPanel);

        curY += panelHeight + padding;
        
        var techPanel = new Rect(0, curY, panelWidth, panelHeight);
        AddTechPanel(techPanel);

        curY += 70f;

        AddSenatorDistribution(inRect, curY);

        curY += 60f;

        AddInformationalDisplay(ref inRect, ref curY);

        if (Widgets.ButtonText(new Rect(inRect.width - 200f, inRect.height - 40f, 190f, 35f), "CR.ButtonAccept".Translate()))
        {
            SaveSelections();
            Close();
        }
    }

    private void AddTechPanel(Rect panel)
    {
        float y = panel.y + 5f;
        var ignoreTechprintRect = new Rect(padding + panel.x, y + 5f, panel.width - 13f, 28f);
        Widgets.CheckboxLabeled(ignoreTechprintRect, "CR.IgnoreTechprintResearch".Translate(), ref ignoreTechprintResearch);
        TooltipHandler.TipRegion(ignoreTechprintRect, "CR.IgnoreTechprintResearchDesc".Translate());

        y += 30f;
        var useDummyRect = new Rect(padding + panel.x, y + 5f, panel.width - 13f, 28f);
        Widgets.CheckboxLabeled(useDummyRect, "CR.UseDummyResearch".Translate(), ref useDummyResearch);
        TooltipHandler.TipRegion(useDummyRect, "CR.UseDummyResearchDesc".Translate());
    }

    private void AddFactionPanel(Rect panel)
    {
        Widgets.DrawMenuSection(panel);

        // --- Header ---
        float headerHeight = 30f;
        Rect headerRect = new(panel.x + 5f, panel.y + 5f, panel.width - 10f, 25f);
        Widgets.Label(headerRect, "CR.SelectRepublicFactions".Translate());

        // Senator PawnKind header label (placed on same line as header)
        Rect pawnHeaderRect = new(headerRect.x + 240f, headerRect.y, 150f, headerRect.height);
        Widgets.Label(pawnHeaderRect, "CR.SenatorPawnKindHeader".Translate());

        // --- Footer ---
        float footerHeight = 25f;
        Rect footerRect = new(panel.x + 5f, panel.yMax - footerHeight - 5f, panel.width - 10f, footerHeight);

        // --- Scroll area rect (visible window) ---
        float scrollY = headerRect.yMax + 5f;
        float scrollHeight = panel.height - headerHeight - footerHeight - 15f;
        // Subtract header, footer, and some padding

        Rect scrollOuter = new(panel.x + 5f, scrollY, panel.width - 10f, scrollHeight);

        // --- Inner scroll content rect (relative to 0,0 inside scroll) ---
        float rowHeight = 32f;
        Rect viewRect = new(0, 0, scrollOuter.width - 20f, selectedFactions.Count * rowHeight);

        Widgets.BeginScrollView(scrollOuter, ref factionScroll, viewRect);

        // --- Draw rows (local coords begin at 0,0) ---
        float y = 0f;

        foreach (var faction in selectedFactions.Keys.ToList())
        {
            Rect row = new(0, y, viewRect.width, rowHeight);

            // Checkbox
            bool selected = selectedFactions[faction];
            Widgets.CheckboxLabeled(new Rect(5f, y, 200f, rowHeight), faction.label, ref selected);
            selectedFactions[faction] = selected;

            if (factionDefsWithExtension.Contains(faction))
            {
                // Display note that data comes from ModExtension
                float dropdownX = headerRect.x + 220f;
                Rect modLabelRect = new(dropdownX, y + 4f, 300f, rowHeight - 8f);
                Widgets.Label(modLabelRect, "CR.DataDefinedByModExtension".Translate());
            }
            else
            {
                // PawnKind dropdown (aligned under the header label)
                var current = selectedFactionPawnKinds[faction];
                string pawnLabel = current != null ? current.LabelCap : "CR.Default".Translate();

                float pawnWidth = 120f;
                // Calculate dropdown X relative to panel so it lines up with pawnHeaderRect
                float dropdownX = headerRect.x + 220f;
                Rect dropRect = new(dropdownX, y + 4f, pawnWidth, rowHeight - 8f);
                if (Widgets.ButtonText(dropRect, pawnLabel))
                    Find.WindowStack.Add(new FloatMenu(GeneratePawnKindOptions(faction)));
            }

            y += rowHeight;
        }

        Widgets.EndScrollView();

        // --- Footer label ---
        Widgets.Label(footerRect, "CR.SelectedFactions".Translate(SelectedFactionsCount));
    }

    private void AddPerkPanel(Rect panel)
    {
        Widgets.DrawMenuSection(panel);

        // --- Header ---
        float headerHeight = 30f;
        Rect headerRect = new(panel.x + 5f, panel.y + 5f, panel.width - 10f, 25f);
        Widgets.Label(headerRect, "CR.SelectAvailablePerks".Translate());

        // --- Footer ---
        float footerHeight = 25f;
        Rect footerRect = new(panel.x + 5f, panel.yMax - footerHeight - 5f, panel.width - 10f, footerHeight);

        // --- Scroll window (visible area) ---
        float scrollY = headerRect.yMax + 5f;
        float scrollHeight = panel.height - headerHeight - footerHeight - 15f;

        Rect scrollOuter = new(panel.x + 5f, scrollY, panel.width - 10f, scrollHeight);

        // --- Inner scroll content (local 0,0 coords) ---
        float rowHeight = 28f;
        Rect viewRect = new(0, 0, scrollOuter.width - 20f, selectedPerks.Count * rowHeight);

        Widgets.BeginScrollView(scrollOuter, ref perkScroll, viewRect);

        // Draw rows locally inside the scroll area
        float y = 0f;

        foreach (var perk in selectedPerks.Keys.ToList())
        {
            bool selected = selectedPerks[perk];
            Rect row = new(5f, y, viewRect.width - 10f, rowHeight);

            Widgets.CheckboxLabeled(row, perk.LabelCap, ref selected);
            selectedPerks[perk] = selected;

            TooltipHandler.TipRegion(row, perk.description);

            y += rowHeight;
        }

        Widgets.EndScrollView();

        // --- Footer label ---
        Widgets.Label(footerRect, "CR.SelectedPerks".Translate(SelectedPerksCount));
    }

    private void AddSenatorDistribution(Rect inRect, float curY)
    {
        // Slider label
        Widgets.Label(new Rect(padding, curY, 350f, 30f), "CR.NumberOfSenatorsPerFaction".Translate(numOfSenatorsPerfaction));

        Rect sliderRect = new(padding, curY + 25f, inRect.width - 20f, 28f);

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
    }

    private void AddInformationalDisplay(ref Rect inRect, ref float curY)
    {
        curY += 30f;
        var totalNumberOfSenators = SelectedFactionsCount * numOfSenatorsPerfaction;
        Widgets.Label(new Rect(padding, curY, inRect.width, 25f), "CR.TotalNumberOfSenators".Translate(totalNumberOfSenators));
        curY += 30f;
        if(totalNumberOfSenators > SelectedPerksCount)
            Widgets.Label(new Rect(padding, curY, inRect.width, 25f), "CR.MoreSenatorsThanPerks".Translate());
        curY += 30f;
        if (totalNumberOfSenators < SelectedPerksCount)
            Widgets.Label(new Rect(padding, curY, inRect.width, 25f), "CR.LessSenatorsThanPerks".Translate());
        curY += 30f;

        if(SelectedFactionsCount > 7)
        {
            Widgets.Label(new Rect(padding, curY, inRect.width, 25f), "CR.MoreThan7FactionsWarning".Translate());
            curY += 30f;
        }
    }

    private List<FloatMenuOption> GeneratePawnKindOptions(FactionDef factionDef)
    {
        var opts = new List<FloatMenuOption>
        {
            new("CR.Default".Translate(), () =>
            {
                selectedFactionPawnKinds[factionDef] = null;
            })
        };

        foreach (var kind in GetPawnKindsForFaction(factionDef))
        {
            opts.Add(new FloatMenuOption(kind.label, () =>
            {
                selectedFactionPawnKinds[factionDef] = kind;
            }));
        }

        return opts;
    }

    // defaultFactionDef alone misses most humanlike kinds (e.g. tribal warriors), which are
    // only linked to a faction through its pawnGroupMakers, not the reverse field on the kind.
    private static IEnumerable<PawnKindDef> GetPawnKindsForFaction(FactionDef factionDef)
    {
        var kinds = new HashSet<PawnKindDef>(
            DefDatabase<PawnKindDef>.AllDefs.Where(k => k.defaultFactionDef == factionDef));

        if (factionDef.pawnGroupMakers != null)
        {
            foreach (var maker in factionDef.pawnGroupMakers)
            {
                AddKinds(maker.options);
                AddKinds(maker.traders);
                AddKinds(maker.carriers);
            }
        }

        return kinds.Where(k => k.race?.race?.Humanlike == true).OrderBy(k => k.label);

        void AddKinds(List<PawnGenOption>? options)
        {
            if (options == null) return;
            foreach (var option in options)
                if (option.kind != null)
                    kinds.Add(option.kind);
        }
    }

    private void SaveSelections()
    {
        var comp = GameComponent_Republic.Instance;
        if (comp is null)
            return;
        comp.rules.ignoreTechprintResearch = ignoreTechprintResearch;
        comp.rules.useDummyResearch = useDummyResearch;
        comp.rules.numOfSenatorsPerFaction = numOfSenatorsPerfaction;
        comp.rules.selectedFactionDefs = [.. selectedFactions.Where(kvp => kvp.Value).Select(kvp => kvp.Key.defName)];
        comp.rules.pawnKindPerFaction = selectedFactionPawnKinds
            .ToDictionary(kvp => kvp.Key.defName, kvp => kvp.Value?.defName);
        comp.rules.selectedPerkDefs = [.. selectedPerks.Where(kvp => kvp.Value).Select(kvp => kvp.Key.defName)];
        RepublicStateBuilder.BuildFromRules();
    }
}
