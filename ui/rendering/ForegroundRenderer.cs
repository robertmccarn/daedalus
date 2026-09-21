using System.Drawing;
using System.Drawing.Drawing2D;

public sealed class ForegroundRenderer
{
    public void Draw(Graphics g, ExplorationRenderContext context)
    {
        WorldPresentationProfile p = context.Profile;
        var originalSmoothing = g.SmoothingMode;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        using Brush side = new SolidBrush(Color.FromArgb(185, p.Foreground.R, p.Foreground.G, p.Foreground.B));

        Point[] leftFraming =
        {
            new(0, 0),
            new(42, 0),
            new(34, 95),
            new(48, 180),
            new(31, 276),
            new(43, 388),
            new(28, 490),
            new(46, 620),
            new(0, 620)
        };

        Point[] rightFraming =
        {
            new(860, 0),
            new(818, 0),
            new(826, 110),
            new(812, 215),
            new(829, 305),
            new(815, 420),
            new(832, 525),
            new(810, 620),
            new(860, 620)
        };

        g.FillPolygon(side, leftFraming);
        g.FillPolygon(side, rightFraming);

        using Brush bottom = new SolidBrush(Color.FromArgb(135, p.Foreground.R, p.Foreground.G, p.Foreground.B));
        Point[] debrisBand =
        {
            new(0, 605),
            new(85, 575),
            new(150, 592),
            new(230, 565),
            new(315, 598),
            new(390, 578),
            new(470, 603),
            new(545, 573),
            new(625, 596),
            new(705, 566),
            new(780, 592),
            new(860, 575),
            new(860, 620),
            new(0, 620)
        };
        g.FillPolygon(bottom, debrisBand);

        using Pen edge = new(
            Color.FromArgb(120, p.WallHighlight.R, p.WallHighlight.G, p.WallHighlight.B),
            2);
        g.DrawLine(edge, 42, 0, 34, 95);
        g.DrawLine(edge, 34, 95, 48, 180);
        g.DrawLine(edge, 818, 0, 826, 110);
        g.DrawLine(edge, 826, 110, 812, 215);

        using Brush hanging = new SolidBrush(Color.FromArgb(155, p.Foreground.R, p.Foreground.G, p.Foreground.B));
        g.FillPolygon(hanging, new[]
        {
            new(92, 0), new(118, 0), new(114, 58), new(104, 77), new(97, 48)
        });
        g.FillPolygon(hanging, new[]
        {
            new(738, 0), new(765, 0), new(761, 43), new(750, 63), new(744, 31)
        });

        g.SmoothingMode = originalSmoothing;
    }
}
