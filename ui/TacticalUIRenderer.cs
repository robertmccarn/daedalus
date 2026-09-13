using System.Drawing;

public class TacticalUIRenderer : IDisposable
{
    private readonly AtlasSlicer atlas = new();

    public void DrawBossPortrait(Graphics graphics, BossExpression expression, Rectangle bounds)
    {
        atlas.Draw(graphics, atlas.GetBossExpressionRegion(expression), bounds);
    }

    public void DrawTileHighlight(Graphics graphics, TacticalHighlight highlight, int tileCenterX, int tileCenterY)
    {
        atlas.Draw(graphics, atlas.GetTacticalHighlightRegion(highlight),
            new Rectangle(tileCenterX - 16, tileCenterY - 16, 32, 32));
    }

    public void DrawEnemyCard(
        Graphics graphics,
        AtlasUnit enemy,
        IReadOnlyList<bool> availableActions,
        Rectangle bounds)
    {
        atlas.Draw(graphics, atlas.GetEnemyCardPortraitRegion(enemy),
            new Rectangle(bounds.X, bounds.Y, 96, 138));

        for (int index = 0; index < 6; index++)
        {
            bool active = index < availableActions.Count && availableActions[index];
            int x = bounds.X + 104 + (index % 3) * 42;
            int y = bounds.Y + (index / 3) * 52;
            atlas.Draw(graphics, atlas.GetEnemyCardActionRegion(enemy, index, active),
                new Rectangle(x, y, 36, 45));
        }
    }

    public void Dispose()
    {
        atlas.Dispose();
    }
}
