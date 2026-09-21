using System.Drawing;
using Systemic.Engine.State;

public sealed class ExplorationRenderContext
{
    public GameWorld World { get; }
    public ExpeditionState Expedition { get; }
    public PartyController Party { get; }
    public Func<int, int, bool> IsDiscovered { get; }
    public Func<int, int, bool> IsVisible { get; }
    public string CurrentObjective { get; }
    public string Message { get; }
    public int CameraX { get; }
    public int CameraY { get; }
    public int TileSize { get; } = 28;
    public BiomeType Biome => BiomeCatalog.ForFloor(World.Floor);
    public WorldPresentationProfile Profile => WorldPresentationProfile.ForBiome(Biome);

    public ExplorationRenderContext(
        GameWorld world,
        PartyController party,
        ExpeditionState expedition,
        Func<int, int, bool> isCellDiscovered,
        Func<int, int, bool> isCellVisible,
        string currentObjective,
        string message)
    {
        World = world;
        Party = party;
        Expedition = expedition;
        IsDiscovered = isCellDiscovered;
        IsVisible = isCellVisible;
        CurrentObjective = currentObjective;
        Message = message;

        GridPosition leader = party.LeaderPosition;
        CameraX = leader.X * TileSize - 420;
        CameraY = leader.Y * TileSize - 300;
    }

    public Point ToScreen(GridPosition position) =>
        new(position.X * TileSize - CameraX, position.Y * TileSize - CameraY);

    public bool Visible(GridPosition position)
    {
        Point p = ToScreen(position);
        return p.X >= -TileSize * 2 && p.X <= 860 &&
               p.Y >= -TileSize * 2 && p.Y <= 620;
    }
}
