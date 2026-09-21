using System.Drawing;
using Systemic.Engine.State;

public sealed class StructureRenderer
{
    public void Draw(Graphics g, ExplorationRenderContext context)
    {
        WorldPresentationProfile p = context.Profile;
        foreach (StaticUnit unit in context.World.StaticUnits)
        {
            GridPosition pos = new(unit.X, unit.Y);
            if (!context.IsDiscovered(unit.X, unit.Y) || !context.Visible(pos))
                continue;

            Point s = context.ToScreen(pos);
            int lift = context.World.Dungeon[unit.Y, unit.X].Elevation * 4;

            using Brush shadow = new SolidBrush(Color.FromArgb(90, 0, 0, 0));
            g.FillEllipse(shadow, s.X + 3, s.Y + 18 - lift, 23, 8);

            Color c = unit.NodeType switch
            {
                DungeonNodeType.Terminal => p.Accent,
                DungeonNodeType.Extraction => p.WarmLight,
                _ => p.WallHighlight
            };

            using Brush body = new SolidBrush(c);
            g.FillRectangle(body, s.X + 6, s.Y + 5 - lift, 16, 18);

            using Pen edge = new(Color.FromArgb(220, p.Void.R, p.Void.G, p.Void.B), 2);
            g.DrawRectangle(edge, s.X + 6, s.Y + 5 - lift, 16, 18);

            if (unit.IsActivated)
            {
                using Pen active = new(Color.FromArgb(180, c.R, c.G, c.B), 2);
                g.DrawEllipse(active, s.X + 2, s.Y + 1 - lift, 24, 24);
            }
        }

        foreach (InteractiveProp prop in context.World.Props)
        {
            if (!context.IsDiscovered(prop.X, prop.Y) || !context.Visible(new GridPosition(prop.X, prop.Y)))
                continue;

            Point s = context.ToScreen(new GridPosition(prop.X, prop.Y));
            int lift = context.World.Dungeon[prop.Y, prop.X].Elevation * 4;

            Color c = prop switch
            {
                Chest => p.WarmLight,
                Terminal => p.Accent,
                _ => p.WallHighlight
            };

            using Brush b = new SolidBrush(c);
            g.FillRectangle(b, s.X + 7, s.Y + 8 - lift, 14, 12);
            using Pen outline = new(p.Void, 2);
            g.DrawRectangle(outline, s.X + 7, s.Y + 8 - lift, 14, 12);
        }
    }
}
