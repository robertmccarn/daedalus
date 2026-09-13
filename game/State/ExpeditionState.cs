namespace Systemic.Engine.State;

public class ExpeditionState
{
    public int CurrentFloor { get; set; } = 1;
    public int TurnCount { get; set; }
    public int Health { get; set; } = 30;
    public int MaxHealth { get; set; } = 30;
    public List<string> Inventory { get; set; } = new();
    public (int X, int Y) PlayerGridPosition { get; set; }
}
