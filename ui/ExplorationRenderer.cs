using System.Drawing;

public class ExplorationRenderer : IDisposable
{
    private readonly TopDownWorldRenderer worldRenderer = new();
    private readonly ExplorationHudRenderer hudRenderer = new();

    public ExplorationRenderer(GameWorld world)
    {
    }

    public void Draw(Graphics graphics, GameWorld world)
    {
        worldRenderer.Draw(graphics, world);
        hudRenderer.Draw(graphics, world);
    }

    public void Dispose()
    {
        hudRenderer.Dispose();
    }
}
