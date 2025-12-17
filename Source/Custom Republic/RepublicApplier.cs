using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using VFEC.Perks;
using VFEC.Senators;

namespace Custom_Republic;
public static class RepublicApplier
{
    public static void Apply(RepublicRules rules, RepublicState republicState)
    {
        if (rules.selectedFactionDefs.NullOrEmpty())
            return;

        // 1. Clear existing extensions
        foreach (var f in DefDatabase<FactionDef>.AllDefsListForReading)
            f.modExtensions?.RemoveAll(m => m is FactionExtension_SenatorInfoExtended);

        // 2. Resolve faction defs
        var factions = rules.selectedFactionDefs
            .Select(factionDefName => {
                var faction = DefDatabase<FactionDef>.GetNamedSilentFail(factionDefName);
                if (faction == null)
                    Log.Error($"[RepublicApplier] FactionDef '{factionDefName}' not found.");
                return faction;
            })
            .Where(f => f != null)
            .ToList();

        // 3. Apply RepublicDef parts
        var republic = DefDatabase<RepublicDef>.GetNamed("VFEC_Republic");
        republic.parts.Clear();
        republic.parts.AddRange(factions);

        // 4. Build perk pool
        var perkPool = rules.selectedPerkDefs
            .Select(perkDefName => DefDatabase<PerkDef>.GetNamed(perkDefName))
            .Where(p => p.defName != "VeniVidiVici")
            .InRandomOrder()
            .ToList();

        // 5. Build research pool
        var useAllTechLevels = rules.allowedTechLevels == null || rules.allowedTechLevels.Count == 0;

        var allowedResearch = DefDatabase<ResearchProjectDef>.AllDefsListForReading
            .Where(r => useAllTechLevels || rules.allowedTechLevels!.Contains(r.techLevel))
            .ToList();

        var techprintResearch = rules.prioritizeTechprintResearch
            ? allowedResearch.Where(r => r.techprintCount > 0).InRandomOrder().ToList()
            : new List<ResearchProjectDef>();

        var nonTechprintResearch = allowedResearch
            .Except(techprintResearch)
            .InRandomOrder()
            .ToList();

        // 6. Senator distribution setup
        int availableSenators = perkPool.Count;
        int selectedFactionCount = factions.Count;

        int averageSenators = 0;
        int remainingSenators = 0;

        if (rules.distributeSenatorsEvenly && selectedFactionCount > 0)
        {
            averageSenators = Math.Min(availableSenators / selectedFactionCount, 5);
            remainingSenators = availableSenators;
        }

        int perkIndex = 0;
        int researchIndex = 0;
        int finalResearchIndex = 0;

        // 7. Apply per faction
        foreach (var faction in factions)
        {
            var factionKey = faction.defName;

            // === ALREADY APPLIED → REAPPLY EXACT STATE ===
            if (republicState.selectedFactions.Contains(factionKey))
            {
                ApplyStoredFaction(faction, rules, republicState);
                continue;
            }

            // === NEW FACTION → COMPUTE ===
            int senatorsForFaction;

            if (rules.distributeSenatorsEvenly)
            {
                senatorsForFaction = Math.Min(averageSenators, remainingSenators);
                remainingSenators -= senatorsForFaction;
            }
            else
            {
                senatorsForFaction = rules.numOfSenatorsPerFaction;
            }

            senatorsForFaction = Math.Clamp(senatorsForFaction, 0, 5);

            // --- Perks ---
            var factionPerks = new List<PerkDef>();
            for (int i = 0; i <= senatorsForFaction; i++)
            {
                factionPerks.Add(perkPool[perkIndex % perkPool.Count]);
                perkIndex++;
            }

            var finalPerk = factionPerks[senatorsForFaction];
            var senatorPerks = factionPerks.Take(senatorsForFaction).ToList();

            // --- Research ---
            var senatorResearch = new List<ResearchProjectDef>();
            for (int i = 0; i < senatorsForFaction; i++)
            {
                if (researchIndex >= nonTechprintResearch.Count)
                    researchIndex = 0;

                senatorResearch.Add(nonTechprintResearch[researchIndex++]);
            }

            ResearchProjectDef? finalResearch = null;

            if (rules.prioritizeTechprintResearch && techprintResearch.Count > 0)
            {
                finalResearch = techprintResearch[finalResearchIndex % techprintResearch.Count];
                finalResearchIndex++;
            }
            else if (nonTechprintResearch.Count > 0)
            {
                finalResearch = nonTechprintResearch[researchIndex % nonTechprintResearch.Count];
            }

            // --- PawnKind ---
            PawnKindDef? pawnKind = null;
            if (rules.pawnKindPerFaction.TryGetValue(factionKey, out var pkName))
                pawnKind = DefDatabase<PawnKindDef>.GetNamedSilentFail(pkName);

            // --- Apply Extension ---
            faction.modExtensions ??= new List<DefModExtension>();
            faction.modExtensions.Add(new FactionExtension_SenatorInfoExtended
            {
                numSenators = senatorsForFaction,
                senatorPerks = senatorPerks,
                finalPerk = finalPerk,
                senatorResearch = senatorResearch,
                finalResearch = finalResearch,
                senatorPawnKindDef = pawnKind,
                perkBGPath = "UI/Perks/PerkBG_WesternRepublic"
            });

            // --- Persist applied state ---
            republicState.selectedFactions.Add(factionKey);
            republicState.senatorsPerFaction[factionKey] = senatorsForFaction;
            republicState.perksPerFaction[factionKey] = senatorPerks.Select(p => p.defName).ToList();
            republicState.finalPerkPerFaction[factionKey] = finalPerk.defName;
            republicState.researchPerFaction[factionKey] = senatorResearch.Select(r => r.defName).ToList();
            republicState.finalResearchPerFaction[factionKey] = finalResearch!.defName;
        }

        // 8. Update letter text
        republic.letterText =
            republic.letterText.Replace("3", factions.Count.ToString());
    }

    private static void ApplyStoredFaction(
        FactionDef faction,
        RepublicRules rules,
        RepublicState state)
    {
        var factionDefName = faction.defName;

        PawnKindDef? pawnKind = null;
        if (rules.pawnKindPerFaction.TryGetValue(factionDefName, out var pk))
            pawnKind = DefDatabase<PawnKindDef>.GetNamed(pk);

        faction.modExtensions ??= new List<DefModExtension>();
        faction.modExtensions.Add(new FactionExtension_SenatorInfoExtended
        {
            numSenators = state.senatorsPerFaction[factionDefName],
            senatorPerks = state.perksPerFaction[factionDefName]
                .Select(DefDatabase<PerkDef>.GetNamedSilentFail)
                .ToList(),
            finalPerk = DefDatabase<PerkDef>.GetNamed(state.finalPerkPerFaction[factionDefName]),
            senatorResearch = state.researchPerFaction[factionDefName]
                .Select(DefDatabase<ResearchProjectDef>.GetNamedSilentFail)
                .ToList(),
            finalResearch = DefDatabase<ResearchProjectDef>.GetNamed(state.finalResearchPerFaction[factionDefName]),
            senatorPawnKindDef = pawnKind,
            perkBGPath = "UI/Perks/PerkBG_WesternRepublic"
        });
    }
}
