namespace Systemic.Engine.Math;

public static class IsoProjection
{
    public const int TileWidth = 32;
    public const int TileHeight = 16;

    public static (float ScreenX, float ScreenY) GridToScreen(
        int gridX,
        int gridY,
        float cameraOffsetX = 0,
        float cameraOffsetY = 0)
    {
        float screenX = (gridX - gridY) * (TileWidth / 2f) + cameraOffsetX;
        float screenY = (gridX + gridY) * (TileHeight / 2f) + cameraOffsetY;

        return (screenX, screenY);
    }

    public static float GetDepthKey(int gridX, int gridY, int layerPriority = 0)
    {
        return (gridX + gridY) * 10f + layerPriority;
    }
}
