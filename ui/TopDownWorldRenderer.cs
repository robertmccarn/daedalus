using System.Drawing;
using System.Drawing.Drawing2D;
using Systemic.Engine.State;

public class TopDownWorldRenderer
{
    private const int TileSize = 24;
    private const int ViewportCenterX = 550;
    private const int ViewportCenterY = 350;
    private const int VisibleRadiusX = 23;
    private const int VisibleRadiusY = 14;

    public void Draw(
        Graphics graphics,
        GameWorld world,
        PartyController party,
        ExpeditionState expedition,
        Func<int, int, bool> isCellDiscovered)
    {
        graphics.Clear(Color.Black);
        graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
        graphics.PixelOffsetMode = PixelOffsetMode.Half;

        GridPosition leader = party.LeaderPosition;
        int cameraX = leader.X * TileSize - ViewportCenterX;
        int cameraY = leader.Y * TileSize - ViewportCenterY;

        DrawTiles(graphics, world, leader, cameraX, cameraY, isCellDiscovered);
        DrawProps(graphics, world, cameraX, cameraY, isCellDiscovered);
        DrawCharacters(graphics, world, party, expedition, cameraX, cameraY, isCellDiscovered);
    }

    private static void DrawTiles(
        Graphics graphics,
        GameWorld world,
        GridPosition leader,
        int cameraX,
        int cameraY,
        Func<int, int, bool> isCellDiscovered)
    {
        int minX = Math.Max(0, leader.X - VisibleRadiusX);
        int maxX = Math.Min(world.Dungeon.GetLength(1) - 1, leader.X + VisibleRadiusX);
        int minY = Math.Max(0, leader.Y - VisibleRadiusY);
        int maxY = Math.Min(world.Dungeon.GetLength(0) - 1, leader.Y + VisibleRadiusY);

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                int screenX = x * TileSize - cameraX;
                int screenY = y * TileSize - cameraY;

                if (!isCellDiscovered(x, y))
                {
                    using Brush fog = new SolidBrush(Color.Black);
                    graphics.FillRectangle(fog, screenX, screenY, TileSize, TileSize);
                    continue;
                }

                TileRenderer.Draw(graphics, world.Dungeon[y, x], screenX, screenY, TileSize);
            }
        }
    }

    private static void DrawProps(
        Graphics graphics,
        GameWorld world,
        int cameraX,
        int cameraY,
        Func<int, int, bool> isCellDiscovered)
    {
        foreach (InteractiveProp prop in world.Props)
        {
            int screenX = prop.X * TileSize - cameraX;
            int screenY = prop.Y * TileSize - cameraY;

            if (!IsVisible(screenX, screenY) ||
                !isCellDiscovered(prop.X, prop.Y))
                continue;

            Rectangle body = new(screenX + 4, screenY + 4, TileSize - 8, TileSize - 8);

            using Brush brush = new SolidBrush(GetPropColor(prop));
            graphics.FillRectangle(brush, body);

            using Pen outline = new(Color.Black, 2);
            graphics.DrawRectangle(outline, body);
        }
    }

    private static void DrawCharacters(
        Graphics graphics,
        GameWorld world,
        PartyController party,
        ExpeditionState expedition,
        int cameraX,
        int cameraY,
        Func<int, int, bool> isCellDiscovered)
    {
        foreach (PartyRenderData member in party.GetRenderData(expedition)
                     .OrderBy(data => data.Position.Y)
                     .ThenBy(data => data.Position.X)
                     .ThenBy(data => data.PartySlot))
        {
            DrawPartyMember(
                graphics,
                member,
                cameraX,
                cameraY,
                isCellDiscovered);
        }

        foreach (Character enemy in world.Enemies)
            DrawCharacter(graphics, enemy, cameraX, cameraY, Color.IndianRed, isCellDiscovered);

        foreach (Character enemy in world.DefeatedEnemies)
            DrawCharacter(graphics, enemy, cameraX, cameraY, Color.DarkRed, isCellDiscovered);
    }

    private static void DrawPartyMember(
        Graphics graphics,
        PartyRenderData member,
        int cameraX,
        int cameraY,
        Func<int, int, bool> isCellDiscovered)
    {
        int screenX = member.Position.X * TileSize - cameraX;
        int screenY = member.Position.Y * TileSize - cameraY;

        if (!IsVisible(screenX, screenY) ||
            !isCellDiscovered(member.Position.X, member.Position.Y))
            return;

        Color color = member.IsLeader
            ? Color.Cyan
            : member.PartySlot switch
            {
                1 => Color.LightGreen,
                2 => Color.Gold,
                _ => Color.Violet
            };

        Rectangle body = new(screenX + 5, screenY + 3, TileSize - 10, TileSize - 6);
        using Brush brush = new SolidBrush(color);
        graphics.FillRectangle(brush, body);

        using Pen outline = new(Color.Black, 2);
        graphics.DrawRectangle(outline, body);
    }

    private static void DrawCharacter(
        Graphics graphics,
        Character character,
        int cameraX,
        int cameraY,
        Color color,
        Func<int, int, bool> isCellDiscovered)
    {
        int screenX = character.X * TileSize - cameraX;
        int screenY = character.Y * TileSize - cameraY;

        if (!IsVisible(screenX, screenY) ||
            !isCellDiscovered(character.X, character.Y))
            return;

        Rectangle body = new(screenX + 5, screenY + 3, TileSize - 10, TileSize - 6);

        using Brush brush = new SolidBrush(color);
        graphics.FillRectangle(brush, body);

        using Pen outline = new(Color.Black, 2);
        graphics.DrawRectangle(outline, body);
    }

    private static Color GetPropColor(InteractiveProp prop) => prop switch
    {
        Chest => Color.Goldenrod,
        Terminal => Color.Cyan,
        Rubble => Color.DimGray,
        _ => Color.White
    };

    private static bool IsVisible(int screenX, int screenY) =>
        screenX >= -TileSize &&
        screenX <= 1100 &&
        screenY >= -TileSize &&
        screenY <= 700;
}
