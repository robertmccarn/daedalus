using Systemic.Engine.State;

public static class ContentCatalog
{
    public static IReadOnlyList<string> EnemyFamilies { get; } =
        new[] { "Hollow", "Ashen", "Verdant", "Crystal" };

    public static IReadOnlyList<Gear> StarterGear { get; } =
        new[]
        {
            new Gear { Id = "iron-blade", Name = "Iron Blade", Slot = "Weapon", Power = 3 },
            new Gear { Id = "field-coat", Name = "Field Coat", Slot = "Armor", Power = 2 },
            new Gear { Id = "scout-ring", Name = "Scout Ring", Slot = "Ring", Power = 1 }
        };

    public static IReadOnlyList<Recipe> AdditionalRecipes { get; } =
        new[]
        {
            new Recipe
            {
                Id = "field-coat-reinforcement",
                Name = "Reinforced Field Coat",
                Ingredients = new() { "rusted-catalyst", "monster-residue" },
                IngredientQuantities = new() { ["rusted-catalyst"] = 1, ["monster-residue"] = 2 },
                ResultKind = "Gear",
                ResultSlot = "Armor",
                ResultPower = 4
            },
            new Recipe
            {
                Id = "resonant-scout-ring",
                Name = "Resonant Scout Ring",
                Ingredients = new() { "crystal-shard", "monster-residue" },
                IngredientQuantities = new() { ["crystal-shard"] = 1, ["monster-residue"] = 1 },
                ResultKind = "Gear",
                ResultSlot = "Ring",
                ResultPower = 3
            }
        };

    public static void InitializeCampaign(CampaignState campaign)
    {
        foreach (Gear gear in StarterGear)
            if (campaign.Gear.All(existing => existing.Id != gear.Id))
                campaign.Gear.Add(new Gear { Id = gear.Id, Name = gear.Name, Slot = gear.Slot, Power = gear.Power });

        foreach (Recipe recipe in AdditionalRecipes)
            if (campaign.Recipes.All(existing => existing.Id != recipe.Id))
                campaign.Recipes.Add(new Recipe
                {
                    Id = recipe.Id,
                    Name = recipe.Name,
                    Ingredients = new List<string>(recipe.Ingredients),
                    IngredientQuantities = new Dictionary<string, int>(recipe.IngredientQuantities),
                    ResultKind = recipe.ResultKind,
                    ResultSlot = recipe.ResultSlot,
                    ResultPower = recipe.ResultPower
                });
    }
}
