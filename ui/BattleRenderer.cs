using System.Drawing;
using System.Drawing.Drawing2D;
using Systemic.Engine.State;

public class BattleRenderer : IDisposable
{
    private readonly Font titleFont = new(FontFamily.GenericMonospace, 26, FontStyle.Bold);
    private readonly Font uiFont = new(FontFamily.GenericMonospace, 14);
    private readonly Font smallFont = new(FontFamily.GenericMonospace, 11);

    public void Draw(
        Graphics graphics,
        GameWorld world,
        PartyController party,
        ExpeditionState expedition,
        Character? enemy,
        string message,
        BattleCommand selectedCommand)
    {
        BiomeType biome = BiomeCatalog.ForFloor(world.Floor);
        WorldPresentationProfile p = WorldPresentationProfile.ForBiome(biome);
        graphics.Clear(Color.FromArgb(10, 12, 15));

        using LinearGradientBrush bg = new(
            new Rectangle(0, 0, 1100, 700),
            p.Void,
            Color.FromArgb(42, 34, 35),
            LinearGradientMode.Vertical);
        graphics.FillRectangle(bg, 0, 0, 1100, 700);

        DrawArena(graphics, p);
        graphics.DrawString("TACTICAL CONTACT", titleFont, Brushes.White, 45, 30);
        graphics.DrawString(BiomeCatalog.Name(biome), smallFont, new SolidBrush(p.Accent), 48, 65);

        DrawEnemies(graphics, p, world, enemy);
        DrawParty(graphics, p, expedition, party);

        DrawCommands(graphics, p, selectedCommand);
        DrawDescription(graphics, p, message);
        DrawEnemyTracker(graphics, p, world, enemy);
    }

    private static void DrawArena(Graphics g, WorldPresentationProfile p)
    {
        using Pen grid = new(Color.FromArgb(35, p.WallHighlight.R, p.WallHighlight.G, p.WallHighlight.B), 1);
        for (int x = 60; x <= 810; x += 38)
            g.DrawLine(grid, x, 110, x, 470);
        for (int y = 110; y <= 470; y += 38)
            g.DrawLine(grid, 60, y, 810, y);

        using Brush floor = new SolidBrush(Color.FromArgb(80, p.Floor.R, p.Floor.G, p.Floor.B));
        g.FillRectangle(floor, 60, 110, 750, 360);
        using Pen border = new(Color.FromArgb(160, p.WallHighlight.R, p.WallHighlight.G, p.WallHighlight.B), 2);
        g.DrawRectangle(border, 60, 110, 750, 360);
    }

    private static void DrawParty(
        Graphics g,
        WorldPresentationProfile p,
        ExpeditionState expedition,
        PartyController party)
    {
        int index = 0;
        foreach (PartyMember member in expedition.Party.Take(4))
        {
            int x = 150 + (index % 2) * 120;
            int y = 190 + (index / 2) * 120;
            bool leader = member.Id == party.LeaderId;
            Color c = leader ? p.Accent : Color.FromArgb(150 + index * 20, 170, 180);

            using Brush shadow = new SolidBrush(Color.FromArgb(100, 0, 0, 0));
            g.FillEllipse(shadow, x - 2, y + 45, 56, 14);
            using Brush body = new SolidBrush(c);
            g.FillEllipse(body, x, y, 52, 52);
            using Brush face = new SolidBrush(Color.FromArgb(195, 165, 145));
            g.FillEllipse(face, x + 14, y - 10, 24, 24);

            g.DrawString(member.Name, new Font(FontFamily.GenericMonospace, 10), Brushes.White, x - 6, y + 62);
            g.DrawString($"{member.HP}/{member.MaxHP}", new Font(FontFamily.GenericMonospace, 9), Brushes.Gainsboro, x - 6, y + 77);

            if (leader)
            {
                using Pen marker = new(p.Accent, 2);
                g.DrawEllipse(marker, x - 5, y - 5, 62, 62);
            }
            index++;
        }

        g.DrawString("PARTY", new Font(FontFamily.GenericMonospace, 11, FontStyle.Bold), Brushes.White, 115, 145);
    }

    private static void DrawEnemies(
        Graphics g,
        WorldPresentationProfile p,
        GameWorld world,
        Character? currentEnemy)
    {
        List<Character> enemies = world.Enemies
            .Where(e => currentEnemy == null || e == currentEnemy || world.Enemies.Contains(e))
            .Take(4)
            .ToList();

        for (int i = 0; i < enemies.Count; i++)
        {
            int x = 540 + (i % 2) * 125;
            int y = 190 + (i / 2) * 120;
            Character enemy = enemies[i];

            using Brush shadow = new SolidBrush(Color.FromArgb(100, 0, 0, 0));
            g.FillEllipse(shadow, x - 2, y + 45, 56, 14);
            using Brush body = new SolidBrush(p.Hazard);
            g.FillEllipse(body, x, y, 52, 52);
            using Brush eye = new SolidBrush(Color.FromArgb(238, 220, 190));
            g.FillEllipse(eye, x + 13, y + 19, 7, 7);
            g.FillEllipse(eye, x + 32, y + 19, 7, 7);

            g.DrawString(enemy.Name, new Font(FontFamily.GenericMonospace, 10), Brushes.White, x - 10, y + 62);
            g.DrawString($"{enemy.HP}/{enemy.MAXHP}", new Font(FontFamily.GenericMonospace, 9), Brushes.Gainsboro, x - 10, y + 77);
        }
    }

    private static void DrawCommands(Graphics g, WorldPresentationProfile p, BattleCommand selected)
    {
        using Brush panel = new SolidBrush(Color.FromArgb(230, 13, 16, 20));
        g.FillRectangle(panel, 35, 500, 340, 155);
        using Pen border = new(p.WallHighlight, 1);
        g.DrawRectangle(border, 35, 500, 340, 155);

        BattleCommand[] commands = { BattleCommand.Attack, BattleCommand.Skill, BattleCommand.Item, BattleCommand.Defend, BattleCommand.Run };
        for (int i = 0; i < commands.Length; i++)
        {
            Color c = commands[i] == selected ? p.Accent : Color.FromArgb(190, 200, 200, 205);
            string prefix = commands[i] == selected ? "▶ " : "  ";
            g.DrawString(prefix + commands[i].ToString().ToUpperInvariant(), new Font(FontFamily.GenericMonospace, 13), new SolidBrush(c), 55, 515 + i * 25);
        }
    }

    private static void DrawDescription(Graphics g, WorldPresentationProfile p, string message)
    {
        using Brush panel = new SolidBrush(Color.FromArgb(230, 13, 16, 20));
        g.FillRectangle(panel, 395, 500, 415, 155);
        using Pen border = new(p.WallHighlight, 1);
        g.DrawRectangle(border, 395, 500, 415, 155);

        g.DrawString("ACTION", new Font(FontFamily.GenericMonospace, 11, FontStyle.Bold), new SolidBrush(p.Accent), 415, 515);
        string text = string.IsNullOrWhiteSpace(message) ? "Choose an action." : message;
        g.DrawString(text, new Font(FontFamily.GenericMonospace, 12), Brushes.White,
            new RectangleF(415, 545, 375, 90));
    }

    private static void DrawEnemyTracker(Graphics g, WorldPresentationProfile p, GameWorld world, Character? current)
    {
        using Brush panel = new SolidBrush(Color.FromArgb(220, 12, 15, 18));
        g.FillRectangle(panel, 835, 95, 240, 370);
        using Pen border = new(p.WallHighlight, 1);
        g.DrawRectangle(border, 835, 95, 240, 370);
        g.DrawString("HOSTILE CONTACTS", new Font(FontFamily.GenericMonospace, 11, FontStyle.Bold), Brushes.White, 850, 112);

        int y = 145;
        foreach (Character enemy in world.Enemies.Take(6))
        {
            bool selected = enemy == current;
            g.DrawString(selected ? "◆" : "◇", new Font(FontFamily.GenericMonospace, 11), new SolidBrush(selected ? p.Hazard : Color.Gray), 850, y);
            g.DrawString(enemy.Name, new Font(FontFamily.GenericMonospace, 10), Brushes.White, 870, y);
            g.DrawString($"HP {enemy.HP}/{enemy.MAXHP}", new Font(FontFamily.GenericMonospace, 9), Brushes.Gainsboro, 870, y + 18);
            y += 48;
        }
    }

    public void Dispose()
    {
        titleFont.Dispose();
        uiFont.Dispose();
        smallFont.Dispose();
    }
}
