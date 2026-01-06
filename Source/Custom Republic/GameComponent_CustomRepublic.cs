using HarmonyLib;
using Verse;
using VFEC.Senators;

namespace Custom_Republic;
public class GameComponent_Republic : GameComponent
{
    public RepublicState state = new();
    public RepublicRules rules = new();

    private static bool Patched;

    public GameComponent_Republic(Game game) : base()
    {
        if(Patched) return;
        CustomRepublicMod.Harmony.PatchAll();
        Patch_Reroute_VFEC_Calls.Apply();
        Patched = true;
    }

    public override void LoadedGame()
    {
        EnsureInitialized();
        if (state.factionStates.NullOrEmpty())
            RepublicStateBuilder.BuildFromRules();
    }

    public override void ExposeData()
    {
        Scribe_Deep.Look(ref rules, "republicRules");
        Scribe_Deep.Look(ref state, "republicState");
    }

    private void EnsureInitialized()
    {
        rules ??= new RepublicRules();
        state ??= new RepublicState();
    }
}
