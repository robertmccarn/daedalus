using System.Drawing;
using Systemic.Engine.State;

public sealed class CampaignRenderer : IDisposable
{
    private readonly Font title = new(FontFamily.GenericMonospace, 28, FontStyle.Bold);
    private readonly Font section = new(FontFamily.GenericMonospace, 15, FontStyle.Bold);
    private readonly Font body = new(FontFamily.GenericMonospace, 12);
    private readonly Font small = new(FontFamily.GenericMonospace, 10);

    public void Draw(Graphics g, GameStateManager stateManager, string message)
    {
        long now = AnimationClock.Now;
        g.Clear(Color.FromArgb(8, 10, 13));
        CampaignState c = stateManager.Campaign;

        using Brush panel = new SolidBrush(Color.FromArgb(230, 15, 19, 23));
        g.FillRectangle(panel, 40, 35, 1020, 610);

        using Pen border = new(Color.FromArgb(165, 109, 120, 124), 1);
        g.DrawRectangle(border, 40, 35, 1020, 610);

        g.DrawString("DAEDALUS // CAMP", title, Brushes.White, 72, 62);
        g.DrawString("EXPEDITION CONTROL", small, Brushes.Gainsboro, 75, 103);

        g.DrawString("CAMPAIGN", section, Brushes.White, 75, 145);
        g.DrawString($"GOLD           {c.Gold}", body, Brushes.Gainsboro, 75, 178);
        g.DrawString($"RUNS COMPLETED {c.RunsCompleted}", body, Brushes.Gainsboro, 75, 202);
        g.DrawString($"HIGHEST DEPTH  {c.HighestDepth}", body, Brushes.Gainsboro, 75, 226);
        g.DrawString($"ALIGNMENT      {c.Alignment}", body, Brushes.Gainsboro, 75, 250);

        g.DrawString("ROSTER", section, Brushes.White, 390, 145);
        int y = 180;
        foreach (PartyMember member in c.PartyRoster.Take(5))
        {
            Color accent = member.SpriteId.ToLowerInvariant() switch
            {
                "lyra" => Color.FromArgb(116, 176, 170),
                "marek" => Color.FromArgb(173, 132, 89),
                "sera" => Color.FromArgb(154, 120, 177),
                _ => Color.FromArgb(122, 152, 157)
            };

            float memberPulse = 0.5f + 0.5f * AnimationClock.Sine(now, 1700 + y * 3, y);
            using Brush marker = new SolidBrush(Color.FromArgb(
                145 + (int)(80 * memberPulse),
                accent.R,
                accent.G,
                accent.B));
            g.FillRectangle(marker, 390, y + 2, 5, 27);
            g.DrawString(member.Name, body, Brushes.White, 405, y);
            g.DrawString($"LV {member.Level:00}  HP {member.MaxHP:00}  M {member.Morale:000}", small, Brushes.Gainsboro, 510, y + 2);
            y += 48;
        }

        g.DrawString("STASH", section, Brushes.White, 75, 315);
        int stashY = 350;
        foreach (InventoryItem item in c.Stash.Take(5))
        {
            g.DrawString($"{item.Name}  x{item.Quantity}", body, Brushes.Gainsboro, 75, stashY);
            stashY += 24;
        }

        g.DrawString("MATERIALS", section, Brushes.White, 390, 420);
        int materialY = 455;
        foreach (Material material in c.Materials.Take(6))
        {
            g.DrawString($"{material.Name}  x{material.Quantity}", body, Brushes.Gainsboro, 390, materialY);
            materialY += 22;
        }

        float actionPulse = 0.5f + 0.5f * AnimationClock.Sine(now, 1100);
        using Brush accentBrush = new SolidBrush(Color.FromArgb(
            170 + (int)(65 * actionPulse),
            125,
            180,
            177));
        g.DrawString("ENTER  BEGIN NEXT EXPEDITION", section, accentBrush, 72, 590);

        if (!string.IsNullOrWhiteSpace(message))
            g.DrawString(message, small, Brushes.Gainsboro, 72, 618);
    }

    public void Dispose()
    {
        title.Dispose();
        section.Dispose();
        body.Dispose();
        small.Dispose();
    }
}
