using System.Drawing;
using System.Drawing.Drawing2D;

public class TopDownWorldRenderer
{
    private const int TileSize = 24;
    private const int ViewportCenterX = 550;
    private const int ViewportCenterY = 350;
    private const int VisibleRadiusX = 23;
    private const int VisibleRadiusY = 14;

    public void Draw(Graphics graphics, GameWorld world)
    {
        graphics.Clear(Color.Black);
        graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
        graphics.PixelOffsetMode = PixelOffsetMode.Half;

        int cameraX = world.Player.X * TileSize - ViewportCenterX;
        int cameraY = world.Player.Y * TileSize - ViewportCenterY;

        DrawTiles(graphics, world, cameraX, cameraY);
        DrawProps(graphics, world, cameraX, cameraY);
        DrawCharacters(graphics, world, cameraX, cameraY);
    }

    private static void DrawTiles(Graphics graphics, GameWorld world, int cameraX, int cameraY)
    {
        int minX = Math.Max(0, world.Player.X - VisibleRadiusX);
        int maxX = Math.Min(world.Dungeon.GetLength(1) - 1, world.Player.X + VisibleRadiusX);
        int minY = Math.Max(0, world.Player.Y - VisibleRadiusY);
        int maxY = Math.Min(world.Dungeon.GetLength(0) - 1, world.Player.Y + VisibleRadiusY);

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                int screenX = x * TileSize - cameraX;
                int screenY = y * TileSize - cameraY;
                TileRenderer.Draw(graphics, world.Dungeon[y, x], screenX, screenY, TileSize);
            }
        }
    }

    private static void DrawProps(Graphics graphics, GameWorld world, int cameraX, int cameraY)
    {
        foreach (InteractiveProp prop in world.Props)
        {
            int screenX = prop.X * TileSize - cameraX;
            int screenY = prop.Y * TileSize - cameraY;

            if (!IsVisible(screenX, screenY))
                continue;

            Rectangle body = new(screenX + 4, screenY + 4, TileSize - 8, TileSize - 8);

            using Brush brush = new SolidBrush(GetPropColor(prop));
            graphics.FillRectangle(brush, body);

            using Pen outline = new(Color.Black, 2);
            graphics.DrawRectangle(outline, body);
        }
    }

    private static void DrawCharacters(Graphics graphics, GameWorld world, int cameraX, int cameraY)
    {
        DrawCharacter(graphics, world.Player, cameraX, cameraY, Color.Cyan);

        foreach (Character enemy in world.Enemies)
            DrawCharacter(graphics, enemy, cameraX, cameraY, Color.IndianRed);

        foreach (Character enemy in world.DefeatedEnemies)
            DrawCharacter(graphics, enemy, cameraX, cameraY, Color.DarkRed);
    }

    private static void DrawCharacter(Graphics graphics, Character character, int cameraX, int cameraY, Color color)
    {
        int screenX = character.X * TileSize - cameraX;
        int screenY = character.Y * TileSize - cameraY;

        if (!IsVisible(screenX, screenY))
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
