using HarmonyLib;
using UnityEngine;
using Verse;

namespace CustomRepublic;

internal class CustomRepublicMod : Mod
{
    public static Harmony Harmony = null!;
    public static CustomRepublicSettings Settings = null!;

    public CustomRepublicMod(ModContentPack content) : base(content)
    {
        Settings = GetSettings<CustomRepublicSettings>();

        Harmony = new Harmony("kahirdragoon.customrepublic");

        LongEventHandler.ExecuteWhenFinished(() =>
        {
            Harmony.PatchAll();
            Patch_Reroute_VFEC_Calls.Apply();
        });
    }

    public override string SettingsCategory() => "Custom Republic";

    public override void DoSettingsWindowContents(Rect inRect)
    {
        var listing = new Listing_Standard();
        listing.Begin(inRect);

        listing.Label("CR.ButtonLocationSetting".Translate());
        listing.Gap(4f);

        if (listing.RadioButton("CR.ButtonLocationWorldParams".Translate(), Settings.buttonLocation == ButtonLocation.WorldParams))
            Settings.buttonLocation = ButtonLocation.WorldParams;

        if (listing.RadioButton("CR.ButtonLocationStartingSite".Translate(), Settings.buttonLocation == ButtonLocation.StartingSite))
            Settings.buttonLocation = ButtonLocation.StartingSite;

        listing.End();

        Settings.Write();
    }
}
