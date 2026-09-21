using System.Drawing;
using Systemic.Engine.State;

public class ExplorationRenderer : IDisposable
{
    private readonly BackdropRenderer backdropRenderer = new();
    private readonly WorldRenderer worldRenderer = new();
    private readonly RuinFeatureRenderer ruinFeatureRenderer = new();
    private readonly StructureRenderer structureRenderer = new();
    private readonly EntityRenderer entityRenderer = new();
    private readonly ForegroundRenderer foregroundRenderer = new();
    private readonly EffectRenderer effectRenderer = new();
    private readonly ExplorationHudRenderer hudRenderer = new();

    public ExplorationRenderer(GameWorld world) { }

    public void Draw(
        Graphics graphics,
        GameWorld world,
        PartyController party,
        ExpeditionState expedition,
        Func<int, int, bool> isCellDiscovered,
        Func<int, int, bool> isCellVisible,
        string currentObjective,
        string message,
        FeedbackEffect? feedback)
    {
        ViewportLayout layout = ViewportLayout.ForClientSize(
            (int)Math.Max(1, graphics.VisibleClipBounds.Width),
            (int)Math.Max(1, graphics.VisibleClipBounds.Height));

        ExplorationRenderContext context = new(
            world, party, expedition, isCellDiscovered, isCellVisible,
            currentObjective, message, feedback, layout);

        List<RenderItem> renderItems =
        new()
        {
            new(RenderPass.Backdrop, 0, 0, g => backdropRenderer.Draw(g, context)),
            new(RenderPass.Terrain, 0, 1, g => worldRenderer.Draw(g, context)),
            new(RenderPass.Structures, 0, 2, g => ruinFeatureRenderer.Draw(g, context)),
            new(RenderPass.Structures, 0, 3, g => structureRenderer.Draw(g, context)),
            new(RenderPass.Entities, 0, 4, g => entityRenderer.Draw(g, context)),
            new(RenderPass.Foreground, 0, 5, g => foregroundRenderer.Draw(g, context)),
            new(RenderPass.Effects, 0, 6, g => effectRenderer.Draw(g, context)),
            new(RenderPass.Hud, 0, 7, g => hudRenderer.Draw(g, context))
        };

        foreach (RenderItem item in renderItems
                     .OrderBy(item => item.Pass)
                     .ThenBy(item => item.Depth)
                     .ThenBy(item => item.StableOrder))
            item.Draw(graphics);
    }

    public void Dispose() => hudRenderer.Dispose();
}
