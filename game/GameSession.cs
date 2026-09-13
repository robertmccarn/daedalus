using Systemic.Engine.State;

public class GameSession
{
    private readonly GameLogic logic;

    public GameWorld World { get; }
    public GameState State { get; private set; }
    public BattleSystem? Battle { get; private set; }
    public string Message { get; private set; }
    public GameStateManager StateManager { get; }

    public GameSession(GameWorld world)
    {
        World = world;
        logic = new GameLogic();
        State = GameState.Exploration;
        Message = string.Empty;
        StateManager = new GameStateManager();
        StateManager.StartNewExpedition(
            World.Player.X,
            World.Player.Y,
            World.Player.HP,
            World.Player.MAXHP);
    }

    public MoveResult MovePlayer(int deltaX, int deltaY)
    {
        if (State != GameState.Exploration)
        {
            return MoveResult.Blocked;
        }

        int targetX = World.Player.X + deltaX;
        int targetY = World.Player.Y + deltaY;
        MoveResult result = logic.MovePlayer(World, targetX, targetY);

        if (result == MoveResult.Moved)
        {
            StateManager.ActiveExpedition.TurnCount++;
            SynchronizeExpedition();
        }

        if (result == MoveResult.Encounter)
        {
            Character? enemy = World.GetEnemyAt(targetX, targetY);
            if (enemy != null)
            {
                StartBattle(enemy);
            }
        }

        return result;
    }

    public void StartTestBattle()
    {
        if (State == GameState.Exploration && World.Enemies.Count > 0)
        {
            StartBattle(World.Enemies[0]);
        }
    }

    public void Interact()
    {
        if (State != GameState.Exploration)
        {
            return;
        }

        InteractiveProp? prop = World.GetPropAt(World.Player.X, World.Player.Y);
        if (prop != null)
        {
            Message = prop.Interact();
            return;
        }

        Tile tile = logic.GetPlayerTile(World);
        Message = tile.Type == TileType.StairsDown
            ? "The stairs lead deeper into the dungeon."
            : "There is nothing to interact with here.";
    }

    public void SelectPreviousBattleCommand() => Battle?.SelectPreviousCommand();
    public void SelectNextBattleCommand() => Battle?.SelectNextCommand();

    public void CancelBattle()
    {
        if (State == GameState.Battle)
        {
            EndBattle();
        }
    }

    public void PerformBattleCommand()
    {
        if (Battle == null || State != GameState.Battle)
        {
            return;
        }

        BattleResult result = Battle.PerformPlayerTurn(out _, out int enemyDamage);
        SynchronizeExpedition();
        Message = Battle.CommandMessage;

        if (result == BattleResult.EnemyDefeated)
        {
            World.BeginEnemyDeath(Battle.Enemy);
            EndBattle();
            return;
        }

        if (result == BattleResult.PlayerDefeated)
        {
            Message = $"{Battle.Enemy.Name} attacks for {enemyDamage} damage. You were defeated.";
            State = GameState.GameOver;
            return;
        }

        if (result == BattleResult.Escaped)
        {
            EndBattle();
            return;
        }

        if (enemyDamage > 0)
        {
            Message += $" {Battle.Enemy.Name} attacks for {enemyDamage} damage.";
        }
    }

    private void StartBattle(Character enemy)
    {
        Battle = new BattleSystem(World.Player, enemy);
        State = GameState.Battle;
        Message = string.Empty;
    }

    private void EndBattle()
    {
        State = GameState.Exploration;
        Battle = null;
        Message = string.Empty;
    }

    private void SynchronizeExpedition()
    {
        StateManager.SynchronizeExpedition(
            World.Player.X,
            World.Player.Y,
            World.Player.HP,
            World.Player.MAXHP);
    }
}
