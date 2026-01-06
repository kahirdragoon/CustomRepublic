using Custom_Republic;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using VFEC.Senators;

//[HarmonyPatch(typeof(SenatorUIUtility), nameof(SenatorUIUtility.DoSenatorInfoButton))]
public static class Patch_DoSenatorInfoButton
{
    static bool Prefix(Faction faction, ref Rect fillRect, float rowY)
    {
        var comp = Current.Game?.GetComponent<GameComponent_Republic>();
        if (comp?.state == null)
            return true;

        if (!comp.state.HasFaction(faction.def))
            return true;

        fillRect.width -= 130f;

        if (Widgets.ButtonText(new Rect(fillRect.width + 5f, rowY + 25f, 120f, 30f), "VFEC.UI.ViewSenators".Translate()))
        {
            Log.Message($"[Custom Republic] Viewing senators for faction {faction.def.defName}");

            
                if (faction.ShouldHaveSenators())
                {
                Log.Message($"1");
                if (WorldComponent_Senators.Instance.SenatorInfo.ContainsKey(faction) != WorldComponent_Senators.Instance.Permanent.ContainsKey(faction))
                    {
                        Log.Warning("[VFE - Classical] SenatorInfo and Permanent dictionaries are out of sync! Clearing data.");
                        WorldComponent_Senators.Instance.SenatorInfo.Remove(faction);
                        WorldComponent_Senators.Instance.Permanent.Remove(faction);
                    }
                    Log.Message($"2");
                    //else if (WorldComponent_Senators.Instance.SenatorInfo.ContainsKey(faction)) ;
                    var extension = faction.def.GetModExtension<FactionExtension_SenatorInfo>();
                    Log.Message($"3 {extension == null}");
                    WorldComponent_Senators.Instance.SenatorInfo.Add(faction, Enumerable.Repeat((false, true), extension.numSenators).Select(
                        info =>
                            new SenatorInfo
                            {
                                Pawn = WorldComponent_Senators.Instance.GenerateSenator(faction),
                                Favored = info.Item1,
                                CanBribe = info.Item2,
                                Quest = null
                            }).ToList());
                    WorldComponent_Senators.Instance.Permanent.Add(faction, false);
                }
            Log.Message($"4");
            WorldComponent_Senators.Instance.CheckInit();
            Log.Message($"[Custom Republic] Retrieved WorldComponent_Senators");
            var ext = FactionExtension_SenatorInfoExtendedFactory.CreateForFaction(faction.def, comp.state);
            Log.Message($"[Custom Republic] Retrieved senator extension for faction {faction.def.defName}");
            Find.WindowStack.Add(new Dialog_SenatorInfo(ext, WorldComponent_Senators.Instance.SenatorInfo[faction], false)
            {
                Faction = faction
            });
        }

        return false;
    }
}
