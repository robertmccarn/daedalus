namespace Systemic.Engine.State;

public class CampaignState
{
    public int EnergyCores { get; set; }
    public int RustedCatalysts { get; set; }
    public List<string> UnlockedBlueprintIds { get; set; } = new();
    public int LevelCapModifier { get; set; } = 1;
}
