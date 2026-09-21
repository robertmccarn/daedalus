using System.Drawing;

public sealed class EffectRenderer
{
    public void Draw(Graphics g, ExplorationRenderContext context)
    {
        long now = AnimationClock.Now;
        WorldPresentationProfile p = context.Profile;

        DrawEnvironmentLights(g, context, p, now);
        DrawAbyssMotes(g, context, p, now);
        DrawResonanceWisps(g, context, p, now);
        DrawAmbientDust(g, context, p, now);
        DrawMist(g, context, p, now);
        DrawFeedback(g, context, p, now);
        DrawVignette(g, context);
    }

    private static void DrawEnvironmentLights(
        Graphics g,
        ExplorationRenderContext context,
        WorldPresentationProfile p,
        long now)
    {
        float pulse = 0.5f + 0.5f * AnimationClock.Sine(now, 420);
        Point leader = context.ToScreen(context.Party.LeaderPosition);
        DrawRadialLight(g, leader, 185, 16 + (int)(10 * pulse), p.Accent);

        foreach (InteractiveProp prop in context.World.Props)
        {
            if (!context.IsVisible(prop.X, prop.Y))
                continue;

            Point point = context.ToScreen(new GridPosition(prop.X, prop.Y));

            if (prop is Terminal terminal)
            {
                int intensity = terminal.IsActivated
                    ? 42 + (int)(24 * AnimationClock.PingPong(now, 900, point.X + point.Y))
                    : 18;

                DrawRadialLight(g, point, 105, intensity, p.Accent);
            }
        }

        foreach (WorldVisualFeature feature in context.World.VisualFeatures)
        {
            if (!context.IsVisible(feature.X, feature.Y))
                continue;

            Point point = context.ToScreen(new GridPosition(feature.X, feature.Y));

            if (feature.Type == WorldVisualFeatureType.Landmark)
            {
                int intensity = 22 + (int)(12 * AnimationClock.PingPong(now, 1400, feature.Variant * 23));
                DrawRadialLight(g, point, 155, intensity, p.Accent);
            }
        }

        foreach (Character enemy in context.World.Enemies)
        {
            if (!context.IsVisible(enemy.X, enemy.Y))
                continue;

            Point point = context.ToScreen(new GridPosition(enemy.X, enemy.Y));
            DrawRadialLight(g, point, 70, 12, p.Hazard);
        }
    }

    private static void DrawRadialLight(Graphics g, Point center, int diameter, int alpha, Color color)
    {
        for (int i = 4; i >= 1; i--)
        {
            float scale = i / 4f;
            int size = Math.Max(2, (int)(diameter * scale));
            int a = Math.Max(1, alpha * (5 - i) / 5);

            using Brush glow = new SolidBrush(Color.FromArgb(a, color.R, color.G, color.B));
            g.FillEllipse(glow, center.X - size / 2, center.Y - size / 2, size, size);
        }
    }

    private static void DrawAbyssMotes(
        Graphics g,
        ExplorationRenderContext context,
        WorldPresentationProfile p,
        long now)
    {
        foreach (WorldVisualFeature feature in context.World.VisualFeatures)
        {
            if (feature.Type != WorldVisualFeatureType.Abyss ||
                !context.IsVisible(feature.X, feature.Y))
                continue;

            Point origin = context.ToScreen(new GridPosition(feature.X, feature.Y));
            int width = feature.Width * context.TileSize;
            int height = feature.Height * context.TileSize;

            for (int i = 0; i < 14; i++)
            {
                float phase = AnimationClock.Phase(
                    now,
                    4200 + i * 130,
                    feature.Variant * 97 + i * 53);

                float driftX = (phase * 2f - 1f) * 11f;
                float driftY = (phase * 2f - 1f) * 28f;
                int x = origin.X + 14 + PositiveMod(feature.Variant * 43 + i * 37, Math.Max(22, width - 20)) + (int)driftX;
                int y = origin.Y + 12 + PositiveMod(feature.Variant * 17 + i * 31, Math.Max(20, height - 18)) + (int)driftY;

                float alphaPulse = 0.35f + 0.65f * AnimationClock.PingPong(
                    now,
                    1300 + i * 80,
                    feature.Variant * 29 + i * 11);

                using Brush mote = new SolidBrush(Color.FromArgb(
                    (int)(35 + alphaPulse * 85),
                    p.Accent.R,
                    p.Accent.G,
                    p.Accent.B));

                int size = i % 3 == 0 ? 3 : 2;
                g.FillEllipse(mote, x, y, size, size);
            }
        }
    }

    private static void DrawResonanceWisps(
        Graphics g,
        ExplorationRenderContext context,
        WorldPresentationProfile p,
        long now)
    {
        foreach (WorldVisualFeature feature in context.World.VisualFeatures)
        {
            if (feature.Type != WorldVisualFeatureType.Landmark ||
                !context.IsVisible(feature.X, feature.Y))
                continue;

            Point center = context.ToScreen(new GridPosition(feature.X, feature.Y));
            float pulse = 0.5f + 0.5f * AnimationClock.Sine(now, 1400, feature.X * 41 + feature.Y * 17);

            for (int i = 0; i < 7; i++)
            {
                float phase = AnimationClock.Phase(now, 1900 + i * 90, feature.Variant * 71 + i * 23);
                float angle = phase * MathF.PI * 2f + i * 0.8f;
                float radius = 16f + pulse * 8f + i * 2.5f;

                using Brush wisp = new SolidBrush(Color.FromArgb(
                    55 + (int)(45 * pulse),
                    p.Accent.R,
                    p.Accent.G,
                    p.Accent.B));

                g.FillEllipse(
                    wisp,
                    center.X + MathF.Cos(angle) * radius - 2,
                    center.Y + MathF.Sin(angle) * radius - 5,
                    4,
                    7);
            }

            int rayAlpha = 30 + (int)(45 * pulse);
            using Pen rays = new(Color.FromArgb(rayAlpha, p.Accent.R, p.Accent.G, p.Accent.B), 1);
            for (int i = 0; i < 4; i++)
            {
                float angle = now / 1400f + i * MathF.PI / 2f;
                int length = 18 + (int)(pulse * 13);

                g.DrawLine(
                    rays,
                    center.X + (int)(MathF.Cos(angle) * 7),
                    center.Y + (int)(MathF.Sin(angle) * 7),
                    center.X + (int)(MathF.Cos(angle) * length),
                    center.Y + (int)(MathF.Sin(angle) * length));
            }
        }
    }

    private static void DrawAmbientDust(
        Graphics g,
        ExplorationRenderContext context,
        WorldPresentationProfile p,
        long now)
    {
        for (int i = 0; i < 32; i++)
        {
            float phase = AnimationClock.Phase(now, 5200 + i * 71, i * 313);
            int x = 24 + PositiveMod(i * 97, 812) + (int)(MathF.Sin(phase * MathF.PI * 2f) * 10f);
            int y = 28 + PositiveMod(i * 53, 548) - (int)(phase * 12f);

            if (y < 18)
                y += 548;

            int alpha = 22 + (int)(38 * (0.5f + 0.5f * MathF.Sin(phase * MathF.PI * 2f + i)));
            int size = i % 5 == 0 ? 3 : 2;

            using Brush dust = new SolidBrush(Color.FromArgb(
                alpha,
                p.Mist.R,
                p.Mist.G,
                p.Mist.B));
            g.FillEllipse(dust, x, y, size, size);
        }
    }

    private static void DrawMist(
        Graphics g,
        ExplorationRenderContext context,
        WorldPresentationProfile p,
        long now)
    {
        _ = context;

        for (int band = 0; band < 4; band++)
        {
            float phase = AnimationClock.Phase(now, 7200 + band * 500, band * 911);
            int y = 145 + band * 112 + (int)(MathF.Sin(phase * MathF.PI * 2f) * 9f);
            int alpha = 10 + (int)(8 * (0.5f + 0.5f * MathF.Sin(phase * MathF.PI * 2f)));

            using Brush mist = new SolidBrush(Color.FromArgb(
                alpha,
                p.Mist.R,
                p.Mist.G,
                p.Mist.B));
            g.FillEllipse(mist, 80, y, 680, 22);
        }
    }

    private static void DrawFeedback(
        Graphics g,
        ExplorationRenderContext context,
        WorldPresentationProfile p,
        long now)
    {
        FeedbackEffect? feedback = context.Feedback;
        if (feedback == null || !feedback.IsActive(now))
            return;

        float t = AnimationClock.AttackProgress(
            now,
            feedback.StartedAt,
            feedback.DurationMs);
        float pulse = 1f - t;

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

        int radius = feedback.Type == FeedbackEffectType.Danger
            ? 15 + (int)(36 * (1f - pulse))
            : 18 + (int)(54 * (1f - pulse));

        int alpha = Math.Max(8, (int)(155 * pulse));

        using Pen ring = new(Color.FromArgb(alpha, color.R, color.G, color.B), 2.5f);
        g.DrawEllipse(ring, center.X - radius, center.Y - radius, radius * 2, radius * 2);

        if (feedback.Type is
            FeedbackEffectType.Discovery or
            FeedbackEffectType.Victory or
            FeedbackEffectType.Extraction)
        {
            using Pen rays = new(Color.FromArgb(alpha, color.R, color.G, color.B), 2);

            for (int i = 0; i < 8; i++)
            {
                double angle = i * Math.PI / 4.0 + t * Math.PI * 0.25;
                int inner = Math.Max(3, radius / 2);
                int outer = radius + 14;

                g.DrawLine(
                    rays,
                    center.X + (int)(Math.Cos(angle) * inner),
                    center.Y + (int)(Math.Sin(angle) * inner),
                    center.X + (int)(Math.Cos(angle) * outer),
                    center.Y + (int)(Math.Sin(angle) * outer));
            }
        }

        if (feedback.Type is FeedbackEffectType.Loot or FeedbackEffectType.Heal)
        {
            for (int i = 0; i < 8; i++)
            {
                float angle = i * MathF.PI * 2f / 8f;
                float distance = 8f + t * 28f;

                using Brush spark = new SolidBrush(Color.FromArgb(
                    (int)(170 * pulse),
                    color.R,
                    color.G,
                    color.B));

                g.FillEllipse(
                    spark,
                    center.X + MathF.Cos(angle) * distance - 2,
                    center.Y + MathF.Sin(angle) * distance - 2,
                    4,
                    4);
            }
        }

        if (feedback.Type is FeedbackEffectType.Damage or FeedbackEffectType.Danger)
        {
            float shake = (1f - t) * 3f;
            int dx = (int)MathF.Round(MathF.Sin(now / 32f) * shake);
            int dy = (int)MathF.Round(MathF.Cos(now / 29f) * shake);

            using Pen impact = new(Color.FromArgb(alpha, color.R, color.G, color.B), 2);
            g.DrawEllipse(impact, center.X - radius + dx, center.Y - radius + dy, radius * 2, radius * 2);
        }
    }

    private static void DrawVignette(Graphics g, ExplorationRenderContext context)
    {
        // Vignette is scoped to the playable world, not the HUD.
        // The HUD remains crisp and unaffected by exploration atmosphere.
        WorldPresentationProfile p = context.Profile;
        Rectangle viewport = context.Layout.WorldViewport;
        using Brush top = new SolidBrush(Color.FromArgb(55, p.Void.R, p.Void.G, p.Void.B));
        using Brush bottom = new SolidBrush(Color.FromArgb(80, p.Void.R, p.Void.G, p.Void.B));
        g.FillRectangle(top, viewport.X, viewport.Y, viewport.Width, Math.Min(34, viewport.Height));
        g.FillRectangle(bottom, viewport.X, Math.Max(viewport.Y, viewport.Bottom - Math.Min(38, viewport.Height)), viewport.Width, Math.Min(38, viewport.Height));
    }

    private static int PositiveMod(int value, int divisor) =>
        divisor <= 0 ? 0 : ((value % divisor) + divisor) % divisor;
}
