using Systemic.Engine.State;

public static class ExpeditionRewardSystem
{
    public static RewardBundle CreateCombatReward(
        Character enemy,
        int floor)
    {
        int level = Math.Max(1, enemy.Level);

        RewardBundle reward = new()
        {
            Experience = 10 * level + 5 * Math.Max(0, floor - 1),
            Gold = 10 * Math.Max(1, floor)
        };

        reward.Cores.Add(new CoreReward
        {
            Type = floor >= 3 ? "Refined" : "Standard",
            Charge = 10 + floor * 2,
            Quantity = 1
        });

        reward.Materials.Add(new Material
        {
            Id = "monster-residue",
            Name = "Monster Residue",
            Quantity = 1 + floor / 3
        });

        reward.Items.Add(new InventoryItem
        {
            Id = "field-ration",
            Name = "Field Ration",
            Quantity = 1
        });

        if (floor % 3 == 0)
        {
            reward.Gear.Add(new Gear
            {
                Name = "Salvaged Blade",
                Slot = "Weapon",
                Power = 2 + floor
            });
        }

        return reward;
    }

    public static void ApplyCombatReward(
        CampaignState campaign,
        ExpeditionState expedition,
        PartyMember partyMember,
        RewardBundle reward)
    {
        partyMember.Experience += reward.Experience;

        ExtractionSystem.ApplyReward(
            campaign,
            expedition,
            reward);
    }
}
