using System.Drawing;
using System.Drawing.Drawing2D;

public sealed class BackdropRenderer
{
    public void Draw(Graphics g, ExplorationRenderContext context)
    {
        WorldPresentationProfile p = context.Profile;
        long now = AnimationClock.Now;
        Rectangle viewport = context.Layout.WorldViewport;
        int width = viewport.Width;
        int height = viewport.Height;
        g.Clear(p.Void);

        using LinearGradientBrush gradient = new(
            viewport,
            p.Void,
            Color.FromArgb(38, 42, 47),
            LinearGradientMode.Vertical);
        g.FillRectangle(gradient, viewport);

        // Distant ruin silhouettes create the large-scale environmental read
        // before the playable grid is drawn.
        using Brush distant = new SolidBrush(Color.FromArgb(95, p.Wall.R, p.Wall.G, p.Wall.B));
        for (int i = 0; i < 9; i++)
        {
            int drift = (int)MathF.Round(AnimationClock.Sine(now, 9000 + i * 220, i * 317) * 3f);
            int x = 20 + i * Math.Max(60, width / 9) + drift;
            int ruinHeight = 90 + (i * 37 % Math.Max(90, height / 3));
            int baseY = (int)(height * 0.53f);
            g.FillRectangle(distant, x, baseY - ruinHeight, 58, ruinHeight);
            g.FillRectangle(distant, x - 12, baseY - ruinHeight - 30, 82, 18);
        }

        using Pen cracks = new(Color.FromArgb(75, p.Accent.R, p.Accent.G, p.Accent.B), 2);
        for (int i = 0; i < 7; i++)
        {
            int x = 90 + i * Math.Max(70, width / 7);
            g.DrawLine(cracks, x, 80 + i * 19, x + 34, 160 + i * 21);
        }

        // Keep the supernatural source localized around the benchmark focal zone.
        // It scales with the world viewport instead of assuming a fixed 1100x700 frame.
        float pulse = 0.5f + 0.5f * AnimationClock.Sine(now, 2400);
        Point source = new(
            viewport.X + (int)(viewport.Width * 0.68f),
            viewport.Y + (int)(viewport.Height * 0.27f));
        LocalLightRenderer.Draw(g, source, Math.Max(70, (int)(Math.Min(viewport.Width, viewport.Height) * 0.18f)), 90, p.Accent, pulse * 0.18f);

        using Pen sourceRay = new(Color.FromArgb(22 + (int)(12 * pulse), p.Accent.R, p.Accent.G, p.Accent.B), 1);
        for (int i = -2; i <= 2; i++)
        {
            int endX = source.X + i * Math.Max(24, viewport.Width / 16);
            g.DrawLine(sourceRay, source.X, source.Y, endX, viewport.Y + (int)(viewport.Height * 0.72f));
        }
    }
}
