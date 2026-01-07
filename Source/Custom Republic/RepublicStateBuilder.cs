using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using VFEC.Perks;

namespace CustomRepublic;

public static class RepublicStateBuilder
{
    public static void BuildFromRules()
    {
        var comp = Current.Game.GetComponent<GameComponent_Republic>();
        var rules = comp.rules;
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
            .Where(r => r.knowledgeCost == 0 || r.knowledgeCategory == null);
        var availableResearch = nonAnomlyResearch.ToList();

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
            if (availableResearch.Count == 0)
                availableResearch = nonAnomlyResearch.ToList();
            var availableFinalResearch = availableResearch
                .Where(r => r.techLevel == factionDef.techLevel && (rules.ignoreTechprintResearch || r.techprintCount > 0));
            if (!availableFinalResearch.Any())
                availableFinalResearch = availableResearch.Where(r => r.techLevel == factionDef.techLevel);
            if (!availableFinalResearch.Any())
            {
                Log.ErrorOnce($"No available research projects for faction {factionDef.defName} with tech level {factionDef.techLevel}", factionDef.GetHashCode());
                return;
            }
            var finalResearch = availableFinalResearch.RandomElement();
            availableResearch.Remove(finalResearch);

            if (availableResearch.Count < numSenators)
                availableResearch = nonAnomlyResearch.ToList();
            var availableSenatorResearch = availableResearch.Where(r => r.techLevel == factionDef.techLevel);
            if (availableSenatorResearch.Count() < numSenators)
            {
                Log.ErrorOnce($"No enough available research projects for faction {factionDef.defName} with tech level {factionDef.techLevel}", factionDef.GetHashCode());
                return;
            }
            var senatorResearch = availableSenatorResearch.TakeRandomDistinct(numSenators);
            availableResearch.RemoveAll(r => senatorResearch.Contains(r));

            // --- PawnKind ---
            rules.pawnKindPerFaction.TryGetValue(factionDef.defName, out var pawnKindDef);

            state.factionStates.Add(new RepublicStateFaction
            {
                factionDefName = factionDef.defName,
                factionDef = factionDef,
                numSenators = numSenators,
                senatorPerks = senatorPerks,
                finalPerk = finalPerk,
                senatorResearch = senatorResearch.Select(r => r.defName).ToList(),
                finalResearch = finalResearch.defName,
                pawnKindDef = pawnKindDef,
            });
        }
    }

    public static RepublicStateFaction BuildFactionState(FactionDef factionDef, int numSenators)
    {
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
        var perkDefs =  availablePerkDefs.Where(p => !usedPerks.Contains(p.defName)).TakeRandomDistinct(numPerksNeeded).ToList();
        if (perkDefs.Count < numPerksNeeded)
            perkDefs.AddRange(perkDefs.TakeRandom(numPerksNeeded - perkDefs.Count));
        var senatorPerks = perkDefs.Take(numSenators).Select(p => p.defName).ToList();
        string finalPerk = perkDefs.TakeLast(1).First().defName;

        // --- Research ---
        var nonAnomlyResearch = DefDatabase<ResearchProjectDef>.AllDefsListForReading
            .Where(r => r.knowledgeCost == 0 || r.knowledgeCategory == null);
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

        Log.Message($"[Custom Republic] Built faction state for {factionDef.defName} with {numSenators} senators.");
        rules.pawnKindPerFaction.TryGetValue(factionDef.defName, out var senatorPawnKind);
        Log.Message($"[Custom Republic] Assigned pawn kind {senatorPawnKind} to faction {factionDef.defName}.");

        return new RepublicStateFaction
        {
            factionDefName = factionDef.defName,
            factionDef = factionDef,
            numSenators = numSenators,
            senatorPerks = senatorPerks,
            finalPerk = finalPerk,
            senatorResearch = senatorResearch.Select(r => r.defName).ToList(),
            finalResearch = finalResearch.defName,
            pawnKindDef = senatorPawnKind
        };
    }
}

