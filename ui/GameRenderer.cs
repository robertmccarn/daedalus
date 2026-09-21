using System.Drawing;
using Systemic.Engine.State;

public class GameRenderer : IDisposable
{
    private readonly ExplorationRenderer explorationRenderer;
    private readonly BattleRenderer battleRenderer;
    private readonly ExtractionResultsRenderer extractionResultsRenderer;
    private readonly CampaignRenderer campaignRenderer;
    private readonly GameOverRenderer gameOverRenderer;

    public GameRenderer(GameWorld world)
    {
        explorationRenderer = new ExplorationRenderer(world);
        battleRenderer = new BattleRenderer();
        extractionResultsRenderer = new ExtractionResultsRenderer();
        campaignRenderer = new CampaignRenderer();
        gameOverRenderer = new GameOverRenderer();
    }

    public void Draw(
        Graphics graphics,
        GameWorld world,
        PartyController party,
        ExpeditionState expedition,
        GameState gameState,
        BattleSystem? battle,
        ExtractionSummary? extraction,
        FeedbackEffect? feedback,
        string message,
        string currentObjective,
        Func<int, int, bool> isCellDiscovered,
        Func<int, int, bool> isCellVisible,
        GameStateManager stateManager)
    {
        switch (gameState)
        {
            case GameState.Battle when battle != null:
                battleRenderer.Draw(graphics, world, party, expedition, battle, message);
                return;
            case GameState.ExtractionResults:
                extractionResultsRenderer.Draw(graphics, extraction, message);
                return;
            case GameState.Campaign:
                campaignRenderer.Draw(graphics, stateManager, message);
                return;
            case GameState.GameOver:
                gameOverRenderer.Draw(graphics, message);
                return;
            default:
                explorationRenderer.Draw(
                    graphics, world, party, expedition,
                    isCellDiscovered, isCellVisible,
                    currentObjective, message, feedback);
                return;
        }
    }

    public void Dispose()
    {
        explorationRenderer.Dispose();
        battleRenderer.Dispose();
        extractionResultsRenderer.Dispose();
        campaignRenderer.Dispose();
        gameOverRenderer.Dispose();
    }
}
