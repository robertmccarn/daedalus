using System.Drawing;

public sealed class ExtractionResultsRenderer : IDisposable
{
    private readonly Font title = new(FontFamily.GenericMonospace, 28, FontStyle.Bold);
    private readonly Font section = new(FontFamily.GenericMonospace, 15, FontStyle.Bold);
    private readonly Font body = new(FontFamily.GenericMonospace, 13);
    private readonly Font small = new(FontFamily.GenericMonospace, 10);

    public void Draw(Graphics g, ExtractionSummary? summary, string message)
    {
        long now = AnimationClock.Now;
        g.Clear(Color.FromArgb(7, 10, 12));

        float pulse = 0.5f + 0.5f * AnimationClock.Sine(now, 1800);
        using Brush panel = new SolidBrush(Color.FromArgb(232, 14, 18, 22));
        g.FillRectangle(panel, 90, 70, 920, 520);

        using Pen border = new(
            Color.FromArgb(155 + (int)(50 * pulse), 117, 159, 155),
            1);
        g.DrawRectangle(border, 90, 70, 920, 520);

        for (int i = 0; i < 12; i++)
        {
            float phase = AnimationClock.Phase(now, 3600 + i * 120, i * 71);
            float x = 120 + i * 70 + MathF.Sin(phase * MathF.PI * 2f) * 8f;
            float y = 90 + (0.5f + 0.5f * MathF.Cos(phase * MathF.PI * 2f)) * 460f;
            using Brush mote = new SolidBrush(Color.FromArgb(
                18 + (int)(28 * pulse),
                117,
                159,
                155));
            g.FillEllipse(mote, x, y, 2, 2);
        }

        g.DrawString("EXPEDITION EXTRACTED", title, Brushes.White, 125, 105);
        g.DrawString("RECOVERED MATERIAL", small, Brushes.Gainsboro, 128, 150);

        if (summary == null)
        {
            g.DrawString(message, body, Brushes.White, 128, 205);
        }
        else
        {
            g.DrawString($"DEPTH REACHED     {summary.Depth:00}", body, Brushes.White, 128, 200);
            g.DrawString($"GOLD RECOVERED    +{summary.Gold}", body, Brushes.Gainsboro, 128, 235);
            g.DrawString($"ENERGY CORES      +{summary.CoreCount}", body, Brushes.Gainsboro, 128, 270);
            g.DrawString($"MATERIALS         +{summary.MaterialCount}", body, Brushes.Gainsboro, 128, 305);
            g.DrawString($"ITEMS              +{summary.ItemCount}", body, Brushes.Gainsboro, 128, 340);
            g.DrawString($"GEAR               +{summary.GearCount}", body, Brushes.Gainsboro, 128, 375);
            g.DrawString($"CAMPAIGN GOLD     {summary.CampaignGoldAfter}", body, Brushes.White, 128, 430);
            g.DrawString($"RUNS COMPLETED    {summary.CampaignRunsAfter}", body, Brushes.White, 128, 465);
        }

        int accentAlpha = 175 + (int)(70 * pulse);
        using Brush accent = new SolidBrush(Color.FromArgb(accentAlpha, 125, 180, 177));
        g.DrawString("ENTER  RETURN TO CAMP", section, accent, 128, 535);
    }

    public void Dispose()
    {
        title.Dispose();
        section.Dispose();
        body.Dispose();
        small.Dispose();
    }
}
