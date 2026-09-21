using System.Drawing;
using System.Drawing.Drawing2D;

public sealed class ForegroundRenderer
{
    public void Draw(Graphics g, ExplorationRenderContext context)
    {
        WorldPresentationProfile p = context.Profile;
        long now = AnimationClock.Now;
        var originalSmoothing = g.SmoothingMode;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        using Brush side = new SolidBrush(Color.FromArgb(185, p.Foreground.R, p.Foreground.G, p.Foreground.B));

        Point[] leftFraming =
        {
            new Point(0, 0), new Point(42, 0), new Point(34, 95), new Point(48, 180),
            new Point(31, 276), new Point(43, 388), new Point(28, 490), new Point(46, 620), new Point(0, 620)
        };
        Point[] rightFraming =
        {
            new Point(860, 0), new Point(818, 0), new Point(826, 110), new Point(812, 215),
            new Point(829, 305), new Point(815, 420), new Point(832, 525), new Point(810, 620), new Point(860, 620)
        };
        g.FillPolygon(side, leftFraming);
        g.FillPolygon(side, rightFraming);

        using Brush bottom = new SolidBrush(Color.FromArgb(135, p.Foreground.R, p.Foreground.G, p.Foreground.B));
        Point[] debrisBand =
        {
            new Point(0, 605), new Point(85, 575), new Point(150, 592), new Point(230, 565),
            new Point(315, 598), new Point(390, 578), new Point(470, 603), new Point(545, 573),
            new Point(625, 596), new Point(705, 566), new Point(780, 592), new Point(860, 575),
            new Point(860, 620), new Point(0, 620)
        };
        g.FillPolygon(bottom, debrisBand);

        using Pen edge = new(Color.FromArgb(120, p.WallHighlight.R, p.WallHighlight.G, p.WallHighlight.B), 2);
        g.DrawLine(edge, 42, 0, 34, 95);
        g.DrawLine(edge, 34, 95, 48, 180);
        g.DrawLine(edge, 818, 0, 826, 110);
        g.DrawLine(edge, 826, 110, 812, 215);

        using Brush hanging = new SolidBrush(Color.FromArgb(155, p.Foreground.R, p.Foreground.G, p.Foreground.B));
        Point[] fragmentLeft =
        {
            new Point(92, 0), new Point(118, 0), new Point(114, 58), new Point(104, 77), new Point(97, 48)
        };
        Point[] fragmentRight =
        {
            new Point(738, 0), new Point(765, 0), new Point(761, 43), new Point(750, 63), new Point(744, 31)
        };
        float leftSway = AnimationClock.Sine(now, 3100, 17) * 3f;
        float rightSway = AnimationClock.Sine(now, 3600, 29) * 4f;

        g.TranslateTransform(leftSway, 0f);
        g.FillPolygon(hanging, fragmentLeft);
        g.ResetTransform();

        g.TranslateTransform(rightSway, 0f);
        g.FillPolygon(hanging, fragmentRight);
        g.ResetTransform();

        g.SmoothingMode = originalSmoothing;
    }
}
