using HarmonyLib;
using RimWorld;
using Steamworks;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using Verse;
using VFEC.Senators;

namespace CustomRepublic;

internal static class Patch_Reroute_VFEC_Calls
{
    private static bool isApplied = false;

    private static readonly MethodInfo getModExt = AccessTools.Method(typeof(Def), nameof(Def.GetModExtension)).MakeGenericMethod(typeof(FactionExtension_SenatorInfo));
    private static readonly MethodInfo getModExtReplacement = AccessTools.Method(typeof(FactionExtension_SenatorInfoExtendedFactory), nameof(FactionExtension_SenatorInfoExtendedFactory.CreateForFactionFromDef));

    private static readonly MethodInfo hasModExt = AccessTools.Method(typeof(Def), nameof(Def.HasModExtension)).MakeGenericMethod(typeof(FactionExtension_SenatorInfo));
    private static readonly MethodInfo hasModExtReplacement = AccessTools.Method(typeof(Patch_Reroute_VFEC_Calls), nameof(HasModExtensionReplacement));

    //private static readonly MethodInfo republicDefsGetter = AccessTools.PropertyGetter(typeof(DefDatabase<RepublicDef>), nameof(DefDatabase<RepublicDef>.AllDefs));
    //private static readonly MethodInfo republicDefsGetterReplacement = AccessTools.Method(typeof(RepublicState), nameof(RepublicState.GetRepublicDefAsList));
    //private static readonly FieldInfo partsField = AccessTools.Field(typeof(RepublicDef), nameof(RepublicDef.parts));
    //private static readonly MethodInfo partsReplacement = AccessTools.Method(typeof(Patch_Reroute_VFEC_Calls), nameof(Patch_Reroute_VFEC_Calls.PartReplacement));

    public static void Apply()
    {
        if (isApplied) 
            return;

        var vfecAssembly = AppDomain.CurrentDomain
            .GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name.Contains("VFEC"));

        if (vfecAssembly == null)
        {
            Log.Error("[Custom Republic] VFEC assembly not found");
            return;
        }

        //Log.Message("[Custom Republic] Using assembly: " + vfecAssembly.FullName);

        var types = GetAllTypes(vfecAssembly);
        foreach (var type in types)
        {
            var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                
            foreach (var method in methods)
            {
                if (method.IsAbstract || method.IsGenericMethodDefinition || method.DeclaringType.Assembly != vfecAssembly) continue;

                //Log.Message("[Custom Republic] Checking method " + method.FullDescription());
                var transpiler = new HarmonyMethod(typeof(Patch_Reroute_VFEC_Calls), nameof(Transpiler));
                try
                {
                   CustomRepublicMod.Harmony.Patch(method, transpiler: transpiler);
                }
                catch
                {
                    // ignore patch failures (abstract methods, compiler-generated, etc)
                }
            }
            
            var ctors = type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var ctor in ctors)
            {
                if (ctor.IsAbstract || ctor.IsGenericMethodDefinition || ctor.DeclaringType.Assembly != vfecAssembly) continue;

                //Log.Message("[Custom Republic] Checking method " + method.FullDescription());
                var transpiler = new HarmonyMethod(typeof(Patch_Reroute_VFEC_Calls), nameof(Transpiler));
                try
                {
                    CustomRepublicMod.Harmony.Patch(ctor, transpiler: transpiler);
                }
                catch
                {
                    // ignore patch failures (abstract methods, compiler-generated, etc)
                }
            }
        }

        isApplied = true;
    }

    private static IEnumerable<Type> GetAllTypes(Assembly asm)
    {
        foreach (var t in asm.GetTypes())
        {
            yield return t;
            foreach (var nt in GetNestedTypesRecursive(t))
                yield return nt;
        }
    }

    private static IEnumerable<Type> GetNestedTypesRecursive(Type t)
    {
        foreach (var nt in t.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic))
        {
            yield return nt;
            foreach (var nnt in GetNestedTypesRecursive(nt))
                yield return nnt;
        }
    }

    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
    {
        foreach (var ci in instructions)
        {
            // Replace GetModExtension<FactionExtension_SenatorInfo>
            if ((ci.opcode == OpCodes.Call || ci.opcode == OpCodes.Callvirt) &&
                ci.operand is MethodInfo mi &&
                mi.IsGenericMethod &&
                mi.GetGenericMethodDefinition() == AccessTools.Method(typeof(Def), nameof(Def.GetModExtension)) &&
                mi.GetGenericArguments()[0] == typeof(FactionExtension_SenatorInfo))
            {
                //Log.Message("[Custom Republic] Replacing GetModExtension<FactionExtension_SenatorInfo> in " + __originalMethod.FullDescription());
                ci.opcode = OpCodes.Call; // safe for static replacement
                ci.operand = getModExtReplacement;
            }

            // Replace HasModExtension<FactionExtension_SenatorInfo>
            if ((ci.opcode == OpCodes.Call || ci.opcode == OpCodes.Callvirt) &&
                ci.operand is MethodInfo mi2 &&
                mi2.IsGenericMethod &&
                mi2.GetGenericMethodDefinition() == AccessTools.Method(typeof(Def), nameof(Def.HasModExtension)) &&
                mi2.GetGenericArguments()[0] == typeof(FactionExtension_SenatorInfo))
            {
                //Log.Message("[Custom Republic] Replacing HasModExtension<FactionExtension_SenatorInfo> in " + __originalMethod.FullDescription());
                ci.opcode = OpCodes.Call;
                ci.operand = hasModExtReplacement;
            }

            //if (ci.LoadsField(partsField))
            //{
            //    Log.Message("[Custom Republic]: Replacing RepublicDef.parts access with RepublicState.GetFactionDefs in " + __originalMethod.FullDescription());
            //    yield return new CodeInstruction(OpCodes.Call, partsReplacement);
            //}

            //// Replace DefDatabase<RepublicDef>.AllDefs
            //if (ci.opcode == OpCodes.Call && ci.operand is MethodInfo mi3 && mi3 == republicDefsGetter)
            //{
            //    Log.Message("[Custom Republic] Replacing DefDatabase<RepublicDef>.AllDefs call in " + __originalMethod.FullDescription());
            //    ci.opcode = OpCodes.Call; // static method
            //    ci.operand = republicDefsGetterReplacement;
            //}

            yield return ci;
        }
    }

    private static bool HasModExtensionReplacement(Def def)
    {
        if (def is null || def is not FactionDef)
            return false;

        var state = GameComponent_Republic.Instance?.state;
        if (state == null)
            return def.HasModExtension<FactionExtension_SenatorInfoExtended>();
        return state.factionStates.Any(f => f.factionDefName == def.defName) || def.HasModExtension<FactionExtension_SenatorInfoExtended>();
    }

    //private static List<FactionDef> PartReplacement(RepublicDef republicDef)
    //{
    //    var state = GameComponent_Republic.Instance?.state;
    //    if (state == null)
    //        return republicDef.parts;
    //    return state.GetFactionDefs(republicDef);
    //}
}
