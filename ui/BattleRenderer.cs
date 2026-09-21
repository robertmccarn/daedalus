using System.Drawing;
using System.Drawing.Drawing2D;
using Systemic.Engine.State;

public class BattleRenderer : IDisposable
{
    private readonly Font titleFont = new(FontFamily.GenericMonospace, 26, FontStyle.Bold);
    private readonly Font uiFont = new(FontFamily.GenericMonospace, 14);
    private readonly Font smallFont = new(FontFamily.GenericMonospace, 11);
    private readonly Font microFont = new(FontFamily.GenericMonospace, 9);

    public void Draw(
        Graphics graphics,
        GameWorld world,
        PartyController party,
        ExpeditionState expedition,
        BattleSystem battle,
        string message)
    {
        BiomeType biome = BiomeCatalog.ForFloor(world.Floor);
        WorldPresentationProfile p = WorldPresentationProfile.ForBiome(biome);

        graphics.Clear(Color.FromArgb(8, 10, 12));

        using LinearGradientBrush bg = new(
            new Rectangle(0, 0, 1100, 700),
            p.Void,
            Color.FromArgb(42, 34, 35),
            LinearGradientMode.Vertical);
        graphics.FillRectangle(bg, 0, 0, 1100, 700);

        DrawArena(graphics, p);
        graphics.DrawString("TACTICAL CONTACT", titleFont, Brushes.White, 45, 30);
        graphics.DrawString(BiomeCatalog.Name(biome), smallFont, new SolidBrush(p.Accent), 48, 65);

        DrawParty(graphics, p, battle);
        DrawEnemies(graphics, p, battle);

        DrawTurnOrder(graphics, p, battle);
        DrawCommands(graphics, p, battle.SelectedCommand);
        DrawDescription(graphics, p, battle);

        _ = party;
        _ = expedition;
    }

    private static void DrawArena(Graphics g, WorldPresentationProfile p)
    {
        using Brush floor = new SolidBrush(Color.FromArgb(92, p.Floor.R, p.Floor.G, p.Floor.B));
        g.FillRectangle(floor, 60, 110, 750, 360);

        using Pen grid = new(Color.FromArgb(32, p.WallHighlight.R, p.WallHighlight.G, p.WallHighlight.B), 1);
        for (int x = 60; x <= 810; x += 38)
            g.DrawLine(grid, x, 110, x, 470);
        for (int y = 110; y <= 470; y += 38)
            g.DrawLine(grid, 60, y, 810, y);

        using Pen border = new(Color.FromArgb(160, p.WallHighlight.R, p.WallHighlight.G, p.WallHighlight.B), 2);
        g.DrawRectangle(border, 60, 110, 750, 360);

        using Brush glow = new SolidBrush(Color.FromArgb(26, p.Accent.R, p.Accent.G, p.Accent.B));
        g.FillEllipse(glow, 250, 150, 320, 270);
    }

    private void DrawParty(Graphics g, WorldPresentationProfile p, BattleSystem battle)
    {
        int index = 0;
        foreach (PartyMember member in battle.Party.Take(4))
        {
            int x = 120 + (index % 2) * 145;
            int y = 185 + (index / 2) * 120;
            bool selected = battle.SelectedActor?.Id == member.Id;

            DrawActor(g, p, member.SpriteId, member.Name, member.HP, member.MaxHP, x, y, selected, member.Id);
            index++;
        }
    }

    private void DrawEnemies(Graphics g, WorldPresentationProfile p, BattleSystem battle)
    {
        int index = 0;
        foreach (Character enemy in battle.Enemies.Take(3))
        {
            int x = 515 + (index % 2) * 120;
            int y = 190 + (index / 2) * 120;
            bool selected = battle.SelectedTarget == enemy;

            using Brush shadow = new SolidBrush(Color.FromArgb(100, 0, 0, 0));
            g.FillEllipse(shadow, x - 3, y + 48, 58, 13);

            using Brush body = new SolidBrush(
                enemy.HP > 0 ? p.Hazard : Color.FromArgb(65, 60, 60));
            Point[] silhouette =
            {
                new Point(x + 25, y),
                new Point(x + 42, y + 10),
                new Point(x + 48, y + 31),
                new Point(x + 37, y + 48),
                new Point(x + 13, y + 48),
                new Point(x + 2, y + 31),
                new Point(x + 8, y + 10)
            };
            g.FillPolygon(body, silhouette);

            using Brush eye = new SolidBrush(Color.FromArgb(235, 220, 185));
            if (enemy.HP > 0)
            {
                g.FillEllipse(eye, x + 14, y + 22, 6, 6);
                g.FillEllipse(eye, x + 32, y + 22, 6, 6);
            }

            if (selected && enemy.HP > 0)
            {
                using Pen marker = new(Color.FromArgb(220, p.Accent.R, p.Accent.G, p.Accent.B), 2);
                g.DrawEllipse(marker, x - 6, y - 6, 60, 60);
            }

            g.DrawString(enemy.Name, smallFont, Brushes.White, x - 12, y + 54);
            g.DrawString($"HP {enemy.HP}/{enemy.MAXHP}", microFont, Brushes.Gainsboro, x - 12, y + 68);
            if (enemy.HP > 0 && battle.HasStatus(enemy, "Poisoned"))
                g.DrawString("POISON", microFont, new SolidBrush(p.Hazard), x - 12, y + 81);
            index++;
        }
    }

    private void DrawActor(
        Graphics g,
        WorldPresentationProfile p,
        string spriteId,
        string name,
        int hp,
        int maxHp,
        int x,
        int y,
        bool selected,
        string id)
    {
        using Brush shadow = new SolidBrush(Color.FromArgb(100, 0, 0, 0));
        g.FillEllipse(shadow, x - 3, y + 46, 58, 13);

        Color accent = id == "arden"
            ? p.Accent
            : spriteId.ToLowerInvariant() switch
            {
                "lyra" => Color.FromArgb(116, 176, 170),
                "marek" => Color.FromArgb(173, 132, 89),
                "sera" => Color.FromArgb(154, 120, 177),
                _ => Color.FromArgb(122, 152, 157)
            };

        using Brush cloak = new SolidBrush(hp > 0 ? accent : Color.FromArgb(55, 60, 62));
        Point[] body =
        {
            new Point(x + 8, y + 13), new Point(x + 18, y + 5),
            new Point(x + 40, y + 5), new Point(x + 50, y + 13),
            new Point(x + 42, y + 49), new Point(x + 15, y + 49)
        };
        g.FillPolygon(cloak, body);

        using Brush face = new SolidBrush(Color.FromArgb(192, 162, 138));
        g.FillEllipse(face, x + 20, y - 7, 18, 19);

        using Pen gear = new(Color.FromArgb(205, 210, 207, 195), 2);
        if (spriteId.Equals("marek", StringComparison.OrdinalIgnoreCase))
            g.DrawLine(gear, x + 44, y + 28, x + 57, y + 16);
        else if (spriteId.Equals("lyra", StringComparison.OrdinalIgnoreCase))
            g.DrawLine(gear, x + 7, y + 10, x + 7, y + 48);

        using Brush hpBack = new SolidBrush(Color.FromArgb(80, 20, 20, 20));
        g.FillRectangle(hpBack, x, y + 58, 60, 6);
        using Brush hpFill = new SolidBrush(
            hp > 0 ? Color.FromArgb(195, 78, 165, 95) : Color.FromArgb(160, 72, 72, 72));
        int width = maxHp <= 0 ? 0 : 60 * hp / maxHp;
        g.FillRectangle(hpFill, x, y + 58, width, 6);

        g.DrawString(name, smallFont, Brushes.White, x - 4, y + 69);
        g.DrawString($"HP {hp}/{maxHp}", microFont, Brushes.Gainsboro, x - 4, y + 83);

        if (selected)
        {
            using Pen marker = new(Color.FromArgb(220, p.Accent.R, p.Accent.G, p.Accent.B), 2);
            g.DrawEllipse(marker, x - 7, y - 10, 64, 76);
        }
    }

    private void DrawTurnOrder(Graphics g, WorldPresentationProfile p, BattleSystem battle)
    {
        using Brush panel = new SolidBrush(Color.FromArgb(220, 11, 14, 18));
        g.FillRectangle(panel, 835, 95, 240, 110);

        using Pen border = new(Color.FromArgb(160, p.WallHighlight.R, p.WallHighlight.G, p.WallHighlight.B), 1);
        g.DrawRectangle(border, 835, 95, 240, 110);

        g.DrawString("TURN", smallFont, Brushes.White, 850, 110);
        using Brush activeBrush = new SolidBrush(p.Accent);
        g.DrawString(
            battle.SelectedActor == null ? "NO ACTIVE PARTY MEMBER" : $"ACTIVE  {battle.SelectedActor.Name}",
            microFont,
            activeBrush,
            850,
            132);

        g.DrawString(
            $"ROUND INDEX {battle.State.TurnIndex + 1:00}",
            microFont,
            Brushes.Gainsboro,
            850,
            152);

        g.DrawString(
            $"TARGET  {battle.SelectedTarget?.Name ?? "NONE"}",
            microFont,
            battle.SelectedTarget == null ? Brushes.Gray : Brushes.White,
            850,
            172);
    }

    private void DrawCommands(Graphics g, WorldPresentationProfile p, BattleCommand selected)
    {
        using Brush panel = new SolidBrush(Color.FromArgb(230, 13, 16, 20));
        g.FillRectangle(panel, 35, 500, 340, 155);

        using Pen border = new(p.WallHighlight, 1);
        g.DrawRectangle(border, 35, 500, 340, 155);

        BattleCommand[] commands =
        {
            BattleCommand.Attack,
            BattleCommand.Skill,
            BattleCommand.Item,
            BattleCommand.Interact,
            BattleCommand.Defend,
            BattleCommand.Run
        };

        for (int i = 0; i < commands.Length; i++)
        {
            Color c = commands[i] == selected ? p.Accent : Color.FromArgb(190, 200, 200, 205);
            string prefix = commands[i] == selected ? "▶ " : "  ";
            using Brush commandBrush = new SolidBrush(c);
            g.DrawString(prefix + commands[i].ToString().ToUpperInvariant(), smallFont, commandBrush, 55, 508 + i * 23);
        }
    }

    private void DrawDescription(Graphics g, WorldPresentationProfile p, BattleSystem battle)
    {
        using Brush panel = new SolidBrush(Color.FromArgb(230, 13, 16, 20));
        g.FillRectangle(panel, 395, 500, 415, 155);

        using Pen border = new(p.WallHighlight, 1);
        g.DrawRectangle(border, 395, 500, 415, 155);

        using Brush actionBrush = new SolidBrush(p.Accent);
        g.DrawString("ACTION", smallFont, actionBrush, 415, 515);
        g.DrawString(
            battle.CommandMessage,
            microFont,
            Brushes.White,
            new RectangleF(415, 545, 375, 82));
    }

    public void Dispose()
    {
        titleFont.Dispose();
        uiFont.Dispose();
        smallFont.Dispose();
        microFont.Dispose();
    }
}
