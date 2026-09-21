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
        Character? battleEnemy,
        string message,
        BattleCommand selectedCommand,
        Func<int, int, bool> isCellDiscovered)
    {
        if (gameState == GameState.Battle)
        {
            battleRenderer.Draw(
                graphics,
                world,
                party,
                expedition,
                battleEnemy,
                message,
                selectedCommand);
            return;
        }

        if (gameState == GameState.GameOver)
        {
            gameOverRenderer.Draw(graphics, message);
            return;
        }

        explorationRenderer.Draw(
            graphics,
            world,
            party,
            expedition,
            isCellDiscovered);
    }

    public void Dispose()
    {
        explorationRenderer.Dispose();
        battleRenderer.Dispose();
        gameOverRenderer.Dispose();
    }
}
