using Systemic.Engine.State;

public static class UpkeepSystem
{
    public static int CalculateTurnCost(ExpeditionState expedition) =>
        Math.Max(1, expedition.Party.Count);

    public static int ApplyTurn(
        CampaignState campaign,
        ExpeditionState expedition)
    {
        int cost = CalculateTurnCost(expedition);

        expedition.Upkeep += cost;
        return cost;
    }
}
