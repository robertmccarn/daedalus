using System.Drawing;

public readonly record struct ViewportLayout(
    Rectangle WorldViewport,
    Rectangle HudViewport,
    int TileSize,
    int BottomSafeArea)
{
    public static ViewportLayout ForClientSize(int clientWidth, int clientHeight)
    {
        const int hudWidth = 240;
        const int bottomSafeArea = 80;
        int worldWidth = Math.Max(480, clientWidth - hudWidth);
        int worldHeight = Math.Max(480, clientHeight - bottomSafeArea);

        if (worldWidth + hudWidth > clientWidth)
            worldWidth = Math.Max(1, clientWidth - hudWidth);

        if (worldHeight + bottomSafeArea > clientHeight)
            worldHeight = Math.Max(1, clientHeight - bottomSafeArea);

        int hudX = Math.Max(0, clientWidth - hudWidth);
        return new ViewportLayout(
            new Rectangle(0, 0, worldWidth, worldHeight),
            new Rectangle(hudX, 0, Math.Max(1, clientWidth - hudX), clientHeight),
            28,
            Math.Max(0, clientHeight - worldHeight));
    }
}
