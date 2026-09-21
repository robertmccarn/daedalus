using System.Drawing;

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
        int maxX = Math.Min(GameWorld.Width - 1, leader.X + radiusX);
        int minY = Math.Max(0, leader.Y - radiusY);
        int maxY = Math.Min(GameWorld.Height - 1, leader.Y + radiusY);

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                Point screen = context.ToScreen(new GridPosition(x, y));

                if (!context.IsDiscovered(x, y))
                {
                    using Brush fog = new SolidBrush(Color.FromArgb(246, 7, 9, 12));
                    g.FillRectangle(fog, screen.X, screen.Y, context.TileSize + 1, context.TileSize + 1);
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
        int size = context.TileSize;
        Rectangle r = new(s.X, s.Y - z, size + 1, size + 1);

        switch (tile.Type)
        {
            case TileType.Wall:
            case TileType.Pillar:
                DrawWall(g, r, p, tile.Type == TileType.Pillar);
                return;

            case TileType.StairsDown:
                DrawFloor(g, r, p, x, y);
                DrawStairs(g, r, p.Accent);
                return;

            case TileType.StairsUp:
                DrawFloor(g, r, p, x, y);
                DrawStairs(g, r, p.WarmLight);
                return;

            case TileType.Door:
                DrawFloor(g, r, p, x, y);
                DrawDoor(g, r, p);
                return;

            case TileType.Water:
                DrawFloor(g, r, p, x, y);
                DrawWater(g, r, p, x, y);
                return;

            default:
                DrawFloor(g, r, p, x, y);
                return;
        }
    }

    private static void DrawFloor(
        Graphics g,
        Rectangle r,
        WorldPresentationProfile p,
        int x,
        int y)
    {
        Color baseColor = ((x + y) & 1) == 0
            ? p.Floor
            : p.FloorAlternate;

        using Brush fill = new SolidBrush(baseColor);
        g.FillRectangle(fill, r);

        Color insetColor = Blend(baseColor, p.Void, 0.14f);
        using Brush inset = new SolidBrush(insetColor);
        g.FillRectangle(inset, r.X + 2, r.Y + 2, r.Width - 4, r.Height - 4);

        int pattern = PositiveMod(x * 92821 + y * 68917, 17);
        using Pen seam = new(Color.FromArgb(40, p.WallHighlight.R, p.WallHighlight.G, p.WallHighlight.B), 1);

        if (pattern is 2 or 7 or 13)
            g.DrawLine(seam, r.X + 5, r.Y + 8, r.Right - 5, r.Y + 6);

        if (pattern is 4 or 11)
            g.DrawLine(seam, r.X + 9, r.Y + 19, r.X + 17, r.Y + 22);

        using Pen topEdge = new(Color.FromArgb(85, p.WallHighlight.R, p.WallHighlight.G, p.WallHighlight.B), 1);
        g.DrawLine(topEdge, r.X + 1, r.Y + 1, r.Right - 1, r.Y + 1);
    }

    private static void DrawWall(
        Graphics g,
        Rectangle r,
        WorldPresentationProfile p,
        bool pillar)
    {
        int extrusion = pillar ? 10 : 7;

        using Brush shadow = new SolidBrush(Color.FromArgb(80, 0, 0, 0));
        g.FillRectangle(shadow, r.X + 3, r.Bottom - 2, r.Width - 1, extrusion);

        Color bodyColor = Blend(p.Wall, p.Void, 0.10f);
        using Brush body = new SolidBrush(bodyColor);
        g.FillRectangle(body, r);

        Point[] topPlane =
        {
            new(r.X + 1, r.Y + 2),
            new(r.Right - 2, r.Y + 2),
            new(r.Right - 5, r.Y + 7),
            new(r.X + 4, r.Y + 7)
        };

        using Brush top = new SolidBrush(Blend(p.WallHighlight, p.Wall, 0.55f));
        g.FillPolygon(top, topPlane);

        using Brush face = new SolidBrush(Blend(p.Wall, p.Void, 0.28f));
        g.FillRectangle(face, r.X + 2, r.Bottom - 7, r.Width - 3, 7);

        using Pen edge = new(Color.FromArgb(175, p.WallHighlight.R, p.WallHighlight.G, p.WallHighlight.B), 1);
        g.DrawLine(edge, r.X + 1, r.Y + 2, r.Right - 2, r.Y + 2);
        g.DrawLine(edge, r.X + 2, r.Bottom - 7, r.Right - 2, r.Bottom - 7);

        int notch = PositiveMod(r.X * 13 + r.Y * 29, 4);
        if (notch == 1)
        {
            using Pen crack = new(Color.FromArgb(85, p.Void.R, p.Void.G, p.Void.B), 1);
            g.DrawLine(crack, r.X + 7, r.Y + 10, r.X + 11, r.Y + 17);
            g.DrawLine(crack, r.X + 11, r.Y + 17, r.X + 8, r.Y + 23);
        }
    }

    private static void DrawStairs(Graphics g, Rectangle r, Color accent)
    {
        using Brush dark = new SolidBrush(Color.FromArgb(90, 0, 0, 0));
        g.FillRectangle(dark, r.X + 4, r.Y + 5, r.Width - 8, r.Height - 8);

        using Brush glow = new SolidBrush(Color.FromArgb(55, accent.R, accent.G, accent.B));
        g.FillRectangle(glow, r.X + 5, r.Y + 6, r.Width - 10, r.Height - 10);

        using Pen step = new(Color.FromArgb(190, accent.R, accent.G, accent.B), 1);
        for (int i = 0; i < 5; i++)
        {
            int y = r.Y + 8 + i * 4;
            g.DrawLine(step, r.X + 5, y, r.Right - 6, y);
        }
    }

    private static void DrawDoor(Graphics g, Rectangle r, WorldPresentationProfile p)
    {
        using Brush dark = new SolidBrush(Color.FromArgb(220, p.Void.R, p.Void.G, p.Void.B));
        g.FillRectangle(dark, r.X + 6, r.Y + 4, r.Width - 12, r.Height - 5);

        using Pen frame = new(Color.FromArgb(190, p.WallHighlight.R, p.WallHighlight.G, p.WallHighlight.B), 2);
        g.DrawLine(frame, r.X + 5, r.Bottom - 2, r.X + 5, r.Y + 8);
        g.DrawLine(frame, r.Right - 6, r.Bottom - 2, r.Right - 6, r.Y + 8);
        g.DrawArc(frame, r.X + 5, r.Y + 2, r.Width - 10, 19, 180, 180);

        using Pen inner = new(Color.FromArgb(130, p.Accent.R, p.Accent.G, p.Accent.B), 1);
        g.DrawLine(inner, r.X + 10, r.Bottom - 6, r.Right - 11, r.Bottom - 6);
    }

    private static void DrawWater(
        Graphics g,
        Rectangle r,
        WorldPresentationProfile p,
        int x,
        int y)
    {
        using Brush water = new SolidBrush(Color.FromArgb(55, 85, 91));
        g.FillRectangle(water, r.X, r.Y, r.Width, r.Height);

        using Pen ripples = new(Color.FromArgb(80, p.Accent.R, p.Accent.G, p.Accent.B), 1);
        int pattern = PositiveMod(x * 31 + y * 17, 3);
        for (int i = 0; i < 2; i++)
        {
            int yy = r.Y + 9 + (i + pattern) * 7;
            g.DrawLine(ripples, r.X + 5, yy, r.Right - 5, yy - 1);
        }
    }

    private static Color Blend(Color source, Color target, float amount)
    {
        amount = Math.Clamp(amount, 0f, 1f);
        return Color.FromArgb(
            source.A,
            source.R + (int)((target.R - source.R) * amount),
            source.G + (int)((target.G - source.G) * amount),
            source.B + (int)((target.B - source.B) * amount));
    }

    private static int PositiveMod(int value, int divisor) =>
        ((value % divisor) + divisor) % divisor;
}
