using System.Drawing;
using Systemic.Engine.State;

public class ExplorationHudRenderer : IDisposable
{
    private readonly Font uiFont = new(FontFamily.GenericMonospace, 14);
    private readonly Font titleFont = new(FontFamily.GenericMonospace, 20);
    private readonly Font bottomFont = new(FontFamily.GenericMonospace, 14);

    public void Draw(Graphics graphics, PartyController party, ExpeditionState expedition)
    {
        const int sidebarX = 780;
        graphics.DrawLine(Pens.White, sidebarX, 20, sidebarX, 620);
        float x = sidebarX + 25;
        float y = 30;
        graphics.DrawString("PARTY", titleFont, Brushes.White, x, y);

        PartyMember? leader = expedition.Party
            .FirstOrDefault(member => member.Id == party.LeaderId);

        if (leader == null)
            return;

        graphics.FillRectangle(Brushes.DarkSlateBlue, x, y + 35, 120, 120);
        graphics.DrawRectangle(Pens.White, x, y + 35, 120, 120);
        graphics.DrawString(leader.Name, uiFont, Brushes.White, x + 12, y + 85);

        y += 165;
        graphics.DrawString(leader.Name, titleFont, Brushes.Red, x, y);
        y += 35;
        graphics.DrawString($"LV {leader.Level}", uiFont, Brushes.White, x, y);
        y += 30;
        graphics.DrawString($"HP {leader.HP}/{leader.MaxHP}", uiFont, Brushes.White, x, y);
        y += 30;
        graphics.DrawString($"MORALE {leader.Morale}", uiFont, Brushes.White, x, y);
        y += 40;
        graphics.DrawLine(Pens.White, x, y, x + 220, y);
        y += 25;
        GridPosition position = party.LeaderPosition;
        graphics.DrawString($"Position: ({position.X}, {position.Y})", uiFont, Brushes.White, x, y);
        y += 30;
        graphics.DrawString($"Party: {expedition.Party.Count}/4", uiFont, Brushes.White, x, y);
        y += 30;
        graphics.DrawString($"Formation: {expedition.Formation}", uiFont, Brushes.White, x, y);

        const int bottomY = 640;
        graphics.DrawLine(Pens.White, 20, bottomY, 1080, bottomY);
        graphics.DrawString("W/A/S/D  MOVE", bottomFont, Brushes.White, 30, bottomY + 15);
        graphics.DrawString("C  STATS", bottomFont, Brushes.White, 260, bottomY + 15);
        graphics.DrawString("B  TEST BATTLE", bottomFont, Brushes.White, 430, bottomY + 15);
        graphics.DrawString("E  INTERACT", bottomFont, Brushes.White, 650, bottomY + 15);
    }

    public void Dispose()
    {
        uiFont.Dispose();
        titleFont.Dispose();
        bottomFont.Dispose();
    }
}
