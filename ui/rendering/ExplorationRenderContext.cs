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
    public FeedbackEffect? Feedback { get; }
    public int CameraX { get; }
    public int CameraY { get; }
    public int TileSize => Layout.TileSize;
    public ViewportLayout Layout { get; }
    public BiomeType Biome => BiomeCatalog.ForFloor(World.Floor);
    public WorldPresentationProfile Profile => WorldPresentationProfile.ForBiome(Biome);

    public ExplorationRenderContext(
        GameWorld world,
        PartyController party,
        ExpeditionState expedition,
        Func<int, int, bool> isCellDiscovered,
        Func<int, int, bool> isCellVisible,
        string currentObjective,
        string message,
        FeedbackEffect? feedback,
        ViewportLayout layout)
    {
        World = world;
        Party = party;
        Expedition = expedition;
        IsDiscovered = isCellDiscovered;
        IsVisible = isCellVisible;
        CurrentObjective = currentObjective;
        Message = message;
        Feedback = feedback;
        Layout = layout;

        GridPosition leader = party.LeaderPosition;
        CameraX = leader.X * TileSize - Layout.WorldViewport.Width / 2;
        CameraY = leader.Y * TileSize - Layout.WorldViewport.Height / 2;
    }

    public ExplorationRenderContext(
        GameWorld world,
        PartyController party,
        ExpeditionState expedition,
        Func<int, int, bool> isCellDiscovered,
        Func<int, int, bool> isCellVisible,
        string currentObjective,
        string message,
        FeedbackEffect? feedback)
        : this(
            world,
            party,
            expedition,
            isCellDiscovered,
            isCellVisible,
            currentObjective,
            message,
            feedback,
            ViewportLayout.ForClientSize(1100, 700))
    {
    }

    public Point ToScreen(GridPosition position) =>
        new(position.X * TileSize - CameraX, position.Y * TileSize - CameraY);

    public bool Visible(GridPosition position)
    {
        Point p = ToScreen(position);
        Rectangle viewport = Layout.WorldViewport;
        return p.X >= viewport.Left - TileSize * 2 &&
               p.X <= viewport.Right + TileSize &&
               p.Y >= viewport.Top - TileSize * 2 &&
               p.Y <= viewport.Bottom + TileSize;
    }
}
