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

        // The teal/biome light is deliberately localized rather than a global glow.
        float pulse = 0.5f + 0.5f * AnimationClock.Sine(now, 2400);
        using Brush light = new SolidBrush(Color.FromArgb(
            34 + (int)(14 * pulse),
            p.Accent.R,
            p.Accent.G,
            p.Accent.B));
        g.FillEllipse(light, 610, 55, 230, 230);

        using Brush core = new SolidBrush(Color.FromArgb(
            52 + (int)(24 * pulse),
            p.Accent.R,
            p.Accent.G,
            p.Accent.B));
        g.FillEllipse(core, 685 - (int)(pulse * 3), 105 - (int)(pulse * 3), 95 + (int)(pulse * 6), 95 + (int)(pulse * 6));
    }
}
