//using Custom_Republic;
//using HarmonyLib;
//using RimWorld;
//using System.Reflection;
//using Verse;
//using VFEC.Senators;

//[HarmonyPatch]
//public static class Patch_FactionData_Ctor
//{
//    public static MethodBase TargetMethod()
//    {
//        var type = AccessTools.TypeByName("VFEC.Senators.Dialog_PerkInfo+FactionData");
//        if (type == null) throw new Exception("Cannot find FactionData type");

//        var ctor = type.GetConstructor(new[] { typeof(FactionDef) });
//        if (ctor == null) throw new Exception("Cannot find matching constructor");

//        return ctor;
//    }

//    static void Postfix(object __instance, FactionDef factionDef)
//    {
//        var game = Current.Game;
//        if (game == null) return;

//        var comp = game.GetComponent<GameComponent_Republic>();
//        if (comp?.state == null) return;

//        if (!comp.state.HasFaction(factionDef))
//            return;

//        var ext = RepublicExtensionFactory.CreateForFaction(factionDef, comp.state);

//        var extField = AccessTools.Field(__instance.GetType(), "ext");
//        extField.SetValue(__instance, ext);
//    }

//    private static void ApplyExtensionToInstance(object instance, FactionDef factionDef, FactionExtension_SenatorInfo ext)
//    {
//        //var factionField = AccessTools.Field(instance.GetType(), "faction");
//        //var perkBgField = AccessTools.Field(instance.GetType(), "perkBg");
//        //var perksField = AccessTools.Field(instance.GetType(), "perks");
//        //var finalPerkField = AccessTools.Field(instance.GetType(), "finalPerk");

//        //factionField.SetValue(
//        //    instance,
//        //    Find.FactionManager.FirstFactionOfDef(factionDef)
//        //);

//        //perkBgField.SetValue(instance, ext.PerkBG);

//        //perksField.SetValue(
//        //    instance,
//        //    ext.senatorPerks
//        //        .Select(p => Activator.CreateInstance(
//        //            AccessTools.TypeByName("VFEC.Perks.Dialog_PerkOverview+PerkData"),
//        //            p))
//        //        .ToList()
//        //);

//        //finalPerkField.SetValue(instance, ext.finalPerk);
//        var extField = AccessTools.Field(instance.GetType(), "ext");
//        extField.SetValue(instance, ext);
//    }
//}
