using Verse;

namespace CustomRepublic;

public enum ButtonLocation
{
    WorldParams,
    StartingSite
}

public class CustomRepublicSettings : ModSettings
{
    public ButtonLocation buttonLocation = ButtonLocation.StartingSite;

    public override void ExposeData()
    {
        Scribe_Values.Look(ref buttonLocation, "buttonLocation", ButtonLocation.WorldParams);
    }
}
