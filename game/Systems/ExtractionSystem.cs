using Systemic.Engine.State;

public static class ExtractionSystem
{
    public static bool Extract(GameStateManager stateManager)
    {
        ExpeditionState expedition = stateManager.ActiveExpedition;

        if (expedition.ExtractionState == "Extracted")
            return false;

        InventorySystem.TransferCarriedInventoryToStash(
            stateManager.Campaign,
            expedition);

        foreach (EnergyCore core in expedition.Party
                     .SelectMany(_ => Enumerable.Empty<EnergyCore>()))
        {
            stateManager.Campaign.Cores.Add(core);
        }

        stateManager.Campaign.Gold = Math.Max(
            0,
            stateManager.Campaign.Gold - expedition.Upkeep);

        stateManager.CompleteExpedition();
        return true;
    }

    public static void ExtractReward(
        CampaignState campaign,
        ExpeditionState expedition,
        RewardBundle reward)
    {
        campaign.Gold += reward.Gold;

        foreach (CoreReward coreReward in reward.Cores)
        {
            for (int i = 0; i < coreReward.Quantity; i++)
            {
                campaign.Cores.Add(new EnergyCore
                {
                    Type = coreReward.Type,
                    Charge = coreReward.Charge
                });
            }
        }

        foreach (Gear gear in reward.Gear)
            campaign.Gear.Add(gear);

        foreach (Material material in reward.Materials)
            InventorySystem.AddMaterial(
                campaign,
                material.Id,
                material.Name,
                material.Quantity);

        foreach (InventoryItem item in reward.Items)
            InventorySystem.AddItem(
                expedition,
                item.Id,
                item.Name,
                item.Quantity);
    }
}
