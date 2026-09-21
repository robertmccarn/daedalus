using System.Drawing;

public sealed class DevMenuRenderer : IDisposable
{
    private readonly Font titleFont = new(FontFamily.GenericMonospace, 26, FontStyle.Bold);
    private readonly Font sectionFont = new(FontFamily.GenericMonospace, 14, FontStyle.Bold);
    private readonly Font bodyFont = new(FontFamily.GenericMonospace, 12);
    private readonly Font smallFont = new(FontFamily.GenericMonospace, 10);

    public void Draw(Graphics g, int floor, int partyLevel)
    {
        using SolidBrush shade = new(Color.FromArgb(155, 0, 0, 0));
        g.FillRectangle(shade, 0, 0, 1100, 700);

        using SolidBrush panel = new(Color.FromArgb(245, 10, 14, 18));
        g.FillRectangle(panel, 210, 105, 680, 490);

        using Pen border = new(Color.FromArgb(220, 117, 180, 177), 2);
        g.DrawRectangle(border, 210, 105, 680, 490);

        using SolidBrush accent = new(Color.FromArgb(235, 125, 190, 185));
        g.DrawString("DAEDALUS // DEV MENU", titleFont, Brushes.White, 250, 145);
        g.DrawString("DEBUG EXPEDITION JUMP", sectionFont, accent, 252, 195);

        g.DrawString("DEPTH", smallFont, Brushes.Gainsboro, 255, 245);
        using SolidBrush floorBrush = new(Color.FromArgb(245, 235, 240, 240));
        g.DrawString(floor.ToString("00"), titleFont, floorBrush, 255, 268);
        g.DrawString($"PARTY LEVEL  {partyLevel:00}", bodyFont, Brushes.Gainsboro, 410, 281);

        g.DrawString(
            "Every party member will be normalized to the selected level.",
            smallFont,
            Brushes.Gainsboro,
            410,
            310);
        g.DrawString(
            "A matching Weapon / Armor / Ring loadout will be equipped.",
            smallFont,
            Brushes.Gainsboro,
            410,
            330);

        g.DrawString("CONTROLS", sectionFont, Brushes.White, 255, 375);
        g.DrawString("W / S        depth -1 / +1", bodyFont, Brushes.Gainsboro, 255, 408);
        g.DrawString("A / D        depth -5 / +5", bodyFont, Brushes.Gainsboro, 255, 432);
        g.DrawString("E / ENTER    jump to selected depth", bodyFont, Brushes.Gainsboro, 255, 456);
        g.DrawString("ESC / F1      close dev menu", bodyFont, Brushes.Gainsboro, 255, 480);

        g.DrawString(
            "WARNING: DEV JUMPS intentionally bypass normal campaign progression.",
            smallFont,
            new SolidBrush(Color.FromArgb(220, 208, 156, 112)),
            255,
            535);
    }

    public void Dispose()
    {
        titleFont.Dispose();
        sectionFont.Dispose();
        bodyFont.Dispose();
        smallFont.Dispose();
    }
}
