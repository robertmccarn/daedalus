namespace Systemic.Engine.State;

public enum DungeonNodeType
{
    Start,
    Combat,
    Chest,
    Terminal,
    Extraction,
    Event
}

public class DungeonNode
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = string.Empty;
    public DungeonNodeType Type { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public bool IsCompleted { get; set; }
    public CoreReward? CoreReward { get; set; }
}

public class CoreReward
{
    public string Type { get; set; } = "Standard";
    public int Charge { get; set; }
    public int Quantity { get; set; } = 1;
}

public class RewardBundle
{
    public int Experience { get; set; }
    public int Gold { get; set; }
    public List<CoreReward> Cores { get; set; } = new();
    public List<InventoryItem> Items { get; set; } = new();
    public List<Gear> Gear { get; set; } = new();
    public List<Material> Materials { get; set; } = new();

    public bool IsEmpty =>
        Experience == 0 &&
        Gold == 0 &&
        Cores.Count == 0 &&
        Items.Count == 0 &&
        Gear.Count == 0 &&
        Materials.Count == 0;
}
