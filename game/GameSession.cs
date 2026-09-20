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
            World.SpawnX,
            World.SpawnY,
            World.Player.HP,
            World.Player.MAXHP,
            World.Floor,
            World.FloorSeed);

        SynchronizeExpedition();
    }

    public MoveResult MovePlayer(int deltaX, int deltaY)
    {
        if (State != GameState.Exploration)
            return MoveResult.Blocked;

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
                StartBattle(enemy);
        }

        return result;
    }

    public void StartTestBattle()
    {
        if (State == GameState.Exploration && World.Enemies.Count > 0)
            StartBattle(World.Enemies[0]);
    }

    public void Interact()
    {
        if (State != GameState.Exploration)
            return;

        InteractiveProp? prop = World.GetPropAt(World.Player.X, World.Player.Y);
        if (prop != null)
        {
            Message = prop.Interact();
            return;
        }

        Tile tile = logic.GetPlayerTile(World);

        if (tile.Type == TileType.StairsDown)
        {
            AdvanceToNextFloor();
            return;
        }

        Message = "There is nothing to interact with here.";
    }

    public void SelectPreviousBattleCommand() => Battle?.SelectPreviousCommand();
    public void SelectNextBattleCommand() => Battle?.SelectNextCommand();

    public void CancelBattle()
    {
        if (State == GameState.Battle)
            EndBattle();
    }

    public void PerformBattleCommand()
    {
        if (Battle == null || State != GameState.Battle)
            return;

        BattleResult result = Battle.PerformPlayerTurn(out _, out int enemyDamage);
        SynchronizeExpedition();
        Message = Battle.CommandMessage;

        if (result == BattleResult.EnemyDefeated)
        {
            World.BeginEnemyDeath(Battle.Enemy);
            StateManager.Campaign.Gold += 10 * World.Floor;
            EndBattle();
            return;
        }

        if (result == BattleResult.PlayerDefeated)
        {
            Message = $"{Battle.Enemy.Name} attacks for {enemyDamage} damage. You were defeated.";
            State = GameState.GameOver;
            StateManager.ActiveExpedition.ExtractionState = "Defeated";
            SynchronizeExpedition();
            return;
        }

        if (result == BattleResult.Escaped)
        {
            EndBattle();
            return;
        }

        if (enemyDamage > 0)
            Message += $" {Battle.Enemy.Name} attacks for {enemyDamage} damage.";
    }

    public void DiscoverCell(int x, int y) =>
        StateManager.MarkDiscovered(x, y);

    public bool IsCellDiscovered(int x, int y) =>
        StateManager.IsDiscovered(x, y);

    public bool Save(string path) =>
        StateManager.Save(path);

    public bool Load(string path)
    {
        if (!StateManager.Load(path))
            return false;

        ExpeditionState expedition = StateManager.ActiveExpedition;
        World.RebuildFloor(expedition.FloorSeed, expedition.CurrentFloor);

        World.Player.MoveTo(
            expedition.PlayerGridPosition.X,
            expedition.PlayerGridPosition.Y);

        ApplyPlayerState(expedition);
        State = GameState.Exploration;
        Battle = null;
        Message = "Expedition loaded.";
        return true;
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

    private void AdvanceToNextFloor()
    {
        StateManager.AdvanceFloor(
            World.SpawnX,
            World.SpawnY,
            World.Player.HP,
            World.Player.MAXHP);

        ExpeditionState expedition = StateManager.ActiveExpedition;
        World.RebuildFloor(expedition.FloorSeed, expedition.CurrentFloor);

        ApplyPlayerState(expedition);
        Message = $"You descend to floor {expedition.CurrentFloor}.";
    }

    private void SynchronizeExpedition()
    {
        StateManager.SynchronizeExpedition(
            World.Player.X,
            World.Player.Y,
            World.Player.HP,
            World.Player.MAXHP);

        PartyMember? playerState = StateManager.ActiveExpedition.Party
            .FirstOrDefault(member => member.Id == "arden");

        if (playerState != null)
        {
            playerState.HP = World.Player.HP;
            playerState.MaxHP = World.Player.MAXHP;
            playerState.Level = World.Player.Level;
        }
    }

    private void ApplyPlayerState(ExpeditionState expedition)
    {
        World.Player.MoveTo(
            expedition.PlayerGridPosition.X,
            expedition.PlayerGridPosition.Y);

        int damage = World.Player.MAXHP - expedition.Health;
        if (damage > 0)
            World.Player.TakeDamage(damage);
        else if (damage < 0)
            World.Player.Heal(-damage);

        DiscoverCell(World.Player.X, World.Player.Y);
    }
}
