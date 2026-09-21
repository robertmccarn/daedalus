using System.Drawing;
using Systemic.Engine.State;

public sealed class StructureRenderer
{
    public void Draw(Graphics g, ExplorationRenderContext context)
    {
        WorldPresentationProfile p = context.Profile;
        foreach (StaticUnit unit in context.World.StaticUnits.OrderBy(unit => unit.Y).ThenBy(unit => unit.X))
        {
            GridPosition pos = new(unit.X, unit.Y);
            if (!context.IsDiscovered(unit.X, unit.Y) || !context.Visible(pos)) continue;
            DrawStaticUnit(g, context, unit, p);
        }

        foreach (InteractiveProp prop in context.World.Props.OrderBy(prop => prop.Y).ThenBy(prop => prop.X))
        {
            GridPosition pos = new(prop.X, prop.Y);
            if (!context.IsDiscovered(prop.X, prop.Y) || !context.Visible(pos)) continue;
            DrawInteractiveProp(g, context, prop, p);
        }
    }

    private static void DrawStaticUnit(Graphics g, ExplorationRenderContext context, StaticUnit unit, WorldPresentationProfile p)
    {
        Point s = context.ToScreen(new GridPosition(unit.X, unit.Y));
        int lift = context.World.Dungeon[unit.Y, unit.X].Elevation * 4;

        using Brush shadow = new SolidBrush(Color.FromArgb(85, 0, 0, 0));
        g.FillEllipse(shadow, s.X + 2, s.Y + 19, 25, 8);

        Color baseColor = unit.NodeType switch
        {
            DungeonNodeType.Terminal => p.Accent,
            DungeonNodeType.Extraction => p.WarmLight,
            _ => p.WallHighlight
        };

        using Brush pedestal = new SolidBrush(Color.FromArgb(175, baseColor.R, baseColor.G, baseColor.B));
        g.FillRectangle(pedestal, s.X + 4, s.Y + 17 - lift, 20, 7);

        using Pen edge = new(Color.FromArgb(150, p.Void.R, p.Void.G, p.Void.B), 1);
        g.DrawRectangle(edge, s.X + 4, s.Y + 17 - lift, 20, 7);

        if (unit.IsActivated)
        {
            using Pen active = new(Color.FromArgb(175, baseColor.R, baseColor.G, baseColor.B), 2);
            g.DrawEllipse(active, s.X + 2, s.Y + 2 - lift, 24, 24);
        }
    }

    private static void DrawInteractiveProp(Graphics g, ExplorationRenderContext context, InteractiveProp prop, WorldPresentationProfile p)
    {
        Point s = context.ToScreen(new GridPosition(prop.X, prop.Y));
        int lift = context.World.Dungeon[prop.Y, prop.X].Elevation * 4;

        using Brush shadow = new SolidBrush(Color.FromArgb(100, 0, 0, 0));
        g.FillEllipse(shadow, s.X + 3, s.Y + 20, 23, 7);

        switch (prop)
        {
            case Chest chest:
            {
                DrawChest(g, s, lift, p, chest.IsOpen);
                break;
            }
            case Terminal terminal:
            {
                DrawTerminal(g, s, lift, p, terminal.IsActivated);
                break;
            }
            default:
            {
                using Brush body = new SolidBrush(p.WallHighlight);
                g.FillRectangle(body, s.X + 7, s.Y + 8 - lift, 14, 12);
                using Pen outline = new(p.Void, 2);
                g.DrawRectangle(outline, s.X + 7, s.Y + 8 - lift, 14, 12);
                break;
            }
        }
    }

    private static void DrawChest(Graphics g, Point s, int lift, WorldPresentationProfile p, bool isOpen)
    {
        using Brush wood = new SolidBrush(Color.FromArgb(125, 94, 63));
        g.FillRectangle(wood, s.X + 5, s.Y + 11 - lift, 18, 12);
        using Brush lid = new SolidBrush(Color.FromArgb(159, 121, 78));

        if (isOpen)
        {
            g.FillRectangle(lid, s.X + 5, s.Y + 4 - lift, 18, 7);
            g.FillRectangle(wood, s.X + 6, s.Y + 8 - lift, 16, 5);
        }
        else
        {
            g.FillRectangle(lid, s.X + 5, s.Y + 7 - lift, 18, 6);
        }

        using Pen metal = new(Color.FromArgb(210, p.WarmLight.R, p.WarmLight.G, p.WarmLight.B), 2);
        g.DrawLine(metal, s.X + 14, s.Y + 8 - lift, s.X + 14, s.Y + 20 - lift);

        if (!isOpen)
        {
            using Brush lockBrush = new SolidBrush(p.WarmLight);
            g.FillRectangle(lockBrush, s.X + 12, s.Y + 13 - lift, 5, 5);
        }
    }

    private static void DrawTerminal(Graphics g, Point s, int lift, WorldPresentationProfile p, bool isActivated)
    {
        Color accent = p.Accent;
        using Brush glow = new SolidBrush(Color.FromArgb(isActivated ? 70 : 38, accent.R, accent.G, accent.B));
        g.FillEllipse(glow, s.X - 4, s.Y + 2 - lift, 34, 34);
        using Brush housing = new SolidBrush(Color.FromArgb(74, 79, 78));
        g.FillRectangle(housing, s.X + 7, s.Y + 7 - lift, 14, 16);
        using Brush screen = new SolidBrush(Color.FromArgb(175, accent.R, accent.G, accent.B));
        g.FillRectangle(screen, s.X + 9, s.Y + 9 - lift, 10, 7);
        using Pen frame = new(Color.FromArgb(205, p.WallHighlight.R, p.WallHighlight.G, p.WallHighlight.B), 1);
        g.DrawRectangle(frame, s.X + 7, s.Y + 7 - lift, 14, 16);
        using Pen signal = new(Color.FromArgb(isActivated ? 230 : 110, accent.R, accent.G, accent.B), 2);
        g.DrawLine(signal, s.X + 14, s.Y + 3 - lift, s.X + 14, s.Y + 7 - lift);

        if (isActivated)
        {
            using Pen ring = new(Color.FromArgb(170, accent.R, accent.G, accent.B), 1);
            g.DrawEllipse(ring, s.X + 2, s.Y + 2 - lift, 24, 24);
        }
    }
}
