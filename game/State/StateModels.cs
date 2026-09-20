namespace Systemic.Engine.State;

public class PartyMember
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = string.Empty;
    public int Level { get; set; } = 1;
    public int Experience { get; set; }
    public int HP { get; set; }
    public int MaxHP { get; set; }
    public StatsData Stats { get; set; } = new();
    public List<string> EquippedGearIds { get; set; } = new();
}

public class StatsData
{
    public int Strength { get; set; }
    public int Magic { get; set; }
    public int Agility { get; set; }
    public int Luck { get; set; }
}

public class InventoryItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
}

public class Gear
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = string.Empty;
    public string Slot { get; set; } = string.Empty;
    public int Power { get; set; }
}

public class Recipe
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = string.Empty;
    public List<string> Ingredients { get; set; } = new();
    public Dictionary<string, int> IngredientQuantities { get; set; } = new();
    public string ResultItemId { get; set; } = string.Empty;
    public string ResultKind { get; set; } = "Item";
    public string ResultSlot { get; set; } = string.Empty;
    public int ResultPower { get; set; }
}

public class EnergyCore
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Type { get; set; } = "Standard";
    public int Charge { get; set; }
}

public class Material
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
}
