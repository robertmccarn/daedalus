using System.Drawing;
using Systemic.Engine.State;

public class ExplorationRenderer : IDisposable
{
    private readonly BackdropRenderer backdropRenderer = new();
    private readonly WorldRenderer worldRenderer = new();
    private readonly StructureRenderer structureRenderer = new();
    private readonly EntityRenderer entityRenderer = new();
    private readonly ForegroundRenderer foregroundRenderer = new();
    private readonly EffectRenderer effectRenderer = new();
    private readonly ExplorationHudRenderer hudRenderer = new();

    public ExplorationRenderer(GameWorld world)
    {
    }

    public void Draw(
        Graphics graphics,
        GameWorld world,
        PartyController party,
        ExpeditionState expedition,
        Func<int, int, bool> isCellDiscovered,
        string currentObjective,
        string message)
    {
        ExplorationRenderContext context = new(
            world,
            party,
            expedition,
            isCellDiscovered,
            currentObjective,
            message);

        backdropRenderer.Draw(graphics, context);
        worldRenderer.Draw(graphics, context);
        structureRenderer.Draw(graphics, context);
        entityRenderer.Draw(graphics, context);
        effectRenderer.Draw(graphics, context);
        foregroundRenderer.Draw(graphics, context);
        hudRenderer.Draw(graphics, context);
    }

    public void Dispose() => hudRenderer.Dispose();
}
