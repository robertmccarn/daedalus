public enum TileType
{
    Wall,
    Floor,
    Door,
    StairsUp,
    StairsDown,
    Treasure,
    Trap,
    Water,
    Pillar
}

public class Tile
{
    public TileType Type { get; }
    public bool IsWalkable { get; }
    public bool BlocksVision { get; }

    public Tile(
        TileType type,
        bool isWalkable,
        bool blocksVision)
    {
        Type = type;
        IsWalkable = isWalkable;
        BlocksVision = blocksVision;
    }
}
