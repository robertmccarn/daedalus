using Systemic.Engine.State;

public static class ExtractionSystem
{
    public static bool Extract(GameStateManager stateManager)
    {
        ExpeditionState expedition = stateManager.ActiveExpedition;

        if (expedition.ExtractionState == "Extracted")
            return false;

        stateManager.CommitExpeditionProgress();

        InventorySystem.TransferCarriedInventoryToStash(
            stateManager.Campaign,
            expedition);

        stateManager.Campaign.Gold = Math.Max(
            0,
            stateManager.Campaign.Gold - expedition.Upkeep);

        stateManager.CompleteExpedition();
        return true;
    }

    public static void ApplyReward(
        CampaignState campaign,
        ExpeditionState expedition,
        RewardBundle reward)
    {
        expedition.CarriedGold += reward.Gold;

        foreach (CoreReward coreReward in reward.Cores)
        {
            for (int i = 0; i < coreReward.Quantity; i++)
            {
                expedition.CarriedCores.Add(new EnergyCore
                {
                    Type = coreReward.Type,
                    Charge = coreReward.Charge
                });
            }
        }

        foreach (Gear gear in reward.Gear)
            expedition.CarriedGear.Add(gear);

        foreach (Material material in reward.Materials)
        {
            Material? existing = expedition.CarriedMaterials
                .FirstOrDefault(candidate => candidate.Id == material.Id);

            if (existing != null)
                existing.Quantity += material.Quantity;
            else
                expedition.CarriedMaterials.Add(new Material
                {
                    Id = material.Id,
                    Name = material.Name,
                    Quantity = material.Quantity
                });
        }

        foreach (InventoryItem item in reward.Items)
            InventorySystem.AddItem(expedition, item.Id, item.Name, item.Quantity);
    }
}
