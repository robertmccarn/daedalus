using System.Drawing;

public sealed class WorldRenderer
{
    public void Draw(Graphics g, ExplorationRenderContext context)
    {
        GameWorld world = context.World;
        WorldPresentationProfile p = context.Profile;
        long now = AnimationClock.Now;
        GridPosition leader = context.Party.LeaderPosition;
        int radiusX = 18;
        int radiusY = 12;

        int minX = Math.Max(0, leader.X - radiusX);
        int maxX = Math.Min(GameWorld.Width - 1, leader.X + radiusX);
        int minY = Math.Max(0, leader.Y - radiusY);
        int maxY = Math.Min(GameWorld.Height - 1, leader.Y + radiusY);

        List<(int X, int Y)> cells = new();
        for (int y = minY; y <= maxY; y++)
            for (int x = minX; x <= maxX; x++)
                cells.Add((x, y));

        foreach ((int x, int y) in cells.OrderBy(cell =>
            RenderDepth.Terrain(
                world.Dungeon[cell.Y, cell.X].Elevation,
                cell.Y,
                world.Dungeon[cell.Y, cell.X].DrawLayer,
                cell.X)))
        {
            Point screen = context.ToScreen(new GridPosition(x, y));

            if (!context.IsDiscovered(x, y))
            {
                using Brush fog = new SolidBrush(Color.FromArgb(246, 7, 9, 12));
                g.FillRectangle(fog, screen.X, screen.Y, context.TileSize + 1, context.TileSize + 1);
                continue;
            }

            DrawTile(g, context, x, y, world.Dungeon[y, x], p, now);

            if (!context.IsVisible(x, y))
            {
                using Brush memoryShade = new SolidBrush(Color.FromArgb(118, 4, 7, 9));
                g.FillRectangle(memoryShade, screen.X, screen.Y, context.TileSize + 1, context.TileSize + 1);
            }
        }
    }

    private static void DrawTile(
        Graphics g,
        ExplorationRenderContext context,
        int x,
        int y,
        Tile tile,
        WorldPresentationProfile p,
        long now)
    {
        Point s = context.ToScreen(new GridPosition(x, y));
        int z = tile.Elevation * 4;
        int size = context.TileSize;
        Rectangle r = new(s.X, s.Y - z, size + 1, size + 1);

        switch (tile.Type)
        {
            case TileType.Wall:
            case TileType.Pillar:
                DrawWall(g, r, p, x, y, tile.Type == TileType.Pillar);
                return;

            case TileType.StairsDown:
                DrawFloor(g, r, p, x, y);
                DrawStairs(g, r, p.Accent, now);
                return;

            case TileType.StairsUp:
                DrawFloor(g, r, p, x, y);
                DrawStairs(g, r, p.WarmLight, now);
                return;

            case TileType.Door:
                DrawFloor(g, r, p, x, y);
                DrawDoor(g, r, p);
                return;

            case TileType.Water:
                DrawFloor(g, r, p, x, y);
                DrawWater(g, r, p, x, y, now);
                return;

            default:
                DrawFloor(g, r, p, x, y);
                return;
        }
    }

    private static void DrawFloor(Graphics g, Rectangle r, WorldPresentationProfile p, int x, int y)
    {
        int pattern = PositiveMod(x * 92821 + y * 68917, 31);
        float tone = pattern switch
        {
            3 or 17 => 0.16f,
            8 or 24 => 0.09f,
            12 => 0.22f,
            _ => 0.03f
        };

        Color baseColor = Blend(p.Floor, p.FloorAlternate, tone);
        using Brush fill = new SolidBrush(baseColor);
        g.FillRectangle(fill, r);

        // Each floor cell is treated as a worn stone slab rather than a square tile.
        int chip = PositiveMod(x * 17 + y * 43, 4);
        int inset = 2;
        Point[] slab =
        {
            new(r.X + inset + (chip == 0 ? 2 : 0), r.Y + inset),
            new(r.Right - inset - 1, r.Y + inset + (chip == 1 ? 1 : 0)),
            new(r.Right - inset - 2, r.Bottom - inset - (chip == 2 ? 2 : 0)),
            new(r.X + inset + (chip == 3 ? 1 : 0), r.Bottom - inset - 1)
        };

        using Brush slabBrush = new SolidBrush(Blend(baseColor, p.Void, 0.15f));
        g.FillPolygon(slabBrush, slab);

        using Pen perimeter = new(
            Color.FromArgb(62, p.WallHighlight.R, p.WallHighlight.G, p.WallHighlight.B),
            1);
        g.DrawPolygon(perimeter, slab);

        // Broken seams are intentionally short and offset so the eye does not
        // assemble the scene into a regular checkerboard.
        int seam = PositiveMod(x * 41 + y * 17, 7);
        using Pen joint = new(
            Color.FromArgb(68, p.WallHighlight.R, p.WallHighlight.G, p.WallHighlight.B),
            1);

        if (seam is 0 or 3)
        {
            int yy = r.Y + 8 + PositiveMod(y + x, 5);
            g.DrawLine(joint, r.X + 4, yy, r.X + 17, yy - 1);
        }

        if (seam is 1 or 4)
        {
            int xx = r.X + 8 + PositiveMod(x * 3 + y, 8);
            g.DrawLine(joint, xx, r.Y + 4, xx - 1, r.Y + 18);
        }

        if (seam is 2 or 6)
        {
            g.DrawLine(joint, r.X + 14, r.Y + 5, r.X + 20, r.Y + 9);
            g.DrawLine(joint, r.X + 20, r.Y + 9, r.X + 23, r.Y + 16);
        }

        if (pattern % 7 == 0)
        {
            using Pen fracture = new(Color.FromArgb(95, p.Void.R, p.Void.G, p.Void.B), 1);
            g.DrawLine(fracture, r.X + 5, r.Y + 15, r.X + 11, r.Y + 19);
            g.DrawLine(fracture, r.X + 11, r.Y + 19, r.X + 16, r.Y + 14);
            g.DrawLine(fracture, r.X + 16, r.Y + 14, r.X + 21, r.Y + 16);
        }

        if (pattern % 9 == 0)
        {
            using Brush chipBrush = new SolidBrush(Color.FromArgb(75, p.Void.R, p.Void.G, p.Void.B));
            g.FillPolygon(
                chipBrush,
                new Point[]
                {
                    new(r.X + 2, r.Y + 2),
                    new(r.X + 7, r.Y + 2),
                    new(r.X + 5, r.Y + 6)
                });
        }

        using Pen topEdge = new(
            Color.FromArgb(46, p.WallHighlight.R, p.WallHighlight.G, p.WallHighlight.B),
            1);
        g.DrawLine(topEdge, r.X + 2, r.Y + 2, r.Right - 3, r.Y + 2);
    }

    private static void DrawWall(
        Graphics g,
        Rectangle r,
        WorldPresentationProfile p,
        int x,
        int y,
        bool pillar)
    {
        int extrusion = pillar ? 13 : 9;

        using Brush shadow = new SolidBrush(Color.FromArgb(105, 0, 0, 0));
        g.FillPolygon(
            shadow,
            new Point[]
            {
                new(r.X + 2, r.Bottom - 2),
                new(r.Right - 2, r.Bottom - 2),
                new(r.Right - 1, r.Bottom - 2 + extrusion),
                new(r.X + 5, r.Bottom - 2 + extrusion)
            });

        Color bodyColor = Blend(p.Wall, p.Void, pillar ? 0.04f : 0.09f);
        using Brush body = new SolidBrush(bodyColor);
        g.FillRectangle(body, r);

        // Raised masonry top plane.
        Point[] topPlane =
        {
            new(r.X + 1, r.Y + 2),
            new(r.Right - 3, r.Y + 1),
            new(r.Right - 6, r.Y + 7),
            new(r.X + 4, r.Y + 8)
        };
        using Brush top = new SolidBrush(Blend(p.WallHighlight, p.Wall, 0.48f));
        g.FillPolygon(top, topPlane);

        // Dark lower face and side plane create physical thickness.
        Point[] lowerFace =
        {
            new(r.X + 2, r.Bottom - 9),
            new(r.Right - 3, r.Bottom - 9),
            new(r.Right - 3, r.Bottom - 1),
            new(r.X + 2, r.Bottom - 1)
        };
        using Brush face = new SolidBrush(Blend(p.Wall, p.Void, 0.34f));
        g.FillPolygon(face, lowerFace);

        Point[] sidePlane =
        {
            new(r.Right - 7, r.Y + 7),
            new(r.Right - 3, r.Y + 1),
            new(r.Right - 3, r.Bottom - 2),
            new(r.Right - 7, r.Bottom - 8)
        };
        using Brush side = new SolidBrush(Blend(p.Wall, p.Void, 0.47f));
        g.FillPolygon(side, sidePlane);

        using Pen edge = new(
            Color.FromArgb(190, p.WallHighlight.R, p.WallHighlight.G, p.WallHighlight.B),
            1);
        g.DrawLine(edge, r.X + 2, r.Y + 2, r.Right - 4, r.Y + 1);
        g.DrawLine(edge, r.X + 2, r.Bottom - 9, r.Right - 4, r.Bottom - 9);

        int seam = PositiveMod(x * 19 + y * 7, 6);
        using Pen masonry = new(Color.FromArgb(54, p.Void.R, p.Void.G, p.Void.B), 1);
        if (seam is 0 or 2)
            g.DrawLine(masonry, r.X + 4, r.Y + 13, r.X + 14, r.Y + 12);
        if (seam is 1 or 4)
            g.DrawLine(masonry, r.X + 14, r.Y + 7, r.X + 24, r.Y + 8);
        if (seam == 5)
            g.DrawLine(masonry, r.X + 8, r.Y + 6, r.X + 7, r.Bottom - 10);

        if (PositiveMod(x * 13 + y * 29, 6) is 1 or 5)
        {
            using Pen crack = new(Color.FromArgb(92, p.Void.R, p.Void.G, p.Void.B), 1);
            g.DrawLine(crack, r.X + 7, r.Y + 10, r.X + 11, r.Y + 17);
            g.DrawLine(crack, r.X + 11, r.Y + 17, r.X + 8, r.Y + 23);
        }

        if (PositiveMod(x * 31 + y * 11, 9) == 0)
        {
            using Brush chipBrush = new SolidBrush(Color.FromArgb(90, p.Void.R, p.Void.G, p.Void.B));
            g.FillPolygon(
                chipBrush,
                new Point[]
                {
                    new(r.Right - 11, r.Y + 3),
                    new(r.Right - 4, r.Y + 2),
                    new(r.Right - 6, r.Y + 10)
                });
        }
    }

    private static void DrawStairs(Graphics g, Rectangle r, Color accent, long now)
    {
        using Brush dark = new SolidBrush(Color.FromArgb(90, 0, 0, 0));
        g.FillRectangle(dark, r.X + 4, r.Y + 5, r.Width - 8, r.Height - 8);

        float pulse = 0.5f + 0.5f * AnimationClock.Sine(now, 1400, r.X + r.Y);
        using Brush glow = new SolidBrush(Color.FromArgb(
            42 + (int)(28 * pulse),
            accent.R,
            accent.G,
            accent.B));
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
        int y,
        long now)
    {
        using Brush water = new SolidBrush(Color.FromArgb(55, 85, 91));
        g.FillRectangle(water, r.X, r.Y, r.Width, r.Height);

        using Pen ripples = new(
            Color.FromArgb(55 + (int)(30 * AnimationClock.PingPong(now, 1100, x * 17 + y * 31)), p.Accent.R, p.Accent.G, p.Accent.B),
            1);

        int pattern = PositiveMod(x * 31 + y * 17, 3);
        int drift = (int)MathF.Round(AnimationClock.Sine(now, 1250, x * 43 + y * 7) * 2f);

        for (int i = 0; i < 2; i++)
        {
            int yy = r.Y + 9 + (i + pattern) * 7;
            g.DrawLine(ripples, r.X + 5 + drift, yy, r.Right - 5 + drift, yy - 1);
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
