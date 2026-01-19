using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using VFEC.Perks;

namespace CustomRepublic;

public static class RepublicStateBuilder
{
    private static ResearchProjectDef dummyResearchProjectDef = DefDatabase<ResearchProjectDef>.GetNamedSilentFail("Stonecutting") ??
        DefDatabase<ResearchProjectDef>.GetNamedSilentFail("PassiveCooler") ??
        DefDatabase<ResearchProjectDef>.GetNamedSilentFail("Brewing");

    public static void BuildFromRules()
    {
        var comp = Current.Game.GetComponent<GameComponent_Republic>();
        var rules = comp.rules;
        comp.state = new();
        var state = comp.state;

        // --- Resolve defs from rules ---
        var factionDefs = rules.selectedFactionDefs
            .Select(DefDatabase<FactionDef>.GetNamedSilentFail)
            .Where(f => f != null)
            .ToList();

        var perkDefs = rules.selectedPerkDefs
            .Select(DefDatabase<PerkDef>.GetNamedSilentFail)
            .Where(p => p != null)
            .ToList();

        // --- Resolve research projects ---
        var nonAnomlyResearch = DefDatabase<ResearchProjectDef>.AllDefsListForReading
            .Where(r => (r.knowledgeCost == 0 || r.knowledgeCategory == null) && r != dummyResearchProjectDef);
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

        perkDefs = perkDefs.InRandomOrder().ToList();

        foreach (var factionDef in factionDefs)
        {
            var senatorInfoModExt = factionDef.GetModExtension<FactionExtension_SenatorInfoExtended>();
            if (senatorInfoModExt != null)
            {

                usedResearchDefs.UnionWith(senatorInfoModExt.senatorResearch.Select(r => r.defName));
                //availableResearch.RemoveAll(r => senatorInfoModExt.senatorResearch.Contains(r));
                perkDefs = perkDefs.Where(p => !senatorInfoModExt.senatorPerks.Contains(p)).ToList();

                state.factionStates.Add(new RepublicStateFaction
                {
                    factionDefName = factionDef.defName,
                    factionDef = factionDef,
                    numSenators = senatorInfoModExt.numSenators,
                    senatorPerks = senatorInfoModExt.senatorPerks.Select(p => p.defName).ToList(),
                    finalPerk = senatorInfoModExt.finalPerk.defName,
                    senatorResearch = senatorInfoModExt.senatorResearch.Select(r => r.defName).ToList(),
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

                // --- Perks ---
                var senatorPerks = new List<string>();
                for (int i = 0; i < numSenators; i++)
                {
                    senatorPerks.Add(perkDefs[perkIndex].defName);
                    perkIndex = (perkIndex + 1) % perkDefs.Count;
                }

                string finalPerk = perkDefs[perkIndex].defName;
                perkIndex = (perkIndex + 1) % perkDefs.Count;

                // --- Research ---
                string finalResearchDefName = dummyResearchProjectDef.defName;
                List<string> senatorResearchDefNames = Enumerable.Repeat(dummyResearchProjectDef.defName, numSenators).ToList();

                if (!rules.useDummyResearch)
                {
                    var finalResearchProjects = FindSuitableResearchProjects(factionDef.techLevel, rules.ignoreTechprintResearch, usedResearchDefs, 1);
                    if (!finalResearchProjects.Any())
                    {
                        Log.Warning($"No available research projects for faction {factionDef.defName} with tech level {factionDef.techLevel}. Choosing dummy research.");
                        finalResearchDefName = dummyResearchProjectDef.defName;
                    }
                    var finalResearch = finalResearchProjects[0];

                    var senatorResearch = FindSuitableResearchProjects(factionDef.techLevel, false, usedResearchDefs, numSenators);
                    if (!senatorResearch.Any())
                    {
                        Log.Warning($"No available research projects for faction {factionDef.defName} with tech level {factionDef.techLevel}. Choosing dummy research.");
                        senatorResearch = Enumerable.Repeat(dummyResearchProjectDef, numSenators).ToList();
                    }

                    finalResearchDefName = finalResearch.defName;
                    senatorResearchDefNames = senatorResearch.Select(r => r.defName).ToList();
                }

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

    private static List<ResearchProjectDef> FindSuitableResearchProjects(TechLevel techLevel, bool ignoreTechprintResearch, HashSet<string> usedResearchDefs, int count)
    {
        var nonAnomlyResearch = DefDatabase<ResearchProjectDef>.AllDefsListForReading
            .Where(r => (r.knowledgeCost == 0 || r.knowledgeCategory == null) && r != dummyResearchProjectDef);
        var availableResearch = nonAnomlyResearch
            .Where(r => r.techLevel == techLevel && (ignoreTechprintResearch || r.techprintCount > 0) && !usedResearchDefs.Contains(r.defName));
        if (!availableResearch.Any())
            availableResearch = nonAnomlyResearch.Where(r => r.techLevel == techLevel && !usedResearchDefs.Contains(r.defName));
        if (!availableResearch.Any())
            availableResearch = nonAnomlyResearch.Where(r => r.techLevel == techLevel && (ignoreTechprintResearch || r.techprintCount > 0));
        if (!availableResearch.Any())
            availableResearch = nonAnomlyResearch.Where(r => r.techLevel == techLevel);
        var choosenResearch = availableResearch.TakeRandomDistinct(count).ToList();
        usedResearchDefs.UnionWith(choosenResearch.Select(r => r.defName));

        if(choosenResearch.Count < count)
        {
            var chosenResearch2 = FindSuitableResearchProjects(techLevel, ignoreTechprintResearch, usedResearchDefs, count - choosenResearch.Count);
            choosenResearch.AddRange(chosenResearch2);
            usedResearchDefs.UnionWith(chosenResearch2.Select(r => r.defName));
        }

        return choosenResearch;
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

        var rules = Current.Game.GetComponent<GameComponent_Republic>().rules;

        rules.selectedFactionDefs.Add(factionDef.defName);

        var availablePerkDefs = rules.selectedPerkDefs
            .Select(DefDatabase<PerkDef>.GetNamedSilentFail)
            .Where(p => p != null)
            .ToList();

        numSenators = Math.Min(numSenators, 5);
        var numPerksNeeded = numSenators + 1;

        var republicFactions = Current.Game.GetComponent<GameComponent_Republic>().state.factionStates;
        var usedPerks = republicFactions.SelectMany(f => f.senatorPerks).ToHashSet();
        var perkDefs = availablePerkDefs.Where(p => !usedPerks.Contains(p.defName)).TakeRandomDistinct(numPerksNeeded).ToList();
        if (perkDefs.Count < numPerksNeeded)
            perkDefs.AddRange(perkDefs.TakeRandom(numPerksNeeded - perkDefs.Count));
        var senatorPerks = perkDefs.Take(numSenators).Select(p => p.defName).ToList();
        string finalPerk = perkDefs.TakeLast(1).First().defName;
        // --- Research ---
        string finalResearchDefName = dummyResearchProjectDef.defName;
        List<string> senatorResearchDefNames = Enumerable.Repeat(dummyResearchProjectDef.defName, numSenators).ToList();

        if (!rules.useDummyResearch)
        {
            var nonAnomlyResearch = DefDatabase<ResearchProjectDef>.AllDefsListForReading
                .Where(r => (r.knowledgeCost == 0 || r.knowledgeCategory == null) && r != dummyResearchProjectDef);
            var availableResearch = nonAnomlyResearch.ToList();
            if (availableResearch.Count == 0)
                availableResearch = nonAnomlyResearch.ToList();
            var availableFinalResearch = availableResearch
                .Where(r => r.techLevel == factionDef.techLevel && (rules.ignoreTechprintResearch || r.techprintCount > 0));
            if (!availableFinalResearch.Any())
                availableFinalResearch = availableResearch.Where(r => r.techLevel == factionDef.techLevel);
            if (!availableFinalResearch.Any())
            {
                Log.ErrorOnce($"No available research projects for faction {factionDef.defName} with tech level {factionDef.techLevel}", factionDef.GetHashCode());
            }
            var finalResearch = availableFinalResearch
                .RandomElement();
            availableResearch.Remove(finalResearch);

            if (availableResearch.Count < numSenators)
                availableResearch = nonAnomlyResearch.ToList();
            var availableSenatorResearch = availableResearch
                .Where(r => r.techLevel == factionDef.techLevel);
            if (availableSenatorResearch.Count() < numSenators)
            {
                Log.ErrorOnce($"No enough available research projects for faction {factionDef.defName} with tech level {factionDef.techLevel}", factionDef.GetHashCode());
            }
            var senatorResearch = availableSenatorResearch.TakeRandomDistinct(numSenators);
            availableResearch.RemoveAll(r => senatorResearch.Contains(r));

        }

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

