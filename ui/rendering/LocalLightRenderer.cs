using System.Drawing;

public static class LocalLightRenderer
{
    public static void Draw(
        Graphics g,
        Point center,
        int radius,
        int intensity,
        Color color,
        float pulse = 0f)
    {
        radius = Math.Max(2, radius);
        intensity = Math.Clamp(intensity + (int)(intensity * pulse), 1, 255);

        for (int layer = 4; layer >= 1; layer--)
        {
            float scale = layer / 4f;
            int size = Math.Max(2, (int)(radius * 2f * scale));
            float falloff = 1f - layer / 5f;
            int alpha = Math.Max(1, (int)(intensity * falloff * 0.22f));
            using Brush glow = new SolidBrush(Color.FromArgb(alpha, color.R, color.G, color.B));
            g.FillEllipse(glow, center.X - size / 2, center.Y - size / 2, size, size);
        }
    }
}
