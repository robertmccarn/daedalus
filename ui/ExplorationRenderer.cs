using System.Drawing;

public class ExplorationRenderer : IDisposable
{
    private readonly IsometricWorldRenderer worldRenderer = new();
    private readonly ExplorationHudRenderer hudRenderer = new();

    public ExplorationRenderer(GameWorld world)
    {
    }

    public void Draw(Graphics graphics, GameWorld world)
    {
        graphics.Clear(Color.Black);
        worldRenderer.Draw(graphics, world);
        hudRenderer.Draw(graphics, world);
    }

    public void Dispose()
    {
        worldRenderer.Dispose();
        hudRenderer.Dispose();
    }
}
