public sealed class TileDefinition
{
    public TileType Type { get; }
    public bool IsWalkable { get; }
    public bool BlocksVision { get; }

    public TileDefinition(
        TileType type,
        bool isWalkable,
        bool blocksVision)
    {
        Type = type;
        IsWalkable = isWalkable;
        BlocksVision = blocksVision;
    }
}

public static class TileDefinitions
{
    private static readonly Dictionary<TileType, TileDefinition> Definitions = new()
    {
        [TileType.Wall] =
            new TileDefinition(TileType.Wall, false, true),

        [TileType.Floor] =
            new TileDefinition(TileType.Floor, true, false),

        [TileType.Door] =
            new TileDefinition(TileType.Door, true, false),

        [TileType.StairsUp] =
            new TileDefinition(TileType.StairsUp, true, false),

        [TileType.StairsDown] =
            new TileDefinition(TileType.StairsDown, true, false),

        [TileType.Treasure] =
            new TileDefinition(TileType.Treasure, true, false),

        [TileType.Trap] =
            new TileDefinition(TileType.Trap, true, false),

        [TileType.Water] =
            new TileDefinition(TileType.Water, false, false),

        [TileType.Pillar] =
            new TileDefinition(TileType.Pillar, false, true)
    };

    public static TileDefinition Get(TileType type)
    {
        return Definitions[type];
    }
}
