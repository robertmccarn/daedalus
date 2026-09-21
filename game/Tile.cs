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

public enum TileOcclusionBehavior
{
    None,
    Raised,
    BlocksActors,
    Foreground
}

public class Tile
{
    public TileType Type { get; }
    public bool IsWalkable { get; }
    public bool BlocksVision { get; }
    public int Elevation { get; }
    public int DrawLayer { get; }
    public TileOcclusionBehavior OcclusionBehavior { get; }

    public Tile(
        TileType type,
        bool isWalkable,
        bool blocksVision,
        int elevation = 0,
        int drawLayer = 0,
        TileOcclusionBehavior? occlusionBehavior = null)
    {
        Type = type;
        IsWalkable = isWalkable;
        BlocksVision = blocksVision;
        Elevation = elevation;
        DrawLayer = drawLayer;
        OcclusionBehavior = occlusionBehavior ?? GetDefaultOcclusion(type);
    }

    private static TileOcclusionBehavior GetDefaultOcclusion(TileType type) => type switch
    {
        TileType.Wall => TileOcclusionBehavior.Raised,
        TileType.Pillar => TileOcclusionBehavior.Raised,
        TileType.Door => TileOcclusionBehavior.Raised,
        _ => TileOcclusionBehavior.None
    };
}
