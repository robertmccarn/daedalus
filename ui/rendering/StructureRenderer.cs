using System.Drawing;
using Systemic.Engine.State;

public sealed class StructureRenderer
{
    public void Draw(Graphics g, ExplorationRenderContext context)
    {
        WorldPresentationProfile p = context.Profile;
        long now = AnimationClock.Now;

        foreach (StaticUnit unit in context.World.StaticUnits
                     .OrderBy(unit => GetElevation(context, unit.X, unit.Y))
                     .ThenBy(unit => unit.Y)
                     .ThenBy(unit => unit.X))
        {
            GridPosition pos = new(unit.X, unit.Y);
            if (!context.IsDiscovered(unit.X, unit.Y) ||
                !context.IsVisible(unit.X, unit.Y) ||
                !context.Visible(pos))
                continue;

            DrawStaticUnit(g, context, unit, p, now);
        }

        foreach (InteractiveProp prop in context.World.Props
                     .OrderBy(prop => GetElevation(context, prop.X, prop.Y))
                     .ThenBy(prop => prop.Y)
                     .ThenBy(prop => prop.X))
        {
            GridPosition pos = new(prop.X, prop.Y);
            if (!context.IsDiscovered(prop.X, prop.Y) ||
                !context.IsVisible(prop.X, prop.Y) ||
                !context.Visible(pos))
                continue;

            DrawInteractiveProp(g, context, prop, p, now);
        }
    }

    private static int GetElevation(ExplorationRenderContext context, int x, int y) =>
        x >= 0 && y >= 0 && y < context.World.Dungeon.GetLength(0) &&
        x < context.World.Dungeon.GetLength(1)
            ? context.World.Dungeon[y, x].Elevation
            : 0;

    private static void DrawStaticUnit(
        Graphics g,
        ExplorationRenderContext context,
        StaticUnit unit,
        WorldPresentationProfile p,
        long now)
    {
        Point s = context.ToScreen(new GridPosition(unit.X, unit.Y));
        int lift = GetElevation(context, unit.X, unit.Y) * 4;

        using Brush shadow = new SolidBrush(Color.FromArgb(85, 0, 0, 0));
        g.FillEllipse(shadow, s.X + 2, s.Y + 19, 25, 8);

        Color baseColor = unit.NodeType switch
        {
            DungeonNodeType.Terminal => p.Accent,
            DungeonNodeType.Extraction => p.WarmLight,
            _ => p.WallHighlight
        };

        using Brush pedestal = new SolidBrush(Color.FromArgb(175, baseColor.R, baseColor.G, baseColor.B));
        g.FillRectangle(pedestal, s.X + 4, s.Y + 17 - lift, 20, 7);

        using Pen edge = new(Color.FromArgb(150, p.Void.R, p.Void.G, p.Void.B), 1);
        g.DrawRectangle(edge, s.X + 4, s.Y + 17 - lift, 20, 7);

        if (unit.IsActivated)
        {
            float pulse = 0.5f + 0.5f * AnimationClock.Sine(now, 900, unit.X * 41L + unit.Y * 17L);
            int radius = 11 + (int)(4 * pulse);
            using Pen active = new(
                Color.FromArgb(110 + (int)(70 * pulse), baseColor.R, baseColor.G, baseColor.B),
                2);
            g.DrawEllipse(
                active,
                s.X + 14 - radius,
                s.Y + 20 - radius - lift,
                radius * 2,
                radius * 2);
        }
    }

    private static void DrawInteractiveProp(
        Graphics g,
        ExplorationRenderContext context,
        InteractiveProp prop,
        WorldPresentationProfile p,
        long now)
    {
        Point s = context.ToScreen(new GridPosition(prop.X, prop.Y));
        int lift = GetElevation(context, prop.X, prop.Y) * 4;

        using Brush shadow = new SolidBrush(Color.FromArgb(100, 0, 0, 0));
        g.FillEllipse(shadow, s.X + 3, s.Y + 20, 23, 7);

        switch (prop)
        {
            case Chest chest:
                DrawChest(g, s, lift, p, chest.IsOpen, now, context.Feedback, prop.X, prop.Y);
                break;

            case Terminal terminal:
                DrawTerminal(g, s, lift, p, terminal.IsActivated, now, context.Feedback, prop.X, prop.Y);
                break;

            default:
            {
                using Brush body = new SolidBrush(p.WallHighlight);
                g.FillRectangle(body, s.X + 7, s.Y + 8 - lift, 14, 12);
                using Pen outline = new(p.Void, 2);
                g.DrawRectangle(outline, s.X + 7, s.Y + 8 - lift, 14, 12);
                break;
            }
        }
    }

    private static void DrawChest(
        Graphics g,
        Point s,
        int lift,
        WorldPresentationProfile p,
        bool isOpen,
        long now,
        FeedbackEffect? feedback,
        int worldX,
        int worldY)
    {
        using Brush wood = new SolidBrush(Color.FromArgb(125, 94, 63));
        g.FillRectangle(wood, s.X + 5, s.Y + 11 - lift, 18, 12);

        using Brush lid = new SolidBrush(Color.FromArgb(159, 121, 78));
        float opening = isOpen ? 1f : 0f;
        if (feedback is { Type: FeedbackEffectType.Loot } &&
            feedback.X == worldX &&
            feedback.Y == worldY)
        {
            float elapsed = AnimationClock.AttackProgress(
                now,
                feedback.StartedAt,
                Math.Min(650, feedback.DurationMs));
            opening = isOpen ? Math.Clamp(elapsed, 0f, 1f) : 0f;
        }

        int lidY = s.Y + AnimationClock.Lerp(7, 2, opening) - lift;
        g.FillRectangle(lid, s.X + 5, lidY, 18, 6);
        g.FillRectangle(wood, s.X + 6, s.Y + 8 - lift, 16, 5);

        using Pen metal = new(Color.FromArgb(210, p.WarmLight.R, p.WarmLight.G, p.WarmLight.B), 2);
        g.DrawLine(metal, s.X + 14, s.Y + 8 - lift, s.X + 14, s.Y + 20 - lift);

        if (!isOpen)
        {
            using Brush lockBrush = new SolidBrush(p.WarmLight);
            g.FillRectangle(lockBrush, s.X + 12, s.Y + 13 - lift, 5, 5);
        }
    }

    private static void DrawTerminal(
        Graphics g,
        Point s,
        int lift,
        WorldPresentationProfile p,
        bool isActivated,
        long now,
        FeedbackEffect? feedback,
        int worldX,
        int worldY)
    {
        Color accent = p.Accent;

        float pulse = 0.5f + 0.5f * AnimationClock.Sine(now, isActivated ? 900 : 1500, s.X * 17 + s.Y * 11);
        using Brush glow = new SolidBrush(Color.FromArgb(
            isActivated ? 42 + (int)(36 * pulse) : 28 + (int)(10 * pulse),
            accent.R,
            accent.G,
            accent.B));
        g.FillEllipse(glow, s.X - 4, s.Y + 2 - lift, 34, 34);

        using Brush housing = new SolidBrush(Color.FromArgb(74, 79, 78));
        g.FillRectangle(housing, s.X + 7, s.Y + 7 - lift, 14, 16);

        using Brush screen = new SolidBrush(Color.FromArgb(175, accent.R, accent.G, accent.B));
        g.FillRectangle(screen, s.X + 9, s.Y + 9 - lift, 10, 7);

        using Pen frame = new(Color.FromArgb(205, p.WallHighlight.R, p.WallHighlight.G, p.WallHighlight.B), 1);
        g.DrawRectangle(frame, s.X + 7, s.Y + 7 - lift, 14, 16);

        using Pen signal = new(Color.FromArgb(isActivated ? 230 : 110, accent.R, accent.G, accent.B), 2);
        g.DrawLine(signal, s.X + 14, s.Y + 3 - lift, s.X + 14, s.Y + 7 - lift);

        if (isActivated)
        {
            int radius = 12 + (int)(5 * pulse);
            using Pen ring = new(
                Color.FromArgb(110 + (int)(70 * pulse), accent.R, accent.G, accent.B),
                1);
            g.DrawEllipse(
                ring,
                s.X + 14 - radius,
                s.Y + 14 - radius - lift,
                radius * 2,
                radius * 2);

            if (feedback is { Type: FeedbackEffectType.Heal } &&
                feedback.X == worldX &&
                feedback.Y == worldY)
            {
                float activation = AnimationClock.AttackProgress(
                    now,
                    feedback.StartedAt,
                    Math.Min(900, feedback.DurationMs));
                int burstRadius = 10 + (int)(38 * activation);
                int burstAlpha = Math.Max(8, (int)(150 * (1f - activation)));

                using Pen burst = new(
                    Color.FromArgb(burstAlpha, accent.R, accent.G, accent.B),
                    2);
                g.DrawEllipse(
                    burst,
                    s.X + 14 - burstRadius,
                    s.Y + 14 - burstRadius - lift,
                    burstRadius * 2,
                    burstRadius * 2);
            }
        }
    }
}
