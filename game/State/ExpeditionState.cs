namespace Systemic.Engine.State;

public class ExpeditionState
{
    public int CurrentFloor { get; set; } = 1;
    public int TurnCount { get; set; }
    public int Health { get; set; } = 30;
    public int MaxHealth { get; set; } = 30;
    public List<InventoryItem> CarriedInventory { get; set; } = new();
    public List<EnergyCore> CarriedCores { get; set; } = new();
    public List<Gear> CarriedGear { get; set; } = new();
    public List<Material> CarriedMaterials { get; set; } = new();
    public int CarriedGold { get; set; }
    public List<string> DiscoveredCells { get; set; } = new();
    public int Upkeep { get; set; }
    public List<string> NodeHistory { get; set; } = new();
    public List<string> CompletedNodeIds { get; set; } = new();
    public List<string> DefeatedNodeIds { get; set; } = new();
    public string ExtractionState { get; set; } = "Active";
    public List<PartyMember> Party { get; set; } = new();
    public string LeaderId { get; set; } = string.Empty;
    public PartyFormationType Formation { get; set; } = PartyFormationType.Column;
    public string CurrentNode { get; set; } = "Start";
    public int FloorSeed { get; set; }
    // Compatibility field: this is the persisted leader position.
    public (int X, int Y) PlayerGridPosition { get; set; }

    // Compatibility surface for the prototype's old state API.
    public List<string> Inventory
    {
        get => CarriedInventory.Select(item => item.Id).ToList();
        set => CarriedInventory = value.Select(id => new InventoryItem { Id = id, Name = id }).ToList();
    }
}
