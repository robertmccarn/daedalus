using System.Drawing;
using System.Drawing.Drawing2D;

public sealed class RuinFeatureRenderer
{
    public void Draw(Graphics g, ExplorationRenderContext context)
    {
        WorldPresentationProfile p = context.Profile;
        long now = AnimationClock.Now;
        SmoothingMode originalSmoothing = g.SmoothingMode;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        foreach (WorldVisualFeature feature in context.World.VisualFeatures
                     .OrderBy(feature => feature.Elevation)
                     .ThenBy(feature => feature.Y)
                     .ThenBy(feature => feature.X)
                     .ThenBy(feature => feature.Type))
        {
            GridPosition position = new(feature.X, feature.Y);
            if (!context.IsDiscovered(feature.X, feature.Y) ||
                !context.IsVisible(feature.X, feature.Y) ||
                !context.Visible(position))
                continue;

            switch (feature.Type)
            {
                case WorldVisualFeatureType.Abyss:
                    DrawAbyss(g, context, feature, p, now);
                    break;
                case WorldVisualFeatureType.Bridge:
                    DrawBridge(g, context, feature, p);
                    break;
                case WorldVisualFeatureType.Pillar:
                    DrawPillar(g, context, feature, p);
                    break;
                case WorldVisualFeatureType.Rubble:
                    DrawRubble(g, context, feature, p);
                    break;
                case WorldVisualFeatureType.BrokenWall:
                    DrawBrokenWall(g, context, feature, p);
                    break;
                case WorldVisualFeatureType.Doorway:
                    DrawDoorway(g, context, feature, p);
                    break;
                case WorldVisualFeatureType.Landmark:
                    DrawLandmark(g, context, feature, p, now);
                    break;
            }
        }

        g.SmoothingMode = originalSmoothing;
    }

    private static void DrawAbyss(
        Graphics g,
        ExplorationRenderContext context,
        WorldVisualFeature feature,
        WorldPresentationProfile p,
        long now)
    {
        Point origin = context.ToScreen(new GridPosition(feature.X, feature.Y));
        int width = feature.Width * context.TileSize;
        int height = feature.Height * context.TileSize;

        using Brush outer = new SolidBrush(Color.FromArgb(205, 5, 7, 10));
        g.FillRectangle(outer, origin.X - 4, origin.Y - 3, width + 8, height + 8);

        Point[] rim =
        {
            new(origin.X, origin.Y + 5),
            new(origin.X + width / 5, origin.Y + 1),
            new(origin.X + width * 2 / 5, origin.Y + 5),
            new(origin.X + width * 3 / 5, origin.Y - 2),
            new(origin.X + width * 4 / 5, origin.Y + 3),
            new(origin.X + width, origin.Y + 5),
            new(origin.X + width, origin.Y + height - 6),
            new(origin.X + width * 3 / 4, origin.Y + height - 2),
            new(origin.X + width / 2, origin.Y + height - 7),
            new(origin.X + width / 4, origin.Y + height - 3),
            new(origin.X, origin.Y + height - 6)
        };

        using Brush voidFill = new SolidBrush(Color.FromArgb(248, p.Void.R, p.Void.G, p.Void.B));
        g.FillPolygon(voidFill, rim);

        float pulse = 0.5f + 0.5f * AnimationClock.Sine(now, 2100, feature.Variant * 71);
        using Brush depthGlow = new SolidBrush(Color.FromArgb(
            24 + (int)(18 * pulse),
            p.Accent.R,
            p.Accent.G,
            p.Accent.B));
        g.FillEllipse(depthGlow, origin.X + width / 6, origin.Y + height / 4, width * 2 / 3, height / 2);

        // Distant vertical silhouettes drift at different speeds, creating a layered abyss.
        for (int i = 0; i < 5; i++)
        {
            int sx = origin.X + 10 + i * Math.Max(18, width / 5);
            int drift = (int)MathF.Round(AnimationClock.Sine(now, 3200 + i * 240, feature.Variant * 37 + i * 19) * 7f);
            int top = origin.Y + 16 + PositiveMod(feature.Variant * 11 + i * 17, Math.Max(18, height / 2)) + drift;
            int columnWidth = 7 + (i % 2) * 4;
            int columnHeight = Math.Max(18, height - (top - origin.Y) - 6);

            using Brush silhouette = new SolidBrush(Color.FromArgb(120 - i * 10, 11, 18, 21));
            g.FillRectangle(silhouette, sx, top, columnWidth, columnHeight);

            using Pen rimLight = new(Color.FromArgb(35 + i * 4, p.Accent.R, p.Accent.G, p.Accent.B), 1);
            g.DrawLine(rimLight, sx, top, sx + columnWidth, top);
        }

        using Pen depth = new(Color.FromArgb(72, p.Accent.R, p.Accent.G, p.Accent.B), 1);
        for (int i = 1; i <= 5; i++)
        {
            int y = origin.Y + i * 9 + PositiveMod(feature.Variant * 3, 5);
            g.DrawLine(depth, origin.X + 8, y, origin.X + width - 8, y - 4);
        }

        using Pen rimEdge = new(Color.FromArgb(150, p.WallHighlight.R, p.WallHighlight.G, p.WallHighlight.B), 2);
        g.DrawPolygon(rimEdge, rim);
    }

    private static void DrawBridge(
        Graphics g,
        ExplorationRenderContext context,
        WorldVisualFeature feature,
        WorldPresentationProfile p)
    {
        Point origin = context.ToScreen(new GridPosition(feature.X, feature.Y));
        int width = feature.Width * context.TileSize;
        int height = Math.Max(18, feature.Height * context.TileSize - 5);
        int lift = feature.Elevation * 4;

        using Brush underShadow = new SolidBrush(Color.FromArgb(135, 0, 0, 0));
        g.FillRectangle(underShadow, origin.X + 5, origin.Y + height + 3, width - 2, 13);

        Point[] deck =
        {
            new(origin.X + 2, origin.Y + 7 - lift),
            new(origin.X + width - 5, origin.Y + 3 - lift),
            new(origin.X + width - 5, origin.Y + height - 2 - lift),
            new(origin.X + 2, origin.Y + height + 2 - lift)
        };

        using Brush bridge = new SolidBrush(Color.FromArgb(122, 105, 82));
        g.FillPolygon(bridge, deck);

        using Brush wornTop = new SolidBrush(Color.FromArgb(148, 127, 98));
        g.FillPolygon(wornTop, new Point[]
        {
            new(origin.X + 3, origin.Y + 8 - lift),
            new(origin.X + width - 7, origin.Y + 4 - lift),
            new(origin.X + width - 7, origin.Y + 11 - lift),
            new(origin.X + 3, origin.Y + 15 - lift)
        });

        using Pen edge = new(Color.FromArgb(215, 80, 72, 60), 2);
        g.DrawPolygon(edge, deck);

        using Pen seams = new(Color.FromArgb(105, 48, 43, 36), 1);
        for (int x = origin.X + 12; x < origin.X + width - 7; x += 18)
            g.DrawLine(seams, x, origin.Y + 7 - lift, x - 1, origin.Y + height - 2 - lift);

        using Brush rail = new SolidBrush(Color.FromArgb(145, p.WallHighlight.R, p.WallHighlight.G, p.WallHighlight.B));
        g.FillRectangle(rail, origin.X + 6, origin.Y + 4 - lift, width - 10, 3);
        g.FillRectangle(rail, origin.X + 8, origin.Y + 5 - lift, 3, height - 4);
        g.FillRectangle(rail, origin.X + width - 12, origin.Y + 1 - lift, 3, height - 1);
    }

    private static void DrawPillar(
        Graphics g,
        ExplorationRenderContext context,
        WorldVisualFeature feature,
        WorldPresentationProfile p)
    {
        Point s = context.ToScreen(new GridPosition(feature.X, feature.Y));
        int lift = feature.Elevation * 4;
        int height = 30 + feature.Elevation * 5;

        using Brush shadow = new SolidBrush(Color.FromArgb(100, 0, 0, 0));
        g.FillEllipse(shadow, s.X + 2, s.Y + context.TileSize - 7, 27, 9);

        Point[] body =
        {
            new(s.X + 7, s.Y + 8 - height / 3 - lift),
            new(s.X + 22, s.Y + 4 - height / 3 - lift),
            new(s.X + 23, s.Y + 22 - lift),
            new(s.X + 6, s.Y + 25 - lift)
        };
        using Brush stone = new SolidBrush(Color.FromArgb(88, 84, 77));
        g.FillPolygon(stone, body);

        Point[] sidePlane =
        {
            new(s.X + 18, s.Y + 6 - height / 3 - lift),
            new(s.X + 23, s.Y + 4 - height / 3 - lift),
            new(s.X + 23, s.Y + 22 - lift),
            new(s.X + 18, s.Y + 23 - lift)
        };
        using Brush side = new SolidBrush(Color.FromArgb(68, 64, 59));
        g.FillPolygon(side, sidePlane);

        using Pen highlight = new(Color.FromArgb(145, p.WallHighlight.R, p.WallHighlight.G, p.WallHighlight.B), 1);
        g.DrawLine(
            highlight,
            s.X + 7,
            s.Y + 8 - height / 3 - lift,
            s.X + 22,
            s.Y + 4 - height / 3 - lift);
    }

    private static void DrawRubble(
        Graphics g,
        ExplorationRenderContext context,
        WorldVisualFeature feature,
        WorldPresentationProfile p)
    {
        Point s = context.ToScreen(new GridPosition(feature.X, feature.Y));
        int width = feature.Width * context.TileSize;
        int height = feature.Height * context.TileSize;

        using Brush shadow = new SolidBrush(Color.FromArgb(95, 0, 0, 0));
        g.FillEllipse(shadow, s.X + 3, s.Y + height - 9, width - 5, 10);

        Color[] tones =
        {
            p.WallHighlight,
            Color.FromArgb(90, 87, 80),
            Color.FromArgb(65, 63, 60)
        };

        int seed = feature.Variant * 19 + feature.X * 7 + feature.Y * 13;
        for (int i = 0; i < Math.Max(3, feature.Width * feature.Height + 1); i++)
        {
            int x = s.X + PositiveMod(seed + i * 17, Math.Max(12, width - 12));
            int y = s.Y + PositiveMod(seed / 3 + i * 11, Math.Max(10, height - 10));
            int size = 6 + (i % 3) * 3;

            Point[] rockShape =
            {
                new(x, y + size),
                new(x + size / 2, y),
                new(x + size, y + size / 3),
                new(x + size - 2, y + size)
            };

            using Brush rock = new SolidBrush(tones[i % tones.Length]);
            g.FillPolygon(rock, rockShape);
        }
    }

    private static void DrawBrokenWall(
        Graphics g,
        ExplorationRenderContext context,
        WorldVisualFeature feature,
        WorldPresentationProfile p)
    {
        Point s = context.ToScreen(new GridPosition(feature.X, feature.Y));
        int width = feature.Width * context.TileSize;
        int lift = feature.Elevation * 4;

        using Brush body = new SolidBrush(p.Wall);
        g.FillRectangle(body, s.X + 1, s.Y + 4 - lift, width - 2, 19);

        using Brush face = new SolidBrush(Color.FromArgb(
            Math.Max(0, p.Wall.R - 18),
            Math.Max(0, p.Wall.G - 18),
            Math.Max(0, p.Wall.B - 18)));
        g.FillRectangle(face, s.X + 1, s.Y + 18 - lift, width - 2, 6);

        using Pen edge = new(Color.FromArgb(160, p.WallHighlight.R, p.WallHighlight.G, p.WallHighlight.B), 1);
        g.DrawLine(edge, s.X + 2, s.Y + 4 - lift, s.X + width - 3, s.Y + 4 - lift);

        int gapX = feature.Variant == 0 ? width / 2 - 5 : width / 3;
        using Brush gap = new SolidBrush(p.Void);
        g.FillRectangle(gap, s.X + gapX, s.Y + 2 - lift, 9, 18);
    }

    private static void DrawDoorway(
        Graphics g,
        ExplorationRenderContext context,
        WorldVisualFeature feature,
        WorldPresentationProfile p)
    {
        Point s = context.ToScreen(new GridPosition(feature.X, feature.Y));
        int width = feature.Width * context.TileSize;
        int lift = feature.Elevation * 4;

        using Brush stone = new SolidBrush(p.Wall);
        g.FillRectangle(stone, s.X + 2, s.Y - lift, width - 4, 10);
        g.FillRectangle(stone, s.X + 2, s.Y - lift, 7, 34);
        g.FillRectangle(stone, s.X + width - 9, s.Y - lift, 7, 34);

        using Brush opening = new SolidBrush(p.Void);
        using GraphicsPath arch = new();
        arch.AddArc(s.X + width / 2 - 22, s.Y + 2 - lift, 44, 34, 180, 180);
        arch.AddLine(s.X + width / 2 + 22, s.Y + 19 - lift, s.X + width / 2 + 22, s.Y + 34 - lift);
        arch.AddLine(s.X + width / 2 + 22, s.Y + 34 - lift, s.X + width / 2 - 22, s.Y + 34 - lift);
        arch.AddLine(s.X + width / 2 - 22, s.Y + 34 - lift, s.X + width / 2 - 22, s.Y + 19 - lift);
        g.FillPath(opening, arch);

        using Pen glow = new(Color.FromArgb(150, p.Accent.R, p.Accent.G, p.Accent.B), 2);
        g.DrawArc(glow, s.X + width / 2 - 15, s.Y + 8 - lift, 30, 22, 180, 180);
    }

    private static void DrawLandmark(
        Graphics g,
        ExplorationRenderContext context,
        WorldVisualFeature feature,
        WorldPresentationProfile p,
        long now)
    {
        Point s = context.ToScreen(new GridPosition(feature.X, feature.Y));
        int lift = feature.Elevation * 4;
        int height = 40 + feature.Height * 6 + feature.Elevation * 2;
        float pulse = 0.5f + 0.5f * AnimationClock.Sine(now, 1400, feature.X * 41 + feature.Y * 17);
        int bob = (int)MathF.Round(AnimationClock.Sine(now, 1900, feature.X * 13 + feature.Y * 7) * 1.5f);

        using Brush glow = new SolidBrush(Color.FromArgb(
            24 + (int)(30 * pulse),
            p.Accent.R,
            p.Accent.G,
            p.Accent.B));
        g.FillEllipse(glow, s.X - 30, s.Y - height + 8 - lift, 76, 76);

        using Brush shadow = new SolidBrush(Color.FromArgb(100, 0, 0, 0));
        g.FillEllipse(shadow, s.X + 3, s.Y + 20, 22, 7);

        Point[] monolith =
        {
            new(s.X + 9, s.Y + 14 - height - lift + bob),
            new(s.X + 20, s.Y + 7 - height - lift + bob),
            new(s.X + 27, s.Y + 16 - height - lift + bob),
            new(s.X + 23, s.Y + 37 - lift + bob),
            new(s.X + 9, s.Y + 34 - lift + bob)
        };

        using Brush body = new SolidBrush(Color.FromArgb(84, 85, 82));
        g.FillPolygon(body, monolith);

        int accentAlpha = 180 + (int)(60 * pulse);
        using Pen accent = new(Color.FromArgb(accentAlpha, p.Accent.R, p.Accent.G, p.Accent.B), 2);
        g.DrawLine(accent, s.X + 18, s.Y + 12 - height - lift + bob, s.X + 18, s.Y + 31 - lift + bob);

        int coreSize = 7 + (int)(3 * pulse);
        using Brush core = new SolidBrush(Color.FromArgb(
            accentAlpha,
            p.Accent.R,
            p.Accent.G,
            p.Accent.B));
        g.FillEllipse(
            core,
            s.X + 18 - coreSize / 2,
            s.Y + 23 - height / 2 - lift + bob,
            coreSize,
            coreSize);
    }
    private static int PositiveMod(int value, int divisor) =>
        ((value % divisor) + divisor) % divisor;

}
