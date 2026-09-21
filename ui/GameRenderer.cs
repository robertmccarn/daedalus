using System.Drawing;
using Systemic.Engine.State;

public class GameRenderer : IDisposable
{
    private readonly ExplorationRenderer explorationRenderer;
    private readonly BattleRenderer battleRenderer;
    private readonly GameOverRenderer gameOverRenderer;

    public GameRenderer(GameWorld world)
    {
        explorationRenderer = new ExplorationRenderer(world);
        battleRenderer = new BattleRenderer();
        gameOverRenderer = new GameOverRenderer();
    }

    public void Draw(
        Graphics graphics,
        GameWorld world,
        PartyController party,
        ExpeditionState expedition,
        GameState gameState,
        BattleSystem? battle,
        string message,
        string currentObjective,
        Func<int, int, bool> isCellDiscovered,
        Func<int, int, bool> isCellVisible)
    {
        if (gameState == GameState.Battle && battle != null)
        {
            battleRenderer.Draw(graphics, world, party, expedition, battle, message);
            return;
        }

        if (gameState == GameState.GameOver)
        {
            gameOverRenderer.Draw(graphics, message);
            return;
        }

        explorationRenderer.Draw(
            graphics, world, party, expedition,
            isCellDiscovered, isCellVisible,
            currentObjective, message);
    }

    public void Dispose()
    {
        explorationRenderer.Dispose();
        battleRenderer.Dispose();
        gameOverRenderer.Dispose();
    }
}
