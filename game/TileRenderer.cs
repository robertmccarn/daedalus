using System.Drawing;

public static class TileRenderer
{
    public static void Draw(
        Graphics graphics,
        Tile tile,
        int screenX,
        int screenY,
        int tileSize)
    {
        using Brush brush = new SolidBrush(GetTileColor(tile.Type));
        graphics.FillRectangle(brush, screenX, screenY, tileSize, tileSize);

        if (tile.Type == TileType.Wall)
        {
            using Pen pen = new(Color.FromArgb(35, 35, 45), 1);
            graphics.DrawRectangle(pen, screenX, screenY, tileSize - 1, tileSize - 1);
        }
    }

    private static Color GetTileColor(TileType type) => type switch
    {
        TileType.Wall => Color.FromArgb(48, 45, 58),
        TileType.Floor => Color.FromArgb(82, 78, 91),
        TileType.Door => Color.FromArgb(125, 91, 54),
        TileType.StairsUp => Color.FromArgb(100, 110, 125),
        TileType.StairsDown => Color.FromArgb(55, 50, 70),
        TileType.Treasure => Color.FromArgb(105, 82, 38),
        TileType.Trap => Color.FromArgb(70, 45, 65),
        TileType.Water => Color.FromArgb(35, 70, 95),
        TileType.Pillar => Color.FromArgb(65, 61, 72),
        _ => Color.Black
    };
}
