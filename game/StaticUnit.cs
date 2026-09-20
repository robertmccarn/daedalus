using Systemic.Engine.State;

public class StaticUnit : GameObject
{
    public bool IsBlocking { get; }
    public DungeonNodeType NodeType { get; }
    public bool IsActivated { get; private set; }

    public StaticUnit(
        string name,
        int x,
        int y,
        bool isBlocking,
        DungeonNodeType nodeType)
        : base(name, x, y)
    {
        IsBlocking = isBlocking;
        NodeType = nodeType;
    }

    public void Activate() => IsActivated = true;
}
