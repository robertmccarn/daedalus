namespace Systemic.Engine.State;

public class CampaignState
{
    public List<PartyMember> PartyRoster { get; set; } = new();
    public List<InventoryItem> Stash { get; set; } = new();
    public List<EnergyCore> Cores { get; set; } = new();
    public List<Gear> Gear { get; set; } = new();
    public List<Material> Materials { get; set; } = new();
    public List<Recipe> Recipes { get; set; } = new();
    public string Alignment { get; set; } = "Neutral";
    public HashSet<string> Flags { get; set; } = new();
    public string OverworldState { get; set; } = "StartingArea";
    public int Gold { get; set; }
    public int RunsCompleted { get; set; }
    public int HighestDepth { get; set; } = 1;
    public List<string> UnlockedBiomes { get; set; } = new() { "Ruin" };
    public List<string> Achievements { get; set; } = new();

    // Legacy resources retained as explicit campaign inventory.
    public int EnergyCores { get; set; }
    public int RustedCatalysts { get; set; }
    public List<string> UnlockedBlueprintIds { get; set; } = new();
    public int LevelCapModifier { get; set; } = 1;
}
