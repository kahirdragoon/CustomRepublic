using HarmonyLib;
using Verse;
using VFEC.Senators;

namespace CustomRepublic;
public class GameComponent_Republic : GameComponent
{
    public RepublicState state = new();
    public RepublicRules rules = new();

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
