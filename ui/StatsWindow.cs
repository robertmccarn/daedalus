using System.Drawing;
using System.Windows.Forms;
using Systemic.Engine.State;

public class StatsWindow : Form
{
    private readonly PartyMember member;

    public StatsWindow(PartyMember member)
    {
        this.member = member;

        Text = "Character Stats";
        ClientSize = new Size(400, 400);
        BackColor = Color.Black;
        ForeColor = Color.White;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        Paint += DrawStats;
    }

    private void DrawStats(object? sender, PaintEventArgs e)
    {
        using Font titleFont = new(FontFamily.GenericMonospace, 22);
        using Font textFont = new(FontFamily.GenericMonospace, 16);

        float x = 30;
        float y = 30;

        e.Graphics.DrawString("CHARACTER", titleFont, Brushes.White, x, y);
        y += 50;
        e.Graphics.DrawString(member.Name, textFont, Brushes.Red, x, y);
        y += 30;
        e.Graphics.DrawString($"Level: {member.Level}", textFont, Brushes.White, x, y);
        y += 30;
        e.Graphics.DrawString($"HP: {member.HP}/{member.MaxHP}", textFont, Brushes.White, x, y);
        y += 30;
        e.Graphics.DrawString($"Morale: {member.Morale}", textFont, Brushes.White, x, y);
        y += 45;
        e.Graphics.DrawString($"Strength: {member.Stats.Strength}", textFont, Brushes.White, x, y);
        y += 25;
        e.Graphics.DrawString($"Magic:    {member.Stats.Magic}", textFont, Brushes.White, x, y);
        y += 25;
        e.Graphics.DrawString($"Agility:  {member.Stats.Agility}", textFont, Brushes.White, x, y);
        y += 25;
        e.Graphics.DrawString($"Luck:     {member.Stats.Luck}", textFont, Brushes.White, x, y);
        y += 35;
        e.Graphics.DrawString("Specializations:", textFont, Brushes.White, x, y);
        y += 25;
        e.Graphics.DrawString(
            member.Specializations.Count == 0 ? "None" : string.Join(", ", member.Specializations),
            textFont,
            Brushes.White,
            x,
            y);
    }
}
