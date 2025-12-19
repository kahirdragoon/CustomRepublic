using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using VFEC.Perks;

namespace Custom_Republic;

public static class RepublicStateBuilder
{
    public static void BuildFromRules()
    {
        var comp = Current.Game.GetComponent<GameComponent_Republic>();
        var rules = comp.rules;
        var state = new RepublicState();

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
        var researchPool = ResolveResearchPool(rules, factionDefs.Count);

        // --- Determine available senators ---
        int availableSenators = Math.Min(researchPool.nonFinal.Count + researchPool.final.Count, perkDefs.Count);

        int senatorsPerFactionFallback = Math.Max(1, rules.numOfSenatorsPerFaction);

        int averagePerFaction = 0;
        int remainingSenators = availableSenators;

        if (rules.autoCalculateSenatorsPerFaction && factionDefs.Count > 0)
        {
            averagePerFaction = Math.Min(availableSenators / factionDefs.Count, 5);
        }

        int perkIndex = 0;
        int researchIndex = 0;
        int finalResearchIndex = 0;

        // Shuffle pools once for randomness
        perkDefs = perkDefs.InRandomOrder().ToList();
        researchPool.nonFinal = researchPool.nonFinal.InRandomOrder().ToList();

        foreach (var faction in factionDefs)
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
            var senatorResearch = new List<string>();
            for (int i = 0; i < numSenators; i++)
            {
                senatorResearch.Add(researchPool.nonFinal[researchIndex].defName);
                researchIndex = (researchIndex + 1) % researchPool.nonFinal.Count;
            }

            string finalResearch = researchPool.final[finalResearchIndex].defName;
            finalResearchIndex = (finalResearchIndex + 1) % researchPool.final.Count;

            // --- PawnKind ---
            rules.pawnKindPerFaction.TryGetValue(faction.defName, out var pawnKindDef);

            state.factionStates.Add(new RepublicStateFaction
            {
                factionDefName = faction.defName,
                FactionDef = faction,
                numSenators = numSenators,
                senatorPerks = senatorPerks,
                finalPerk = finalPerk,
                senatorResearch = senatorResearch,
                finalResearch = finalResearch,
                pawnKindDef = pawnKindDef,
            });
        }

        comp.state = state;
    }

    private static (List<ResearchProjectDef> nonFinal, List<ResearchProjectDef> final) ResolveResearchPool(RepublicRules rules, int factionCount)
    {
        bool useAllTechLevels = rules.allowedTechLevels == null || rules.allowedTechLevels.Count == 0;

        var allAllowed = DefDatabase<ResearchProjectDef>.AllDefsListForReading
            .Where(r => useAllTechLevels ||  rules.allowedTechLevels!.Contains(r.techLevel))
            .ToList();

        var techprint = allAllowed.Where(r => r.techprintCount > 0).ToList();
        var nonTechprint = allAllowed.Except(techprint).ToList();

        var final = new List<ResearchProjectDef>();

        if (rules.onlyTechprintResearch)
        {
            final.AddRange(techprint.Take(factionCount));
            techprint.RemoveRange(0, Math.Min(factionCount, techprint.Count));
        }
        else if (rules.prioritizeTechprintResearch)
        {
            final.AddRange(techprint.Take(factionCount));
            techprint.RemoveRange(0, Math.Min(factionCount, techprint.Count));

            if (final.Count < factionCount)
            {
                int needed = factionCount - final.Count;
                final.AddRange(nonTechprint.Take(needed));
                nonTechprint.RemoveRange(0, Math.Min(needed, nonTechprint.Count));
            }
        }
        else
        {
            final.AddRange(allAllowed.Take(factionCount));
            foreach (var r in final)
                nonTechprint.Remove(r);
        }

        var nonFinal = techprint.Concat(nonTechprint).ToList();

        if (final.Count == 0 && nonFinal.Count > 0)
            final.Add(nonFinal[0]);

        return (nonFinal, final);
    }
}

