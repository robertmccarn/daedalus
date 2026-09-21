using System.Drawing;
using Systemic.Engine.State;

public sealed class MinimapRenderer
{
    public void Draw(Graphics g, ExplorationRenderContext context, Rectangle bounds)
    {
        using Brush panel = new SolidBrush(Color.FromArgb(220, 12, 15, 18));
        g.FillRectangle(panel, bounds);
        using Pen border = new(Color.FromArgb(170, context.Profile.WallHighlight.R, context.Profile.WallHighlight.G, context.Profile.WallHighlight.B), 1);
        g.DrawRectangle(border, bounds);

        int cell = Math.Max(2, Math.Min(bounds.Width / GameWorld.Width, bounds.Height / GameWorld.Height));
        int ox = bounds.X + (bounds.Width - cell * context.World.Width) / 2;
        int oy = bounds.Y + (bounds.Height - cell * context.World.Height) / 2;

        for (int y = 0; y < context.World.Height; y++)
        {
            for (int x = 0; x < context.World.Width; x++)
            {
                if (!context.IsDiscovered(x, y))
                    continue;

                Tile tile = context.World.Dungeon[y, x];
                Color c = tile.IsWalkable ? context.Profile.Floor : context.Profile.Wall;
                using Brush b = new SolidBrush(c);
                g.FillRectangle(b, ox + x * cell, oy + y * cell, cell, cell);
            }
        }

        foreach (DungeonNode node in context.World.Nodes)
        {
            if (!context.IsDiscovered(node.X, node.Y))
                continue;
            Color c = node.Type switch
            {
                DungeonNodeType.Extraction => context.Profile.WarmLight,
                DungeonNodeType.Combat => context.Profile.Hazard,
                _ => context.Profile.Accent
            };
            using Brush b = new SolidBrush(c);
            g.FillRectangle(b, ox + node.X * cell, oy + node.Y * cell, Math.Max(2, cell), Math.Max(2, cell));
        }

        GridPosition leader = context.Party.LeaderPosition;
        using Brush marker = new SolidBrush(Color.White);
        g.FillRectangle(marker, ox + leader.X * cell, oy + leader.Y * cell, Math.Max(2, cell), Math.Max(2, cell));
    }
}
