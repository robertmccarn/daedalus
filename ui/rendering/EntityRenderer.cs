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
            if (!context.IsDiscovered(member.Position.X, member.Position.Y) ||
                !context.IsVisible(member.Position.X, member.Position.Y) ||
                !context.Visible(member.Position))
                continue;

            DrawPartyMember(g, context, member);
        }

        foreach (Character enemy in context.World.Enemies.OrderBy(x => x.Y).ThenBy(x => x.X))
        {
            GridPosition pos = new(enemy.X, enemy.Y);
            if (!context.IsDiscovered(enemy.X, enemy.Y) || !context.IsVisible(enemy.X, enemy.Y) || !context.Visible(pos))
                continue;

            DrawEnemy(g, context, enemy);
        }
    }

    private static void DrawPartyMember(Graphics g, ExplorationRenderContext c, PartyRenderData member)
    {
        Point s = c.ToScreen(member.Position);
        int bob = member.AnimationState == CharacterAnimationState.Walk &&
                  member.AnimationTick % 2 == 1 ? -2 : 0;
        Point origin = new(s.X, s.Y + bob);

        Color accent = member.IsLeader
            ? c.Profile.Accent
            : member.SpriteId.ToLowerInvariant() switch
            {
                "lyra" => Color.FromArgb(116, 176, 170),
                "marek" => Color.FromArgb(173, 132, 89),
                "sera" => Color.FromArgb(154, 120, 177),
                _ => Color.FromArgb(122, 152, 157)
            };

        using Brush shadow = new SolidBrush(Color.FromArgb(105, 0, 0, 0));
        g.FillEllipse(shadow, origin.X + 2, origin.Y + 21, 25, 7);

        switch (member.SpriteId.ToLowerInvariant())
        {
            case "lyra":
                DrawLyra(g, c, origin, accent, member.Direction);
                break;
            case "marek":
                DrawMarek(g, c, origin, accent, member.Direction);
                break;
            case "sera":
                DrawSera(g, c, origin, accent, member.Direction);
                break;
            default:
                DrawArden(g, c, origin, accent, member.Direction);
                break;
        }

        if (member.IsLeader)
        {
            using Pen marker = new(Color.FromArgb(205, c.Profile.Accent.R, c.Profile.Accent.G, c.Profile.Accent.B), 2);
            g.DrawEllipse(marker, origin.X + 1, origin.Y - 1, 27, 28);
        }
    }

    private static void DrawArden(Graphics g, ExplorationRenderContext c, Point s, Color accent, CharacterDirection direction)
    {
        using Brush cloak = new SolidBrush(accent);
        using Brush darkCloak = new SolidBrush(Color.FromArgb(
            Math.Max(0, accent.R - 28), Math.Max(0, accent.G - 28), Math.Max(0, accent.B - 28)));

        Point[] body =
        {
            new Point(s.X + 4, s.Y + 10), new Point(s.X + 8, s.Y + 7),
            new Point(s.X + 20, s.Y + 7), new Point(s.X + 24, s.Y + 11),
            new Point(s.X + 21, s.Y + 24), new Point(s.X + 7, s.Y + 24)
        };
        g.FillPolygon(cloak, body);
        g.FillRectangle(darkCloak, s.X + 10, s.Y + 13, 7, 11);
        DrawHead(g, c, s, direction, 8);

        using Pen gear = new(Color.FromArgb(205, 210, 207, 195), 2);
        if (direction == CharacterDirection.Left)
            g.DrawLine(gear, s.X + 5, s.Y + 14, s.X + 1, s.Y + 18);
        else
            g.DrawLine(gear, s.X + 21, s.Y + 14, s.X + 26, s.Y + 18);
    }

    private static void DrawLyra(Graphics g, ExplorationRenderContext c, Point s, Color accent, CharacterDirection direction)
    {
        using Brush robe = new SolidBrush(accent);
        Point[] body =
        {
            new Point(s.X + 13, s.Y + 8), new Point(s.X + 20, s.Y + 14),
            new Point(s.X + 23, s.Y + 24), new Point(s.X + 5, s.Y + 24),
            new Point(s.X + 8, s.Y + 14)
        };
        g.FillPolygon(robe, body);

        using Brush hood = new SolidBrush(Color.FromArgb(80, 91, 101));
        g.FillEllipse(hood, s.X + 7, s.Y + 1, 13, 12);
        DrawHead(g, c, s, direction, 6);

        using Pen staff = new(Color.FromArgb(215, c.Profile.WarmLight.R, c.Profile.WarmLight.G, c.Profile.WarmLight.B), 2);
        int staffX = direction == CharacterDirection.Left ? s.X + 4 : s.X + 22;
        g.DrawLine(staff, staffX, s.Y + 6, staffX, s.Y + 26);
        using Brush gem = new SolidBrush(Color.FromArgb(220, c.Profile.Accent.R, c.Profile.Accent.G, c.Profile.Accent.B));
        g.FillEllipse(gem, staffX - 3, s.Y + 3, 6, 6);
    }

    private static void DrawMarek(Graphics g, ExplorationRenderContext c, Point s, Color accent, CharacterDirection direction)
    {
        using Brush armor = new SolidBrush(Color.FromArgb(
            Math.Min(255, accent.R + 18), Math.Min(255, accent.G + 13), Math.Min(255, accent.B + 5)));

        g.FillRectangle(armor, s.X + 4, s.Y + 9, 20, 16);
        g.FillRectangle(armor, s.X + 1, s.Y + 11, 5, 10);
        g.FillRectangle(armor, s.X + 22, s.Y + 11, 5, 10);

        using Pen plates = new(Color.FromArgb(125, c.Profile.WallHighlight.R, c.Profile.WallHighlight.G, c.Profile.WallHighlight.B), 1);
        g.DrawLine(plates, s.X + 8, s.Y + 15, s.X + 20, s.Y + 15);
        g.DrawLine(plates, s.X + 13, s.Y + 10, s.X + 13, s.Y + 24);

        using Brush helmet = new SolidBrush(Color.FromArgb(93, 93, 89));
        g.FillRectangle(helmet, s.X + 7, s.Y + 1, 14, 10);
        DrawHead(g, c, s, direction, 5);

        using Pen weapon = new(Color.FromArgb(210, 205, 193, 170), 3);
        int x1 = direction == CharacterDirection.Left ? s.X + 5 : s.X + 21;
        int x2 = direction == CharacterDirection.Left ? s.X - 1 : s.X + 27;
        g.DrawLine(weapon, x1, s.Y + 16, x2, s.Y + 9);
    }

    private static void DrawSera(Graphics g, ExplorationRenderContext c, Point s, Color accent, CharacterDirection direction)
    {
        using Brush cloak = new SolidBrush(accent);
        Point[] body =
        {
            new Point(s.X + 13, s.Y + 7), new Point(s.X + 19, s.Y + 10),
            new Point(s.X + 21, s.Y + 24), new Point(s.X + 7, s.Y + 24),
            new Point(s.X + 9, s.Y + 10)
        };
        g.FillPolygon(cloak, body);

        using Brush sash = new SolidBrush(Color.FromArgb(205, c.Profile.WarmLight.R, c.Profile.WarmLight.G, c.Profile.WarmLight.B));
        g.FillRectangle(sash, s.X + 9, s.Y + 16, 12, 3);
        DrawHead(g, c, s, direction, 7);

        using Pen blade = new(Color.FromArgb(210, c.Profile.WallHighlight.R, c.Profile.WallHighlight.G, c.Profile.WallHighlight.B), 2);
        int bladeX = direction == CharacterDirection.Left ? s.X + 5 : s.X + 22;
        g.DrawLine(blade, bladeX, s.Y + 13, bladeX + (direction == CharacterDirection.Left ? -4 : 4), s.Y + 7);
    }

    private static void DrawHead(Graphics g, ExplorationRenderContext c, Point s, CharacterDirection direction, int radius)
    {
        int offsetX = direction switch
        {
            CharacterDirection.Left => -1,
            CharacterDirection.Right => 1,
            _ => 0
        };

        using Brush skin = new SolidBrush(Color.FromArgb(194, 163, 139));
        g.FillEllipse(skin, s.X + 14 - radius / 2 + offsetX, s.Y + 1, radius, radius);

        using Pen hair = new(Color.FromArgb(65, 60, 58), 2);
        if (direction == CharacterDirection.Up)
        {
            g.DrawLine(hair, s.X + 12, s.Y + 3, s.X + 19, s.Y + 3);
        }
        else
        {
            int eyeX = direction == CharacterDirection.Left ? s.X + 13 : s.X + 18;
            using Brush eye = new SolidBrush(Color.FromArgb(235, 225, 205));
            g.FillEllipse(eye, eyeX, s.Y + 4, 2, 2);
        }
    }

    private static void DrawEnemy(Graphics g, ExplorationRenderContext c, Character enemy)
    {
        Point s = c.ToScreen(new GridPosition(enemy.X, enemy.Y));
        using Brush shadow = new SolidBrush(Color.FromArgb(100, 0, 0, 0));
        g.FillEllipse(shadow, s.X + 3, s.Y + 20, 22, 7);

        using Brush body = new SolidBrush(c.Profile.Hazard);
        Point[] silhouette =
        {
            new Point(s.X + 6, s.Y + 7), new Point(s.X + 10, s.Y + 3),
            new Point(s.X + 14, s.Y + 6), new Point(s.X + 18, s.Y + 3),
            new Point(s.X + 23, s.Y + 7), new Point(s.X + 21, s.Y + 20),
            new Point(s.X + 17, s.Y + 24), new Point(s.X + 8, s.Y + 22)
        };
        g.FillPolygon(body, silhouette);

        using Pen outline = new(c.Profile.Void, 2);
        g.DrawPolygon(outline, silhouette);
        using Brush eye = new SolidBrush(Color.FromArgb(245, 220, 185));
        g.FillEllipse(eye, s.X + 9, s.Y + 10, 3, 3);
        g.FillEllipse(eye, s.X + 17, s.Y + 10, 3, 3);
    }
}
