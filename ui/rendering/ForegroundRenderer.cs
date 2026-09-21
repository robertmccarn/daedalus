using System.Drawing;

public sealed class ForegroundRenderer
{
    public void Draw(Graphics g, ExplorationRenderContext context)
    {
        WorldPresentationProfile p = context.Profile;

        using Brush left = new SolidBrush(Color.FromArgb(215, p.Foreground.R, p.Foreground.G, p.Foreground.B));
        using Brush right = new SolidBrush(Color.FromArgb(170, p.Foreground.R, p.Foreground.G, p.Foreground.B));

        g.FillRectangle(left, 0, 0, 28, 620);
        g.FillRectangle(right, 832, 0, 28, 620);

        using Pen edge = new(Color.FromArgb(130, p.WallHighlight.R, p.WallHighlight.G, p.WallHighlight.B), 2);
        g.DrawLine(edge, 28, 0, 28, 620);
        g.DrawLine(edge, 832, 0, 832, 620);
    }
}
