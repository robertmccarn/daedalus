using Systemic.Engine.State;

// StaticUnit is non-interactive world metadata for a DungeonNode.
// InteractiveProp owns player interaction; StaticUnit does not duplicate it.
public class StaticUnit : GameObject
{
    public bool IsBlocking { get; }
    public DungeonNodeType NodeType { get; }
    public string NodeId { get; }
    public bool IsActivated { get; private set; }

    public StaticUnit(
        string name,
        int x,
        int y,
        bool isBlocking,
        DungeonNodeType nodeType,
        string nodeId = "")
        : base(name, x, y)
    {
        IsBlocking = isBlocking;
        NodeType = nodeType;
        NodeId = nodeId;
    }

    public void Activate() => IsActivated = true;
}
