using System.Drawing;

public class ExplorationHudRenderer : IDisposable
{
    private readonly AtlasSlicer uiAtlas = new();
    private readonly Font uiFont = new(FontFamily.GenericMonospace, 14);
    private readonly Font titleFont = new(FontFamily.GenericMonospace, 20);
    private readonly Font bottomFont = new(FontFamily.GenericMonospace, 14);

    public void Draw(Graphics graphics, GameWorld world)
    {
        const int sidebarX = 780;
        graphics.DrawLine(Pens.White, sidebarX, 20, sidebarX, 620);
        float x = sidebarX + 25; float y = 30;
        graphics.DrawString("STATUS", titleFont, Brushes.White, x, y);
        uiAtlas.Draw(graphics, uiAtlas.GetPortraitRegion(AtlasPortrait.Cyrus), new Rectangle((int)x, (int)y + 35, 120, 120));
        y += 165;
        graphics.DrawString(world.Player.Name, titleFont, Brushes.Red, x, y);
        y += 35; graphics.DrawString($"LV {world.Player.Level}", uiFont, Brushes.White, x, y);
        y += 30; graphics.DrawString($"HP {world.Player.HP}/{world.Player.MAXHP}", uiFont, Brushes.White, x, y);
        y += 45; graphics.DrawLine(Pens.White, x, y, x + 220, y);
        y += 25; graphics.DrawString($"Position: ({world.Player.X}, {world.Player.Y})", uiFont, Brushes.White, x, y);

        const int bottomY = 640;
        graphics.DrawLine(Pens.White, 20, bottomY, 1080, bottomY);
        graphics.DrawString("W/A/S/D  MOVE", bottomFont, Brushes.White, 30, bottomY + 15);
        graphics.DrawString("C  STATS", bottomFont, Brushes.White, 260, bottomY + 15);
        graphics.DrawString("B  TEST BATTLE", bottomFont, Brushes.White, 430, bottomY + 15);
        graphics.DrawString("E  INTERACT", bottomFont, Brushes.White, 650, bottomY + 15);
    }

    public void Dispose()
    {
        uiAtlas.Dispose(); uiFont.Dispose(); titleFont.Dispose(); bottomFont.Dispose();
    }
}
