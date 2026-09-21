using System.Drawing;

public sealed class EffectRenderer
{
    public void Draw(Graphics g, ExplorationRenderContext context)
    {
        long ticks = Environment.TickCount64;
        float pulse = (float)(0.5 + 0.5 * Math.Sin(ticks / 420.0));
        WorldPresentationProfile p = context.Profile;

        // Localized teal discovery light around the leader.
        Point center = context.ToScreen(context.Party.LeaderPosition);
        int alpha = 18 + (int)(18 * pulse);
        using Brush glow = new SolidBrush(Color.FromArgb(alpha, p.Accent.R, p.Accent.G, p.Accent.B));
        g.FillEllipse(glow, center.X - 100, center.Y - 100, 200, 200);

        // Small drifting particles make the scene feel alive without requiring a GPU.
        using Brush dust = new SolidBrush(Color.FromArgb(55, p.Mist.R, p.Mist.G, p.Mist.B));
        for (int i = 0; i < 24; i++)
        {
            int x = (i * 71 + (int)(ticks / 13)) % 820 + 20;
            int y = (i * 47 + (int)(ticks / 21)) % 560 + 20;
            g.FillEllipse(dust, x, y, 2, 2);
        }
    }
}
