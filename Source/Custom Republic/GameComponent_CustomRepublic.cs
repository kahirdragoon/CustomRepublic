using Verse;

namespace Custom_Republic;
public class GameComponent_Republic : GameComponent
{
    public RepublicState state = new();
    public RepublicRules rules = new();

    public GameComponent_Republic(Game game) : base()
    {
    }

    public override void StartedNewGame()
    {
        EnsureInitialized();
        ApplyRepublicState();
    }

    public override void LoadedGame()
    {
        EnsureInitialized();
        ApplyRepublicState();
    }

    public void ResetForNewGame()
    {
        state = new RepublicState();
        rules = new RepublicRules();
    }

    private void ApplyRepublicState()
    {
        RepublicApplier.Apply(rules, state);
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
