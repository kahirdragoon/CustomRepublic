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

namespace Custom_Republic;

// Not working. Manual prefix Patch all the methods
public static class Patch_ModExtension
{
    private static bool isApplied = false;

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

        Log.Message("[Custom Republic] Using assembly: " + vfecAssembly.FullName);

        var types = vfecAssembly.GetTypes();
        foreach (var type in types)
        {
            var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                
            foreach (var method in methods)
            {
                if (method.IsAbstract || method.IsGenericMethodDefinition || method.DeclaringType.Assembly != vfecAssembly) continue;

                //Log.Message("[Custom Republic] Checking method " + method.FullDescription());
                var transpiler = new HarmonyMethod(typeof(Patch_ModExtension), nameof(Transpiler));
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
                var transpiler = new HarmonyMethod(typeof(Patch_ModExtension), nameof(Transpiler));
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

    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
    {
        var getModExtTarget = AccessTools.Method(typeof(Def), nameof(Def.GetModExtension))
            .MakeGenericMethod(typeof(FactionExtension_SenatorInfo));

        var hasModExtTarget = AccessTools.Method(typeof(Def), nameof(Def.HasModExtension))
            .MakeGenericMethod(typeof(FactionExtension_SenatorInfo));

        var getReplacement = AccessTools.Method(
            typeof(FactionExtension_SenatorInfoExtendedFactory),
            nameof(FactionExtension_SenatorInfoExtendedFactory.CreateForFactionFromDef));

        var hasReplacement = AccessTools.Method(
            typeof(Patch_ModExtension),
            nameof(HasModExtensionReplacement));

        foreach (var ci in instructions)
        {
            // Replace GetModExtension<FactionExtension_SenatorInfo>
            if ((ci.opcode == OpCodes.Call || ci.opcode == OpCodes.Callvirt) &&
                ci.operand is MethodInfo mi &&
                mi.IsGenericMethod &&
                mi.GetGenericMethodDefinition() == AccessTools.Method(typeof(Def), nameof(Def.GetModExtension)) &&
                mi.GetGenericArguments()[0] == typeof(FactionExtension_SenatorInfo))
            {
                Log.Message("[Custom Republic] Replacing GetModExtension<FactionExtension_SenatorInfo> in " + __originalMethod.FullDescription());
                ci.opcode = OpCodes.Call; // safe for static replacement
                ci.operand = getReplacement;
            }

            // Replace HasModExtension<FactionExtension_SenatorInfo>
            if ((ci.opcode == OpCodes.Call || ci.opcode == OpCodes.Callvirt) &&
                ci.operand is MethodInfo mi2 &&
                mi2.IsGenericMethod &&
                mi2.GetGenericMethodDefinition() == AccessTools.Method(typeof(Def), nameof(Def.HasModExtension)) &&
                mi2.GetGenericArguments()[0] == typeof(FactionExtension_SenatorInfo))
            {
                Log.Message("[Custom Republic] Replacing HasModExtension<FactionExtension_SenatorInfo> in " + __originalMethod.FullDescription());
                ci.opcode = OpCodes.Call;
                ci.operand = hasReplacement;
            }

            yield return ci;
        }
    }


    private static bool HasModExtensionReplacement(Def def)
    {
        if (def is not FactionDef)
            return false;

        var state = Current.Game?.GetComponent<GameComponent_Republic>()?.state;
        if (state == null)
            return def.HasModExtension<FactionExtension_SenatorInfoExtended>();
        return state.factionStates.Any(f => f.factionDefName == def.defName) || def.HasModExtension<FactionExtension_SenatorInfoExtended>();
    }
}
