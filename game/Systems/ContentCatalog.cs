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
                Id = "scout-ring",
                Name = "Resonant Scout Ring",
                Ingredients = new() { "crystal-shard", "monster-residue" },
                IngredientQuantities = new() { ["crystal-shard"] = 1, ["monster-residue"] = 1 },
                ResultKind = "Gear",
                ResultSlot = "Ring",
                ResultPower = 3
            }
        };
}
