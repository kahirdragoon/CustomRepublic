using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using VFEC.Perks;

namespace CustomRepublic;

public static class RepublicStateBuilder
{
    private static ResearchProjectDef dummyResearchProjectDef = 
        DefDatabase<ResearchProjectDef>.GetNamedSilentFail("VBY_PrimitiveStonecutting") ??
        DefDatabase<ResearchProjectDef>.GetNamedSilentFail("Stonecutting") ??
        DefDatabase<ResearchProjectDef>.GetNamedSilentFail("PassiveCooler") ??
        DefDatabase<ResearchProjectDef>.GetNamedSilentFail("Brewing");

    // Node Research (ferny.noderesearch) generates "BRM_Emergence_<Level>" projects that advance the
    // player's tech level when finished; matched by type name to avoid a hard assembly reference.
    private const string NodeResearchEmergenceExtension = "BetterResearchMenu.EmergenceExtension";

    private static bool IsSelectableResearch(ResearchProjectDef r) =>
        (r.knowledgeCost == 0 || r.knowledgeCategory == null)
        && r != dummyResearchProjectDef
        && !IsTechAdvancingResearch(r);

    private static bool IsTechprintLocked(ResearchProjectDef r) => r.techprintCount > 0;

    private static bool IsTechAdvancingResearch(ResearchProjectDef r) =>
        r.modExtensions != null && r.modExtensions.Any(e => e.GetType().FullName == NodeResearchEmergenceExtension);

    public static bool IsSelectablePerk(PerkDef p) => p.defName != "VeniVidiVici";

    // An empty selection means "all perks", the same way Dialog_SelectRepublicFactions displays it. Games where
    // the republic dialog was never confirmed (e.g. a republic started by inviting a faction) have no perks selected.
    private static List<PerkDef> ResolvePerkPool(RepublicRules rules)
    {
        var selected = rules.selectedPerkDefs
            .Select(DefDatabase<PerkDef>.GetNamedSilentFail)
            .Where(p => p != null)
            .ToList();
        return selected.Count > 0 ? selected : [.. DefDatabase<PerkDef>.AllDefsListForReading.Where(IsSelectablePerk)];
    }

    public static void BuildFromRules()
    {
        var comp = GameComponent_Republic.Instance;
        if (comp == null)
        {
            Log.Error("GameComponent_Republic instance is null. Cannot build republic state.");
            return;
        }
        var rules = comp.rules;
        comp.state = new();
        var state = comp.state;

        // --- Resolve defs from rules ---
        var factionDefs = rules.selectedFactionDefs
            .Select(DefDatabase<FactionDef>.GetNamedSilentFail)
            .Where(f => f != null)
            .ToList();

        var perkDefs = ResolvePerkPool(rules);

        // --- Resolve research projects ---
        var nonAnomlyResearch = DefDatabase<ResearchProjectDef>.AllDefsListForReading
            .Where(IsSelectableResearch);
        //var availableResearch = nonAnomlyResearch.ToList();
        var usedResearchDefs = new HashSet<string>();

        // --- Determine available senators ---
        int availableSenators = Math.Min(1, perkDefs.Count);

        int senatorsPerFactionFallback = Math.Max(1, rules.numOfSenatorsPerFaction);

        int averagePerFaction = 0;
        int remainingSenators = availableSenators;

        if (rules.autoCalculateSenatorsPerFaction && factionDefs.Count > 0)
        {
            averagePerFaction = Math.Min(availableSenators / factionDefs.Count, 5);
        }

        int perkIndex = 0;

        perkDefs = [.. perkDefs.InRandomOrder()];

        foreach (var factionDef in factionDefs)
        {
            var senatorInfoModExt = factionDef.GetModExtension<FactionExtension_SenatorInfoExtended>();
            if (senatorInfoModExt != null)
            {

                usedResearchDefs.UnionWith(senatorInfoModExt.senatorResearch.Select(r => r.defName));
                //availableResearch.RemoveAll(r => senatorInfoModExt.senatorResearch.Contains(r));
                // Keep the pool non-empty: the perk indexing below cycles through it.
                var remainingPerks = perkDefs.Where(p => !senatorInfoModExt.senatorPerks.Contains(p)).ToList();
                if (remainingPerks.Count > 0)
                    perkDefs = remainingPerks;

                state.factionStates.Add(new RepublicStateFaction
                {
                    factionDefName = factionDef.defName,
                    factionDef = factionDef,
                    numSenators = senatorInfoModExt.numSenators,
                    senatorPerks = [.. senatorInfoModExt.senatorPerks.Select(p => p.defName)],
                    finalPerk = senatorInfoModExt.finalPerk.defName,
                    senatorResearch = [.. senatorInfoModExt.senatorResearch.Select(r => r.defName)],
                    finalResearch = senatorInfoModExt.finalResearch.defName,
                    pawnKindDef = senatorInfoModExt.senatorPawnKindDef?.defName,
                });
            }
            else
            {
                int numSenators;

                if (rules.autoCalculateSenatorsPerFaction)
                {
                    numSenators = Math.Min(averagePerFaction, remainingSenators);
                    remainingSenators -= numSenators;
                }
                else
                {
                    numSenators = senatorsPerFactionFallback;
                }

                numSenators = Math.Min(numSenators, 5);
                if (numSenators <= 0) continue;
                if (perkDefs.Count == 0)
                {
                    Log.Error($"No perks available for republic faction {factionDef.defName}. Skipping it.");
                    continue;
                }

                // --- Perks ---
                // The pool can shrink between factions (see above), so wrap the index before every use.
                var senatorPerks = new List<string>();
                for (int i = 0; i < numSenators; i++)
                {
                    perkIndex %= perkDefs.Count;
                    senatorPerks.Add(perkDefs[perkIndex].defName);
                    perkIndex++;
                }

                perkIndex %= perkDefs.Count;
                string finalPerk = perkDefs[perkIndex].defName;
                perkIndex++;

                // --- Research ---
                var (finalResearchDefName, senatorResearchDefNames) = ChooseResearch(factionDef, numSenators, rules, usedResearchDefs);

                // --- PawnKind ---
                rules.pawnKindPerFaction.TryGetValue(factionDef.defName, out var pawnKindDef);

                state.factionStates.Add(new RepublicStateFaction
                {
                    factionDefName = factionDef.defName,
                    factionDef = factionDef,
                    numSenators = numSenators,
                    senatorPerks = senatorPerks,
                    finalPerk = finalPerk,
                    senatorResearch = senatorResearchDefNames,
                    finalResearch = finalResearchDefName,
                    pawnKindDef = pawnKindDef,
                });
            }
        }
    }

    // Returns the faction's final research and one research per senator, falling back to the dummy research
    // when disabled by the rules or when nothing is available at the faction's tech level.
    private static (string finalResearch, List<string> senatorResearch) ChooseResearch(FactionDef factionDef, int numSenators, RepublicRules rules, HashSet<string> usedResearchDefs)
    {
        if (!rules.useDummyResearch)
        {
            var finalResearch = FindSuitableResearchProjects(factionDef.techLevel, rules.ignoreTechprintResearch, usedResearchDefs, 1);
            var senatorResearch = FindSuitableResearchProjects(factionDef.techLevel, rules.ignoreTechprintResearch, usedResearchDefs, numSenators);
            if (finalResearch.Count == 1 && senatorResearch.Count == numSenators)
                return (finalResearch[0].defName, senatorResearch.Select(r => r.defName).ToList());

            Log.Warning($"No available research projects for faction {factionDef.defName} with tech level {factionDef.techLevel}. Choosing dummy research.");
        }

        string dummy = dummyResearchProjectDef.defName;
        return (dummy, Enumerable.Repeat(dummy, numSenators).ToList());
    }

    // Picks `count` research projects at the given tech level, preferring ones no faction uses yet.
    // With ignoreTechprintResearch, techprint-locked projects are never chosen; otherwise they are preferred.
    // Returns exactly `count` projects, or an empty list when nothing selectable exists at that tech level.
    private static List<ResearchProjectDef> FindSuitableResearchProjects(TechLevel techLevel, bool ignoreTechprintResearch, HashSet<string> usedResearchDefs, int count)
    {
        var chosen = new List<ResearchProjectDef>();
        var atTechLevel = DefDatabase<ResearchProjectDef>.AllDefsListForReading
            .Where(r => r.techLevel == techLevel && IsSelectableResearch(r) && !(ignoreTechprintResearch && IsTechprintLocked(r)))
            .ToList();
        if (atTechLevel.Count == 0)
            return chosen;

        bool Unused(ResearchProjectDef r) => !usedResearchDefs.Contains(r.defName);

        // Constraints are relaxed step by step: unused before reused by other factions, reused before
        // duplicated within this faction. The last step always matches, so every pass picks at least one.
        var preferences = new Func<ResearchProjectDef, bool>[]
        {
            r => IsTechprintLocked(r) && Unused(r),
            Unused,
            r => IsTechprintLocked(r) && !chosen.Contains(r),
            r => !chosen.Contains(r),
            _ => true,
        };

        while (chosen.Count < count)
        {
            var candidates = preferences
                .Select(p => atTechLevel.Where(p).ToList())
                .First(c => c.Count > 0);
            var picked = candidates.InRandomOrder().Take(count - chosen.Count).ToList();
            chosen.AddRange(picked);
            usedResearchDefs.UnionWith(picked.Select(r => r.defName));
        }

        return chosen;
    }

    public static RepublicStateFaction BuildFactionState(FactionDef factionDef, int numSenators)
    {
        var senatorInfoModExt = factionDef.GetModExtension<FactionExtension_SenatorInfoExtended>();
        if (senatorInfoModExt != null)
        {
            return new RepublicStateFaction
            {
                factionDefName = factionDef.defName,
                factionDef = factionDef,
                numSenators = senatorInfoModExt.numSenators,
                senatorPerks = [.. senatorInfoModExt.senatorPerks.Select(p => p.defName)],
                finalPerk = senatorInfoModExt.finalPerk.defName,
                senatorResearch = [.. senatorInfoModExt.senatorResearch.Select(r => r.defName)],
                finalResearch = senatorInfoModExt.finalResearch.defName,
                pawnKindDef = senatorInfoModExt.senatorPawnKindDef?.defName,
            };
        }

        var rules = GameComponent_Republic.Instance?.rules;
        if (rules == null)
        {
            Log.Error("GameComponent_Republic instance or rules is null. Cannot build faction state.");
            return new RepublicStateFaction
            {
                factionDefName = factionDef.defName,
                factionDef = factionDef,
                numSenators = 0,
                senatorPerks = [],
                finalPerk = "",
                senatorResearch = [],
                finalResearch = "",
                pawnKindDef = null
            };
        }

        var availablePerkDefs = ResolvePerkPool(rules);
        if (availablePerkDefs.Count == 0)
        {
            Log.Error($"No perks available for republic faction {factionDef.defName}. Cannot build faction state.");
            return new RepublicStateFaction
            {
                factionDefName = factionDef.defName,
                factionDef = factionDef,
                numSenators = 0,
                senatorPerks = [],
                finalPerk = "",
                senatorResearch = [],
                finalResearch = "",
                pawnKindDef = null
            };
        }

        if (!rules.selectedFactionDefs.Contains(factionDef.defName))
            rules.selectedFactionDefs.Add(factionDef.defName);

        numSenators = Math.Min(numSenators, 5);
        var numPerksNeeded = numSenators + 1;

        var republicFactions = GameComponent_Republic.Instance?.state.factionStates;
        if (republicFactions == null)
        {
            Log.Error("GameComponent_Republic instance or state factionStates is null. Cannot determine used perks for faction state.");
            republicFactions = [];
        }
        var usedPerks = republicFactions.SelectMany(f => f.senatorPerks.Append(f.finalPerk)).ToHashSet();
        // Perks no other faction holds come first; once the pool runs out, perks are reused (cycling if needed).
        var orderedPerks = availablePerkDefs.Where(p => !usedPerks.Contains(p.defName)).InRandomOrder()
            .Concat(availablePerkDefs.Where(p => usedPerks.Contains(p.defName)).InRandomOrder())
            .ToList();
        var perkDefs = Enumerable.Range(0, numPerksNeeded).Select(i => orderedPerks[i % orderedPerks.Count]).ToList();
        var senatorPerks = perkDefs.Take(numSenators).Select(p => p.defName).ToList();
        string finalPerk = perkDefs[numSenators].defName;
        // --- Research ---
        var usedResearchDefs = republicFactions
            .SelectMany(f => f.senatorResearch.Append(f.finalResearch))
            .ToHashSet();
        var (finalResearchDefName, senatorResearchDefNames) = ChooseResearch(factionDef, numSenators, rules, usedResearchDefs);

        rules.pawnKindPerFaction.TryGetValue(factionDef.defName, out var senatorPawnKind);

        return new RepublicStateFaction
        {
            factionDefName = factionDef.defName,
            factionDef = factionDef,
            numSenators = numSenators,
            senatorPerks = senatorPerks,
            finalPerk = finalPerk,
            senatorResearch = senatorResearchDefNames,
            finalResearch = finalResearchDefName,
            pawnKindDef = senatorPawnKind
        };
    }
}

