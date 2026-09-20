using Systemic.Engine.State;

public static class InventorySystem
{
    public static void AddItem(
        ExpeditionState expedition,
        string id,
        string name,
        int quantity = 1)
    {
        if (quantity <= 0)
            return;

        InventoryItem? existing = expedition.CarriedInventory
            .FirstOrDefault(item => item.Id == id);

        if (existing != null)
            existing.Quantity += quantity;
        else
            expedition.CarriedInventory.Add(new InventoryItem
            {
                Id = id,
                Name = name,
                Quantity = quantity
            });
    }

    public static bool RemoveItem(
        ExpeditionState expedition,
        string id,
        int quantity = 1)
    {
        InventoryItem? item = expedition.CarriedInventory
            .FirstOrDefault(candidate => candidate.Id == id);

        if (item == null || quantity <= 0 || item.Quantity < quantity)
            return false;

        item.Quantity -= quantity;

        if (item.Quantity == 0)
            expedition.CarriedInventory.Remove(item);

        return true;
    }

    public static bool Contains(
        ExpeditionState expedition,
        string id,
        int quantity = 1) =>
        expedition.CarriedInventory
            .Any(item => item.Id == id && item.Quantity >= quantity);

    public static void AddMaterial(
        CampaignState campaign,
        string id,
        string name,
        int quantity = 1)
    {
        if (quantity <= 0)
            return;

        Material? existing = campaign.Materials
            .FirstOrDefault(material => material.Id == id);

        if (existing != null)
            existing.Quantity += quantity;
        else
            campaign.Materials.Add(new Material
            {
                Id = id,
                Name = name,
                Quantity = quantity
            });
    }

    public static void AddGear(CampaignState campaign, Gear gear)
    {
        campaign.Gear.Add(gear);
    }

    public static void AddToStash(
        CampaignState campaign,
        InventoryItem item)
    {
        InventoryItem? existing = campaign.Stash
            .FirstOrDefault(candidate => candidate.Id == item.Id);

        if (existing != null)
            existing.Quantity += item.Quantity;
        else
            campaign.Stash.Add(new InventoryItem
            {
                Id = item.Id,
                Name = item.Name,
                Quantity = item.Quantity
            });
    }

    public static bool EquipGear(
        CampaignState campaign,
        string partyMemberId,
        string gearId)
    {
        PartyMember? member = campaign.PartyRoster
            .FirstOrDefault(candidate => candidate.Id == partyMemberId);

        Gear? gear = campaign.Gear
            .FirstOrDefault(candidate => candidate.Id == gearId);

        if (member == null || gear == null)
            return false;

        member.EquippedGearIds.RemoveAll(id =>
            campaign.Gear.FirstOrDefault(item => item.Id == id)?.Slot == gear.Slot);

        member.EquippedGearIds.Add(gear.Id);
        return true;
    }

    public static void TransferCarriedInventoryToStash(
        CampaignState campaign,
        ExpeditionState expedition)
    {
        foreach (InventoryItem item in expedition.CarriedInventory)
            AddToStash(campaign, item);

        expedition.CarriedInventory.Clear();
    }
}
