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

        return new ViewportLayout(
            new Rectangle(0, 0, worldWidth, worldHeight),
            new Rectangle(clientWidth - hudWidth, 0, hudWidth, clientHeight),
            28,
            Math.Max(0, clientHeight - worldHeight));
    }
}
