using System.Drawing;

public sealed class EffectRenderer
{
    public void Draw(Graphics g, ExplorationRenderContext context)
    {
        long ticks = Environment.TickCount64;
        WorldPresentationProfile p = context.Profile;

        DrawEnvironmentLights(g, context, p, ticks);
        DrawFeedback(g, context, p, ticks);
        DrawAmbientDust(g, context, p, ticks);
        DrawVignette(g, p);
    }

    private static void DrawEnvironmentLights(
        Graphics g,
        ExplorationRenderContext context,
        WorldPresentationProfile p,
        long ticks)
    {
        float pulse = (float)(0.5 + 0.5 * Math.Sin(ticks / 420.0));
        Point leader = context.ToScreen(context.Party.LeaderPosition);
        DrawRadialLight(g, leader, 185, 16 + (int)(10 * pulse), p.Accent);

        foreach (InteractiveProp prop in context.World.Props)
        {
            if (!context.IsVisible(prop.X, prop.Y))
                continue;

            if (prop is Terminal terminal)
            {
                Point point = context.ToScreen(new GridPosition(prop.X, prop.Y));
                DrawRadialLight(g, point, 105, terminal.IsActivated ? 46 : 24, p.Accent);
            }
        }

        foreach (WorldVisualFeature feature in context.World.VisualFeatures)
        {
            if (feature.Type != WorldVisualFeatureType.Landmark ||
                !context.IsVisible(feature.X, feature.Y))
                continue;

            Point point = context.ToScreen(new GridPosition(feature.X, feature.Y));
            DrawRadialLight(g, point, 155, 26 + (int)(8 * pulse), p.Accent);
        }

        foreach (Character enemy in context.World.Enemies)
        {
            if (!context.IsVisible(enemy.X, enemy.Y))
                continue;

            Point point = context.ToScreen(new GridPosition(enemy.X, enemy.Y));
            DrawRadialLight(g, point, 70, 16, p.Hazard);
        }
    }

    private static void DrawRadialLight(Graphics g, Point center, int diameter, int alpha, Color color)
    {
        for (int i = 4; i >= 1; i--)
        {
            float scale = i / 4f;
            int size = (int)(diameter * scale);
            int a = Math.Max(1, alpha * (5 - i) / 5);
            using Brush glow = new SolidBrush(Color.FromArgb(a, color.R, color.G, color.B));
            g.FillEllipse(glow, center.X - size / 2, center.Y - size / 2, size, size);
        }
    }

    private static void DrawFeedback(Graphics g, ExplorationRenderContext context, WorldPresentationProfile p, long ticks)
    {
        FeedbackEffect? feedback = context.Feedback;
        if (feedback == null || !feedback.IsActive(ticks))
            return;

        float elapsed = Math.Clamp((ticks - feedback.StartedAt) / (float)feedback.DurationMs, 0f, 1f);
        float pulse = 1f - elapsed;
        Point center = context.ToScreen(new GridPosition(feedback.X, feedback.Y));

        Color color = feedback.Type switch
        {
            FeedbackEffectType.Loot => p.WarmLight,
            FeedbackEffectType.Heal => p.WarmLight,
            FeedbackEffectType.Damage => p.Hazard,
            FeedbackEffectType.Victory => p.Accent,
            FeedbackEffectType.Extraction => p.WarmLight,
            FeedbackEffectType.Danger => p.Hazard,
            _ => p.Accent
        };

        int radius = 18 + (int)(54 * (1f - pulse));
        int alpha = Math.Max(8, (int)(135 * pulse));

        using Pen ring = new(Color.FromArgb(alpha, color.R, color.G, color.B), 3);
        g.DrawEllipse(ring, center.X - radius, center.Y - radius, radius * 2, radius * 2);

        if (feedback.Type is FeedbackEffectType.Loot or FeedbackEffectType.Heal)
        {
            using Pen rays = new(Color.FromArgb(alpha, color.R, color.G, color.B), 2);
            for (int i = 0; i < 8; i++)
            {
                double angle = i * Math.PI / 4.0;
                int inner = radius / 2;
                int outer = radius + 12;
                g.DrawLine(
                    rays,
                    center.X + (int)(Math.Cos(angle) * inner),
                    center.Y + (int)(Math.Sin(angle) * inner),
                    center.X + (int)(Math.Cos(angle) * outer),
                    center.Y + (int)(Math.Sin(angle) * outer));
            }
        }
    }

    private static void DrawAmbientDust(Graphics g, ExplorationRenderContext context, WorldPresentationProfile p, long ticks)
    {
        using Brush dust = new SolidBrush(Color.FromArgb(55, p.Mist.R, p.Mist.G, p.Mist.B));
        for (int i = 0; i < 24; i++)
        {
            int x = (i * 71 + (int)(ticks / 13)) % 820 + 20;
            int y = (i * 47 + (int)(ticks / 21)) % 560 + 20;
            g.FillEllipse(dust, x, y, 2, 2);
        }
    }

    private static void DrawVignette(Graphics g, WorldPresentationProfile p)
    {
        using Brush top = new SolidBrush(Color.FromArgb(55, p.Void.R, p.Void.G, p.Void.B));
        using Brush bottom = new SolidBrush(Color.FromArgb(80, p.Void.R, p.Void.G, p.Void.B));
        g.FillRectangle(top, 0, 0, 860, 34);
        g.FillRectangle(bottom, 0, 582, 860, 38);
    }
}
