using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Verse;
using VFEC.Senators;

namespace Custom_Republic
{
    public static class Patch_GetModExtension
    {
        private static readonly MethodInfo getExt = AccessTools.Method(typeof(Def), nameof(Def.GetModExtension), new[] { typeof(FactionExtension_SenatorInfo) });
        private static readonly MethodInfo replacement = AccessTools.Method(typeof(RepublicExtensionFactory), nameof(RepublicExtensionFactory.GetForFaction));

        public static void Apply(Harmony harmony, Assembly targetAssembly)
        {
            var types = targetAssembly.GetTypes();
            foreach (var type in types)
            {
                var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                foreach (var method in methods)
                {
                    if (method.IsAbstract || method.IsGenericMethodDefinition || method.DeclaringType.Name.StartsWith("<")) continue;

                    var transpiler = new HarmonyMethod(typeof(Patch_GetModExtension), nameof(GetModExtensionTranspiler));
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
        }

        public static IEnumerable<CodeInstruction> GetModExtensionTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (var instr in instructions)
            {                
                if (instr.Calls(getExt)) 
                    yield return new CodeInstruction(OpCodes.Call, replacement); 
                else 
                    yield return instr; 
            }
        }
    }
}
