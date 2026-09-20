using System.Drawing;

public class ExplorationRenderer : IDisposable
{
    private readonly TopDownWorldRenderer worldRenderer = new();
    private readonly ExplorationHudRenderer hudRenderer = new();

    public ExplorationRenderer(GameWorld world)
    {
    }

    public void Draw(
        Graphics graphics,
        GameWorld world,
        Func<int, int, bool> isCellDiscovered)
    {
        worldRenderer.Draw(graphics, world, isCellDiscovered);
        hudRenderer.Draw(graphics, world);
    }

    public void Dispose()
    {
        hudRenderer.Dispose();
    }
}
