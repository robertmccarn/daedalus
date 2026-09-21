using System.Drawing;
using System.Drawing.Drawing2D;

public sealed class WorldRenderer
{
    public void Draw(Graphics g, ExplorationRenderContext context)
    {
        GameWorld world = context.World;
        WorldPresentationProfile p = context.Profile;
        GridPosition leader = context.Party.LeaderPosition;
        int radiusX = 18;
        int radiusY = 12;

        int minX = Math.Max(0, leader.X - radiusX);
        int maxX = Math.Min(world.Width - 1, leader.X + radiusX);
        int minY = Math.Max(0, leader.Y - radiusY);
        int maxY = Math.Min(world.Height - 1, leader.Y + radiusY);

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                if (!context.IsDiscovered(x, y))
                {
                    Point fog = context.ToScreen(new GridPosition(x, y));
                    using Brush b = new SolidBrush(Color.FromArgb(245, 8, 10, 13));
                    g.FillRectangle(b, fog.X, fog.Y, context.TileSize + 1, context.TileSize + 1);
                    continue;
                }

                DrawTile(g, context, x, y, world.Dungeon[y, x], p);
            }
        }
    }

    private static void DrawTile(
        Graphics g,
        ExplorationRenderContext context,
        int x,
        int y,
        Tile tile,
        WorldPresentationProfile p)
    {
        Point s = context.ToScreen(new GridPosition(x, y));
        int z = tile.Elevation * 4;
        Rectangle r = new(s.X, s.Y - z, context.TileSize + 1, context.TileSize + 1);

        Color baseColor = tile.Type switch
        {
            TileType.Wall => p.Wall,
            TileType.Pillar => p.Wall,
            TileType.Water => Color.FromArgb(40, 79, 88),
            TileType.StairsDown => p.Accent,
            TileType.StairsUp => p.WarmLight,
            TileType.Door => Color.FromArgb(94, 79, 63),
            _ => ((x + y) & 1) == 0 ? p.Floor : p.FloorAlternate
        };

        using Brush fill = new SolidBrush(baseColor);
        g.FillRectangle(fill, r);

        if (tile.Type == TileType.Wall || tile.Type == TileType.Pillar)
        {
            using Brush face = new SolidBrush(Color.FromArgb(
                Math.Max(0, baseColor.R - 22),
                Math.Max(0, baseColor.G - 22),
                Math.Max(0, baseColor.B - 22)));
            g.FillRectangle(face, r.X, r.Bottom - 6, r.Width, 6);
            using Pen highlight = new(Color.FromArgb(150, p.WallHighlight.R, p.WallHighlight.G, p.WallHighlight.B), 1);
            g.DrawLine(highlight, r.Left, r.Top, r.Right, r.Top);
        }

        if (tile.Type == TileType.StairsDown)
        {
            using Pen pen = new(Color.FromArgb(210, p.Accent.R, p.Accent.G, p.Accent.B), 2);
            for (int i = 0; i < 4; i++)
                g.DrawLine(pen, r.Left + 4, r.Top + 7 + i * 4, r.Right - 4, r.Top + 7 + i * 4);
        }
    }
}
