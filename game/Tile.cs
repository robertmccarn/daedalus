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
    public TileType Type { get; private set; }

    public char Symbol { get; private set; }

    public bool IsWalkable { get; private set; }

    public Tile(
        TileType type,
        char symbol,
        bool isWalkable)
    {
        Type = type;
        Symbol = symbol;
        IsWalkable = isWalkable;
    }
}
