using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;
using VFEC.Perks;

namespace CustomRepublic;

internal static class Patch_Tributum
{
    private static FieldInfo centralRepublicField = null!;
    private static MethodInfo getOwningFaction = null!;
    private static MethodInfo isRepublicFactionDef = null!;

    private static readonly MethodInfo factionManagerGetter =
        AccessTools.PropertyGetter(typeof(Find), nameof(Find.FactionManager));

    private static readonly MethodInfo firstFactionOfDef =
        AccessTools.Method(typeof(FactionManager), nameof(FactionManager.FirstFactionOfDef));

    public static void Apply()
    {
        var vfecAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "VFEC");
        if (vfecAssembly == null)
        {
            Log.Error("[Custom Republic] Patch_Tributum: VFEC assembly not found");
            return;
        }

        var vfecDefOf = vfecAssembly.GetType("VFEC.VFEC_DefOf");
        if (vfecDefOf == null)
        {
            Log.Error("[Custom Republic] Patch_Tributum: VFEC.VFEC_DefOf type not found");
            return;
        }

        centralRepublicField = AccessTools.Field(vfecDefOf, "VFEC_CentralRepublic");
        getOwningFaction = AccessTools.Method(typeof(Patch_Tributum), nameof(GetOwningFaction));
        isRepublicFactionDef = AccessTools.Method(typeof(Patch_Tributum), nameof(IsRepublicFactionDef));

        var tributumType = vfecAssembly.GetType("VFEC.Perks.Workers.Tributum");
        if (tributumType == null)
        {
            Log.Error("[Custom Republic] Patch_Tributum: Tributum type not found");
            return;
        }

        CustomRepublicMod.Harmony.Patch(
            AccessTools.Method(tributumType, "TickLong"),
            transpiler: new HarmonyMethod(typeof(Patch_Tributum), nameof(TranspilerTickLong)));

        CustomRepublicMod.Harmony.Patch(
            AccessTools.Method(tributumType, "ModifySellPrice"),
            transpiler: new HarmonyMethod(typeof(Patch_Tributum), nameof(TranspilerFactionDefCheck)));

        CustomRepublicMod.Harmony.Patch(
            AccessTools.Method(tributumType, "AddToTooltip"),
            transpiler: new HarmonyMethod(typeof(Patch_Tributum), nameof(TranspilerFactionDefCheck)));
    }

    // Returns the Faction whose senator currently holds this perk, falling back to VFEC_CentralRepublic.
    public static Faction GetOwningFaction(PerkWorker instance)
    {
        var state = GameComponent_Republic.Instance?.state;
        if (state != null && !state.factionStates.NullOrEmpty())
        {
            foreach (var factionState in state.factionStates)
            {
                if ((factionState.senatorPerks?.Contains(instance.def.defName) == true) ||
                    factionState.finalPerk == instance.def.defName)
                {
                    var faction = factionState.factionDef != null
                        ? Find.FactionManager.FirstFactionOfDef(factionState.factionDef)
                        : null;
                    if (faction != null)
                        return faction;
                }
            }
        }
        return Find.FactionManager.FirstFactionOfDef((FactionDef)centralRepublicField.GetValue(null));
    }

    // Returns true if def belongs to any republic faction (not just the hardcoded Central Republic).
    public static bool IsRepublicFactionDef(FactionDef def)
    {
        if (def == null) return false;
        var state = GameComponent_Republic.Instance?.state;
        if (state == null || state.factionStates.NullOrEmpty())
            return def == (FactionDef)centralRepublicField.GetValue(null);
        return state.factionStates.Any(f => f.factionDefName == def.defName);
    }

    // Replaces: Find.FactionManager.FirstFactionOfDef(VFEC_CentralRepublic)
    // With:     GetOwningFaction(this)
    public static IEnumerable<CodeInstruction> TranspilerTickLong(IEnumerable<CodeInstruction> instructions)
    {
        var list = instructions.ToList();

        for (int i = 1; i + 1 < list.Count; i++)
        {
            if (list[i].LoadsField(centralRepublicField) &&
                list[i - 1].Calls(factionManagerGetter) &&
                list[i + 1].Calls(firstFactionOfDef))
            {
                list[i - 1] = new CodeInstruction(OpCodes.Ldarg_0).WithLabels(list[i - 1].labels);
                list[i] = new CodeInstruction(OpCodes.Nop).WithLabels(list[i].labels);
                list[i + 1] = new CodeInstruction(OpCodes.Call, getOwningFaction).WithLabels(list[i + 1].labels);
                break;
            }
        }

        return list;
    }

    // Replaces equality comparisons against VFEC_CentralRepublic with IsRepublicFactionDef(def).
    // Handles: ceq, beq/beq.s (==) and bne.un/bne.un.s (!=) patterns.
    public static IEnumerable<CodeInstruction> TranspilerFactionDefCheck(IEnumerable<CodeInstruction> instructions)
    {
        var list = instructions.ToList();

        for (int i = 0; i + 1 < list.Count; i++)
        {
            if (!list[i].LoadsField(centralRepublicField))
                continue;

            var next = list[i + 1];
            // Nop out the ldsfld; the def value already sits under it on the stack.
            list[i] = new CodeInstruction(OpCodes.Nop).WithLabels(list[i].labels);

            if (next.opcode == OpCodes.Ceq)
            {
                // [def, centralRepublic] ceq  →  [IsRepublicFactionDef(def)]
                list[i + 1] = new CodeInstruction(OpCodes.Call, isRepublicFactionDef).WithLabels(next.labels);
            }
            else if (next.opcode == OpCodes.Bne_Un_S || next.opcode == OpCodes.Bne_Un)
            {
                // bne.un[.s] LABEL  →  call IsRepublicFactionDef; brfalse[.s] LABEL
                var brOpcode = next.opcode == OpCodes.Bne_Un_S ? OpCodes.Brfalse_S : OpCodes.Brfalse;
                list[i + 1] = new CodeInstruction(OpCodes.Call, isRepublicFactionDef).WithLabels(next.labels);
                list.Insert(i + 2, new CodeInstruction(brOpcode, next.operand));
            }
            else if (next.opcode == OpCodes.Beq_S || next.opcode == OpCodes.Beq)
            {
                // beq[.s] LABEL  →  call IsRepublicFactionDef; brtrue[.s] LABEL
                var brOpcode = next.opcode == OpCodes.Beq_S ? OpCodes.Brtrue_S : OpCodes.Brtrue;
                list[i + 1] = new CodeInstruction(OpCodes.Call, isRepublicFactionDef).WithLabels(next.labels);
                list.Insert(i + 2, new CodeInstruction(brOpcode, next.operand));
            }
        }

        return list;
    }
}
