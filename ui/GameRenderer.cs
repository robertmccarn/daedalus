using System.Drawing;


public class GameRenderer : IDisposable
{
    private readonly ExplorationRenderer explorationRenderer;
    private readonly BattleRenderer battleRenderer;
    private readonly GameOverRenderer gameOverRenderer;


    public GameRenderer(
        GameWorld world)
    {
        explorationRenderer =
            new ExplorationRenderer(
                world);

        battleRenderer =
            new BattleRenderer();

        gameOverRenderer =
            new GameOverRenderer();
    }


    public void Draw(
        Graphics graphics,
        GameWorld world,
        GameState gameState,
        Character? battleEnemy,
        string message,
        BattleCommand selectedCommand)
    {
        if (gameState ==
            GameState.Battle)
        {
            battleRenderer.Draw(
                graphics,
                world,
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
            world);
    }


    public void Dispose()
    {
        explorationRenderer.Dispose();
        battleRenderer.Dispose();
        gameOverRenderer.Dispose();
    }
}
