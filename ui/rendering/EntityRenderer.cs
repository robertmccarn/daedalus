using System.Drawing;

public sealed class EntityRenderer
{
    public void Draw(Graphics g, ExplorationRenderContext context)
    {
        foreach (PartyRenderData member in context.Party.GetRenderData(context.Expedition)
                     .OrderBy(x => x.Position.Y)
                     .ThenBy(x => x.Position.X)
                     .ThenBy(x => x.PartySlot))
        {
            if (!context.IsDiscovered(member.Position.X, member.Position.Y) || !context.Visible(member.Position))
                continue;

            DrawPartyMember(g, context, member);
        }

        foreach (Character enemy in context.World.Enemies.OrderBy(x => x.Y).ThenBy(x => x.X))
        {
            GridPosition pos = new(enemy.X, enemy.Y);
            if (!context.IsDiscovered(enemy.X, enemy.Y) || !context.Visible(pos))
                continue;
            DrawEnemy(g, context, enemy);
        }
    }

    private static void DrawPartyMember(Graphics g, ExplorationRenderContext c, PartyRenderData member)
    {
        Point s = c.ToScreen(member.Position);
        Color accent = member.IsLeader ? c.Profile.Accent : member.PartySlot switch
        {
            1 => Color.FromArgb(117, 190, 151),
            2 => c.Profile.WarmLight,
            _ => Color.FromArgb(166, 132, 191)
        };

        using Brush shadow = new SolidBrush(Color.FromArgb(100, 0, 0, 0));
        g.FillEllipse(shadow, s.X + 3, s.Y + 20, 22, 7);

        using Brush cloak = new SolidBrush(accent);
        Point[] body =
        {
            new(s.X + 14, s.Y + 3),
            new(s.X + 22, s.Y + 10),
            new(s.X + 19, s.Y + 24),
            new(s.X + 8, s.Y + 24),
            new(s.X + 5, s.Y + 10)
        };
        g.FillPolygon(cloak, body);

        using Brush skin = new SolidBrush(Color.FromArgb(196, 164, 139));
        g.FillEllipse(skin, s.X + 9, s.Y + 1, 10, 10);

        using Pen outline = new(Color.FromArgb(235, c.Profile.Void.R, c.Profile.Void.G, c.Profile.Void.B), 1);
        g.DrawPolygon(outline, body);

        if (member.IsLeader)
        {
            using Pen marker = new(Color.FromArgb(210, c.Profile.Accent.R, c.Profile.Accent.G, c.Profile.Accent.B), 2);
            g.DrawEllipse(marker, s.X + 2, s.Y, 24, 26);
        }
    }

    private static void DrawEnemy(Graphics g, ExplorationRenderContext c, Character enemy)
    {
        Point s = c.ToScreen(new GridPosition(enemy.X, enemy.Y));
        using Brush shadow = new SolidBrush(Color.FromArgb(100, 0, 0, 0));
        g.FillEllipse(shadow, s.X + 3, s.Y + 20, 22, 7);

        using Brush body = new SolidBrush(c.Profile.Hazard);
        g.FillEllipse(body, s.X + 5, s.Y + 4, 18, 18);
        using Pen outline = new(c.Profile.Void, 2);
        g.DrawEllipse(outline, s.X + 5, s.Y + 4, 18, 18);

        using Brush eye = new SolidBrush(Color.FromArgb(245, 220, 185));
        g.FillEllipse(eye, s.X + 9, s.Y + 9, 3, 3);
        g.FillEllipse(eye, s.X + 16, s.Y + 9, 3, 3);
    }
}
