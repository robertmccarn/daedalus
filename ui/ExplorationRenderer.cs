using System.Drawing;
using Systemic.Engine.State;

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
        PartyController party,
        ExpeditionState expedition,
        Func<int, int, bool> isCellDiscovered)
    {
        worldRenderer.Draw(graphics, world, party, expedition, isCellDiscovered);
        hudRenderer.Draw(graphics, party, expedition);
    }

    public void Dispose()
    {
        hudRenderer.Dispose();
    }
}
