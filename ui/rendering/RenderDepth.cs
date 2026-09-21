public static class RenderDepth
{
    private const int ElevationStride = 1_000_000;
    private const int WorldYStride = 10_000;
    private const int StableStride = 1_000;

    // Lower values draw first. Elevation dominates world position, which dominates
    // deterministic per-object ordering.
    public static int Key(int elevation, int worldY, int stableOrder) =>
        elevation * ElevationStride + worldY * WorldYStride + stableOrder;

    public static int Terrain(int elevation, int worldY, int drawLayer, int x) =>
        Key(elevation, worldY, drawLayer * StableStride + Math.Max(0, x));

    public static int WorldObject(int elevation, int worldY, int x, int stableOrder = 0) =>
        Key(elevation, worldY, x * StableStride + stableOrder);

    public static int Entity(int elevation, int worldY, int x, int stableOrder = 0) =>
        WorldObject(elevation, worldY, x, stableOrder);
}
