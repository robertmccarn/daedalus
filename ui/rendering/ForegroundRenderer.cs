using System.Drawing;
using System.Drawing.Drawing2D;

public sealed class ForegroundRenderer
{
    public void Draw(Graphics g, ExplorationRenderContext context)
    {
        WorldPresentationProfile p = context.Profile;
        long now = AnimationClock.Now;
        Rectangle viewport = context.Layout.WorldViewport;
        int width = viewport.Width;
        int height = viewport.Height;
        var originalSmoothing = g.SmoothingMode;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        using Brush side = new SolidBrush(Color.FromArgb(185, p.Foreground.R, p.Foreground.G, p.Foreground.B));

        Point[] leftFraming =
        {
            new Point(0, 0), new Point(42, 0), new Point(34, (int)(height * 0.15f)), new Point(48, (int)(height * 0.29f)),
            new Point(31, (int)(height * 0.45f)), new Point(43, (int)(height * 0.63f)), new Point(28, (int)(height * 0.79f)), new Point(46, height), new Point(0, height)
        };
        Point[] rightFraming =
        {
            new Point(width, 0), new Point(width - 42, 0), new Point(width - 34, (int)(height * 0.18f)), new Point(width - 48, (int)(height * 0.35f)),
            new Point(width - 31, (int)(height * 0.50f)), new Point(width - 43, (int)(height * 0.68f)), new Point(width - 28, (int)(height * 0.85f)), new Point(width - 46, height), new Point(width, height)
        };
        g.FillPolygon(side, leftFraming);
        g.FillPolygon(side, rightFraming);

        using Brush bottom = new SolidBrush(Color.FromArgb(135, p.Foreground.R, p.Foreground.G, p.Foreground.B));
        Point[] debrisBand =
        {
            new Point(0, (int)(height * 0.976f)), new Point((int)(width * 0.10f), (int)(height * 0.927f)), new Point((int)(width * 0.175f), (int)(height * 0.953f)), new Point((int)(width * 0.27f), (int)(height * 0.911f)),
            new Point((int)(width * 0.366f), (int)(height * 0.965f)), new Point((int)(width * 0.454f), (int)(height * 0.932f)), new Point((int)(width * 0.547f), (int)(height * 0.973f)), new Point((int)(width * 0.634f), (int)(height * 0.924f)),
            new Point((int)(width * 0.727f), (int)(height * 0.961f)), new Point((int)(width * 0.82f), (int)(height * 0.91f)), new Point((int)(width * 0.907f), (int)(height * 0.953f)), new Point(width, (int)(height * 0.927f)),
            new Point(width, height), new Point(0, height)
        };
        g.FillPolygon(bottom, debrisBand);

        using Pen edge = new(Color.FromArgb(120, p.WallHighlight.R, p.WallHighlight.G, p.WallHighlight.B), 2);
        g.DrawLine(edge, 42, 0, 34, (int)(height * 0.15f));
        g.DrawLine(edge, 34, (int)(height * 0.15f), 48, (int)(height * 0.29f));
        g.DrawLine(edge, width - 42, 0, width - 34, (int)(height * 0.18f));
        g.DrawLine(edge, width - 34, (int)(height * 0.18f), width - 48, (int)(height * 0.35f));

        using Brush hanging = new SolidBrush(Color.FromArgb(155, p.Foreground.R, p.Foreground.G, p.Foreground.B));
        Point[] fragmentLeft =
        {
            new Point((int)(width * 0.107f), 0), new Point((int)(width * 0.137f), 0), new Point((int)(width * 0.132f), 58), new Point((int)(width * 0.121f), 77), new Point((int)(width * 0.113f), 48)
        };
        Point[] fragmentRight =
        {
            new Point((int)(width * 0.858f), 0), new Point((int)(width * 0.89f), 0), new Point((int)(width * 0.886f), 43), new Point((int)(width * 0.872f), 63), new Point((int)(width * 0.865f), 31)
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
