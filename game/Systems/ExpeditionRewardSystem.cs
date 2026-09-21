using Systemic.Engine.State;

public static class ExpeditionRewardSystem
{
    public static RewardBundle CreateCombatReward(Character enemy, int floor)
    {
        int level = Math.Max(1, enemy.Level);
        int tier = Math.Max(1, floor);

        RewardBundle reward = new()
        {
            Experience = 10 * level + 5 * Math.Max(0, floor - 1),
            Gold = 8 + 4 * tier
        };

        reward.Cores.Add(new CoreReward
        {
            Type = tier >= 3 ? "Refined" : "Standard",
            Charge = 10 + tier * 2,
            Quantity = 1
        });

        reward.Materials.Add(new Material
        {
            Id = tier >= 4 ? "crystal-shard" : "monster-residue",
            Name = tier >= 4 ? "Crystal Shard" : "Monster Residue",
            Quantity = 1 + tier / 3
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

    public static bool ApplyCombatReward(
        CampaignState campaign,
        ExpeditionState expedition,
        PartyMember partyMember,
        RewardBundle reward)
    {
        bool leveled = ProgressionSystem.ApplyExperience(partyMember, reward.Experience);
        ExtractionSystem.ApplyReward(campaign, expedition, reward);
        return leveled;
    }
}
