using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Verse;
using VFEC.Senators;

namespace Custom_Republic
{
    //[HarmonyPatch]
    static class Patch_Dialog_PerkInfo_FactionData
    {
        public static MethodBase TargetMethod()
        {
            var type = AccessTools.TypeByName("VFEC.Senators.Dialog_PerkInfo+RepublicData");
            if (type == null) throw new Exception("Cannot find RepublicData type");

            var ctor = AccessTools.Constructor(type, new[] { typeof(RepublicDef) });
            if (ctor == null) throw new Exception("Cannot find matching constructor");

            return ctor;
        }

        static void Postfix(object __instance, RepublicDef republicDef)
        {
            var state = Current.Game?.GetComponent<GameComponent_Republic>()?.state;
            if (state == null) 
                return;

            var newFactions = BuildFactionDataList(state);

            var factionsField = AccessTools.Field(__instance.GetType(), "factions");
            factionsField.SetValue(__instance, newFactions);
        }

        private static object BuildFactionDataList(RepublicState state)
        {
            var factionDataType = AccessTools.TypeByName("VFEC.Senators.Dialog_PerkInfo+FactionData");

            var listType = typeof(List<>).MakeGenericType(factionDataType);
            var list = Activator.CreateInstance(listType);

            var addMethod = listType.GetMethod("Add");

            var ctor = AccessTools.Constructor(
                factionDataType,
                new[] { typeof(FactionDef), typeof(FactionExtension_SenatorInfo) }
            );

            foreach (var fs in state.factionStates)
            {
                var factionDef =
                    DefDatabase<FactionDef>.GetNamedSilentFail(fs.factionDefName);
                if (factionDef == null) continue;

                var ext = FactionExtension_SenatorInfoExtendedFactory.CreateForFaction(factionDef, state);
                if (ext == null) continue;

                var factionData = ctor.Invoke(new object[] { factionDef, ext });
                addMethod.Invoke(list, new[] { factionData });
            }

            return list;
        }
    }

    //[HarmonyPatch(typeof(Dialog_PerkInfo), nameof(Dialog_PerkInfo.DoWindowContents))]
    //public static class Patch_Dialog_PerkInfo
    //{
    //    private static readonly FieldInfo InfoField = AccessTools.Field(typeof(Dialog_PerkInfo), "info");

    //    private static Vector2 scrollPos = Vector2.zero;
    //    private static float scrollViewHeight;

    //    [HarmonyPrefix]
    //    public static bool Prefix(Dialog_PerkInfo __instance, Rect inRect)
    //    {
    //        var state = Current.Game?.GetComponent<GameComponent_Republic>()?.state;
    //        if (state == null || state.factionStates.NullOrEmpty())
    //            return true;

    //        var font = Text.Font;
    //        var anchor = Text.Anchor;

    //        float viewHeight = Mathf.Min(700f, inRect.height);

    //        var outRect = new Rect(
    //            inRect.x,
    //            inRect.y,
    //            inRect.width,
    //            viewHeight
    //        );

    //        var viewRect = new Rect(
    //            0f,
    //            0f,
    //            inRect.width - 16f, // scrollbar width
    //            scrollViewHeight
    //        );

    //        Widgets.BeginScrollView(outRect, ref scrollPos, viewRect);

    //        float y = 0f;

    //        float initialHeight = y;
    //        float x = 0f;

    //        foreach (var (faction, perkBg, perks, finalPerk, finalActive) in state.factionStates)
    //        {
    //            x = 0f;

    //            var rect = new Rect(x, y, 500f, 50f);
    //            Text.Anchor = TextAnchor.MiddleLeft;
    //            Text.Font = GameFont.Small;
    //            Widgets.Label(rect, faction.Name);
    //            y += 60f;

    //            foreach (var (perk, active) in perks)
    //                DoPerkInfo(ref x, y, perk, active, perkBg);

    //            Widgets.DrawLine(
    //                new Vector2(x, y),
    //                new Vector2(x, y + 100f),
    //                finalActive ? faction.Color : Color.gray,
    //                3f
    //            );

    //            x += 15f;
    //            DoPerkInfo(ref x, y, finalPerk, finalActive, perkBg);
    //            y += 110f;
    //        }

    //        float middle = initialHeight + (y - initialHeight) / 2f;
    //        var color = united ? Faction.OfPlayer.Color : Color.gray;

    //        Widgets.DrawLine(new Vector2(x, initialHeight), new Vector2(x + 20f, middle), color, 3f);
    //        Widgets.DrawLine(new Vector2(x, y), new Vector2(x + 20f, middle), color, 3f);

    //        x += 30f;
    //        DoPerkInfo(ref x, middle - 50f, republic.perk, united, SenatorUIUtility.PerkBG_United);

    //        // Store total content height for next frame
    //        scrollViewHeight = y + 10f;

    //        Widgets.EndScrollView();

    //        Text.Font = font;
    //        Text.Anchor = anchor;

    //        return false;
    //    }
    //}
}
