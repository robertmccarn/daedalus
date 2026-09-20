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
        ApplyGearBonuses();
    }

    public MoveResult MovePlayer(int deltaX, int deltaY)
    {
        if (State != GameState.Exploration ||
            StateManager.ActiveExpedition.ExtractionState != "Active")
            return MoveResult.Blocked;

        int targetX = World.Player.X + deltaX;
        int targetY = World.Player.Y + deltaY;
        MoveResult result = logic.MovePlayer(World, targetX, targetY);

        if (result == MoveResult.Moved)
        {
            StateManager.ActiveExpedition.TurnCount++;
            UpkeepSystem.ApplyTurn(
                StateManager.Campaign,
                StateManager.ActiveExpedition);

            RecordNodeVisit(targetX, targetY);
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
        if (State == GameState.Exploration &&
            StateManager.ActiveExpedition.ExtractionState == "Active" &&
            World.Enemies.Count > 0)
        {
            StartBattle(World.Enemies[0]);
        }
    }

    public void Interact()
    {
        if (State != GameState.Exploration ||
            StateManager.ActiveExpedition.ExtractionState != "Active")
            return;

        InteractiveProp? prop = World.GetPropAt(
            World.Player.X,
            World.Player.Y);

        if (prop is Chest chest)
        {
            bool wasOpen = chest.IsOpen;
            Message = chest.Interact();

            if (!wasOpen && chest.IsOpen)
            {
                ExtractionSystem.ApplyReward(
                    StateManager.Campaign,
                    StateManager.ActiveExpedition,
                    chest.Reward);

                World.CompleteNodeAt(chest.X, chest.Y);
                Message += " Supplies recovered.";
            }

            RecordNodeVisit(chest.X, chest.Y);
            return;
        }

        if (prop is Terminal terminal)
        {
            bool wasActivated = terminal.IsActivated;
            Message = terminal.Interact();

            if (!wasActivated && terminal.IsActivated)
            {
                for (int i = 0; i < terminal.CoreReward.Quantity; i++)
                {
                    StateManager.Campaign.Cores.Add(new EnergyCore
                    {
                        Type = terminal.CoreReward.Type,
                        Charge = terminal.CoreReward.Charge
                    });
                }

                World.Player.Heal(terminal.HealAmount);
                World.CompleteNodeAt(terminal.X, terminal.Y);
                Message += $" Restored {terminal.HealAmount} HP.";
            }

            RecordNodeVisit(terminal.X, terminal.Y);
            SynchronizeExpedition();
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

    public void SelectPreviousBattleCommand() =>
        Battle?.SelectPreviousCommand();

    public void SelectNextBattleCommand() =>
        Battle?.SelectNextCommand();

    public void CancelBattle()
    {
        if (State == GameState.Battle)
            EndBattle();
    }

    public void PerformBattleCommand()
    {
        if (Battle == null || State != GameState.Battle)
            return;

        BattleResult result = Battle.PerformPlayerTurn(
            out _,
            out int enemyDamage);

        SynchronizeExpedition();
        Message = Battle.CommandMessage;

        if (result == BattleResult.EnemyDefeated)
        {
            Character defeatedEnemy = Battle.Enemy;
            RewardBundle reward = ExpeditionRewardSystem.CreateCombatReward(
                defeatedEnemy,
                World.Floor);

            PartyMember? partyMember = StateManager.ActiveExpedition.Party
                .FirstOrDefault(member => member.Id == "arden");

            if (partyMember != null)
            {
                ExpeditionRewardSystem.ApplyCombatReward(
                    StateManager.Campaign,
                    StateManager.ActiveExpedition,
                    partyMember,
                    reward);
            }

            World.BeginEnemyDeath(defeatedEnemy);
            EndBattle();

            Message =
                $"Victory. +{reward.Experience} XP, +{reward.Gold} gold, " +
                $"+{reward.Cores.Sum(core => core.Quantity)} core, and drops recovered.";

            ApplyGearBonuses();
            return;
        }

        if (result == BattleResult.PlayerDefeated)
        {
            Message =
                $"{Battle.Enemy.Name} attacks for {enemyDamage} damage. You were defeated.";

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
        World.RebuildFloor(
            expedition.FloorSeed,
            expedition.CurrentFloor);

        ApplyPlayerState(expedition);
        ApplyGearBonuses();

        State = GameState.Exploration;
        Battle = null;
        Message = "Expedition loaded.";
        return true;
    }

    public bool ExtractExpedition()
    {
        if (!ExtractionSystem.Extract(StateManager))
            return false;

        State = GameState.Exploration;
        Battle = null;
        Message =
            $"Expedition extracted at depth {StateManager.ActiveExpedition.CurrentFloor}. " +
            $"Upkeep incurred: {StateManager.ActiveExpedition.Upkeep}.";

        return true;
    }

    public Gear? SynthesizeGear(string recipeId)
    {
        Recipe? recipe = StateManager.Campaign.Recipes
            .FirstOrDefault(candidate => candidate.Id == recipeId);

        if (recipe == null)
            return null;

        return SynthesisSystem.SynthesizeGear(
            StateManager.Campaign,
            recipe);
    }

    public bool EquipGear(string gearId, string partyMemberId = "arden")
    {
        bool equipped = InventorySystem.EquipGear(
            StateManager.Campaign,
            partyMemberId,
            gearId);

        if (equipped)
            ApplyGearBonuses();

        return equipped;
    }

    private void StartBattle(Character enemy)
    {
        ApplyGearBonuses();
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
        World.RebuildFloor(
            expedition.FloorSeed,
            expedition.CurrentFloor);

        ApplyPlayerState(expedition);
        ApplyGearBonuses();
        Message = $"You descend to floor {expedition.CurrentFloor}.";
    }

    private void RecordNodeVisit(int x, int y)
    {
        DungeonNode? node = World.GetNodeAt(x, y);
        if (node == null)
            return;

        StateManager.ActiveExpedition.CurrentNode = node.Id;

        if (!StateManager.ActiveExpedition.NodeHistory.Contains(node.Id))
            StateManager.ActiveExpedition.NodeHistory.Add(node.Id);

        DiscoverCell(x, y);
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

    private void ApplyGearBonuses()
    {
        PartyMember? partyMember = StateManager.ActiveExpedition.Party
            .FirstOrDefault(member => member.Id == "arden");

        if (partyMember == null)
        {
            World.Player.SetEquipmentBonuses(0, 0);
            return;
        }

        int attackBonus = 0;
        int defenseBonus = 0;

        foreach (string gearId in partyMember.EquippedGearIds)
        {
            Gear? gear = StateManager.Campaign.Gear
                .FirstOrDefault(candidate => candidate.Id == gearId);

            if (gear == null)
                continue;

            if (gear.Slot.Equals("Weapon", StringComparison.OrdinalIgnoreCase))
                attackBonus += gear.Power;
            else
                defenseBonus += gear.Power;
        }

        World.Player.SetEquipmentBonuses(
            attackBonus,
            defenseBonus);
    }
}
