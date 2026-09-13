using System.Drawing;
using Systemic.Engine.Math;

public class IsometricWorldRenderer : IDisposable
{
    private readonly TileRenderer tileRenderer = new();

    private const int MapCenterX = 390;
    private const int MapCenterY = 315;
    private const int MapRightEdge = 760;
    private const int MapBottomEdge = 620;
    private const int VisibleRadius = 16;

    public void Draw(Graphics graphics, GameWorld world)
    {
        (float playerX, float playerY) = IsoProjection.GridToScreen(world.Player.X, world.Player.Y);
        float cameraOffsetX = MapCenterX - playerX;
        float cameraOffsetY = MapCenterY - playerY;
        List<RenderCommand> renderList = BuildRenderList(world, cameraOffsetX, cameraOffsetY);

        foreach (RenderCommand command in renderList.OrderBy(command => command.DepthKey))
        {
            command.Draw(graphics);
        }

        world.RemoveFinishedDeathAnimations();
    }

    private List<RenderCommand> BuildRenderList(GameWorld world, float cameraOffsetX, float cameraOffsetY)
    {
        List<RenderCommand> renderList = new();
        int minX = Math.Max(0, world.Player.X - VisibleRadius);
        int maxX = Math.Min(world.Dungeon.GetLength(1) - 1, world.Player.X + VisibleRadius);
        int minY = Math.Max(0, world.Player.Y - VisibleRadius);
        int maxY = Math.Min(world.Dungeon.GetLength(0) - 1, world.Player.Y + VisibleRadius);

        for (int y = minY; y <= maxY; y++)
        for (int x = minX; x <= maxX; x++)
        {
            (float screenX, float screenY) = IsoProjection.GridToScreen(x, y, cameraOffsetX, cameraOffsetY);
            if (!IsInViewport(screenX, screenY)) continue;

            Tile tile = world.Dungeon[y, x];
            int layer = tile.Type switch { TileType.Wall or TileType.StairsDown => 2, TileType.Pillar => 3, _ => 0 };
            int drawX = (int)MathF.Round(screenX);
            int drawY = (int)MathF.Round(screenY);
            renderList.Add(new RenderCommand(IsoProjection.GetDepthKey(x, y, layer), g => tileRenderer.DrawTile(g, tile, drawX, drawY)));
        }

        AddUnit(renderList, world.Player, AtlasDirection.SouthEast, cameraOffsetX, cameraOffsetY);
        foreach (Character enemy in world.Enemies) AddUnit(renderList, enemy, AtlasDirection.SouthWest, cameraOffsetX, cameraOffsetY);
        foreach (Character enemy in world.DefeatedEnemies) AddUnit(renderList, enemy, AtlasDirection.SouthWest, cameraOffsetX, cameraOffsetY);
        foreach (InteractiveProp prop in world.Props) AddProp(renderList, prop, cameraOffsetX, cameraOffsetY);
        return renderList;
    }

    private void AddUnit(List<RenderCommand> list, Character character, AtlasDirection direction, float offsetX, float offsetY)
    {
        (float x, float y) = IsoProjection.GridToScreen(character.X, character.Y, offsetX, offsetY);
        if (!IsInViewport(x, y)) return;
        int drawX = (int)MathF.Round(x); int drawY = (int)MathF.Round(y);
        list.Add(new RenderCommand(IsoProjection.GetDepthKey(character.X, character.Y, 1), g => tileRenderer.DrawUnit(g, character, direction, drawX, drawY)));
    }

    private void AddProp(List<RenderCommand> list, InteractiveProp prop, float offsetX, float offsetY)
    {
        (float x, float y) = IsoProjection.GridToScreen(prop.X, prop.Y, offsetX, offsetY);
        if (!IsInViewport(x, y)) return;
        int drawX = (int)MathF.Round(x); int drawY = (int)MathF.Round(y);
        list.Add(new RenderCommand(IsoProjection.GetDepthKey(prop.X, prop.Y, 2), g => tileRenderer.DrawProp(g, prop.Visual, drawX, drawY)));
    }

    private static bool IsInViewport(float x, float y) => x >= -64 && x <= MapRightEdge + 64 && y >= -64 && y <= MapBottomEdge + 64;
    public void Dispose() => tileRenderer.Dispose();
    private sealed record RenderCommand(float DepthKey, Action<Graphics> Draw);
}
