using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Verse;
using VFEC.Senators;

namespace Custom_Republic;

//[HarmonyPatch(typeof(WorldComponent_Senators), nameof(WorldComponent_Senators.GainFavorOf))]
//static class Patch_GainFavorOf
//{
//    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instr) => RepublicPartsTranspiler.Transpile(instr);
//}

//static class Patch_Dialog_PerkInfo_FactionData
//{
//    public static MethodBase TargetMethod()
//    {
//        var type = AccessTools.TypeByName("VFEC.Senators.Dialog_PerkInfo+RepublicDef");
//        if (type == null) throw new Exception("Cannot find RepublicDef type");

//        var ctor = AccessTools.Constructor(type, new[] { typeof(RepublicDef) });
//        if (ctor == null) throw new Exception("Cannot find matching constructor");

//        return ctor;
//    }

//    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instr) => Patch_RepublicParts.RepublicPartTranspile(instr);
//}

public static class Patch_RepublicParts
{
    private static readonly FieldInfo PartsField = AccessTools.Field(typeof(RepublicDef), nameof(RepublicDef.parts));
    private static readonly MethodInfo Replacement = AccessTools.Method(typeof(RepublicState), nameof(RepublicState.GetFactionDefs));

    private static readonly List<MethodBase> methodsToPatch = new List<MethodBase>
    {
        AccessTools.Method(typeof(SenatorUIUtility), nameof(SenatorUIUtility.DoPerkButton)),
        AccessTools.Method(typeof(WorldComponent_Senators), nameof(WorldComponent_Senators.GainFavorOf)),
        AccessTools.PropertyGetter(typeof(RepublicDef), nameof(RepublicDef.United)),
    };

    public static void Apply(Harmony harmony)
    {
        foreach (var method in methodsToPatch)
        {
            if (method.IsAbstract || method.IsGenericMethodDefinition || method.DeclaringType.Name.StartsWith("<")) continue;

            var transpiler = new HarmonyMethod(typeof(Patch_RepublicParts), nameof(RepublicPartTranspile));
            try
            {
                harmony.Patch(method, transpiler: transpiler);
            }
            catch
            {
                // ignore patch failures (abstract methods, compiler-generated, etc)
            }
        }
    }

    public static IEnumerable<CodeInstruction> RepublicPartTranspile(IEnumerable<CodeInstruction> instructions)
    {
        foreach (var ci in instructions)
        {
            if (ci.LoadsField(PartsField))
            {
                Log.Message("[Custom Republic]: Replacing RepublicDef.parts access with RepublicState.GetFactionDefs in " + ci);
                yield return new CodeInstruction(OpCodes.Call, Replacement);
            }
            else
                yield return ci;
        }
    }
}

