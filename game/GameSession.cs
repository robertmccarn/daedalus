using Systemic.Engine.State;

public class GameSession
{
    private readonly GameLogic logic;

    public GameWorld World { get; }
    public PartyController Party { get; }
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
        Party = new PartyController(World);

        StateManager.StartNewExpedition(
            World.SpawnX,
            World.SpawnY,
            30,
            30,
            World.Floor,
            World.FloorSeed,
            StateManager.Campaign.PartyRoster.Take(4).Select(member => member.Id).ToArray(),
            StateManager.Campaign.PartyRoster[0].Id,
            PartyFormationType.Column);

        Party.Initialize(
            StateManager.ActiveExpedition,
            new GridPosition(World.SpawnX, World.SpawnY));

        SyncCompatibilityPlayer();
        SynchronizeExpedition();
        RecordNodeVisit(World.SpawnX, World.SpawnY);
        ApplyGearBonuses();
    }

    public MoveResult MovePlayer(int deltaX, int deltaY)
    {
        CharacterDirection? direction = (deltaX, deltaY) switch
        {
            (0, -1) => CharacterDirection.Up,
            (0, 1) => CharacterDirection.Down,
            (-1, 0) => CharacterDirection.Left,
            (1, 0) => CharacterDirection.Right,
            _ => null
        };

        return direction.HasValue
            ? MoveLeader(direction.Value)
            : MoveResult.Blocked;
    }

    public MoveResult MoveLeader(CharacterDirection direction)
    {
        if (State != GameState.Exploration ||
            StateManager.ActiveExpedition.ExtractionState != "Active")
            return MoveResult.Blocked;

        MoveResult result = Party.TryMoveLeader(
            direction,
            StateManager.ActiveExpedition);

        if (result == MoveResult.Moved)
        {
            SyncCompatibilityPlayer();
            StateManager.ActiveExpedition.TurnCount++;
            UpkeepSystem.ApplyTurn(
                StateManager.Campaign,
                StateManager.ActiveExpedition);

            GridPosition position = Party.LeaderPosition;
            RecordNodeVisit(position.X, position.Y);
            SynchronizeExpedition();
        }

        if (result == MoveResult.Encounter)
        {
            GridPosition target = GetAdjacentPosition(Party.LeaderPosition, direction);
            Character? enemy = World.GetEnemyAt(target.X, target.Y);
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

        GridPosition leaderPosition = Party.LeaderPosition;
        InteractiveProp? prop = World.GetPropAt(
            leaderPosition.X,
            leaderPosition.Y);

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
                MoraleSystem.ApplyEvent(
                    StateManager.ActiveExpedition,
                    MoraleEventType.LootFound);

                CompleteNode(chest.X, chest.Y);
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
                RewardBundle terminalReward = new();
                terminalReward.Cores.Add(terminal.CoreReward);
                ExtractionSystem.ApplyReward(
                    StateManager.Campaign,
                    StateManager.ActiveExpedition,
                    terminalReward);

                World.Player.Heal(terminal.HealAmount);
                SynchronizeExpedition();
                CompleteNode(terminal.X, terminal.Y);
                Message += $" Restored {terminal.HealAmount} HP.";
            }

            RecordNodeVisit(terminal.X, terminal.Y);
            return;
        }

        Tile tile = logic.GetTile(World, leaderPosition);

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
                .FirstOrDefault(member => member.Id == Party.LeaderId);

            if (partyMember != null)
            {
                ExpeditionRewardSystem.ApplyCombatReward(
                    StateManager.Campaign,
                    StateManager.ActiveExpedition,
                    partyMember,
                    reward);
                MoraleSystem.ApplyEvent(
                    StateManager.ActiveExpedition,
                    MoraleEventType.EnemyDefeated);
            }

            World.BeginEnemyDeath(defeatedEnemy);
            CompleteNode(defeatedEnemy.X, defeatedEnemy.Y);
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
            Party.GetLeaderRuntime().IsDefeated = true;
            SynchronizeExpedition();
            return;
        }

        if (result == BattleResult.Escaped)
        {
            MoraleSystem.ApplyEvent(
                StateManager.ActiveExpedition,
                MoraleEventType.Retreat);
            EndBattle();
            return;
        }

        if (enemyDamage > 0)
            Message += $" {Battle.Enemy.Name} attacks for {enemyDamage} damage.";
    }

    public void DiscoverCell(int x, int y) =>
        StateManager.DiscoverArea(x, y);

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
        World.RestoreExpeditionState(expedition);

        Party.Load(
            expedition,
            new GridPosition(
                expedition.PlayerGridPosition.X,
                expedition.PlayerGridPosition.Y));
        SyncCompatibilityPlayer();
        ApplyGearBonuses();

        State = GameState.Exploration;
        Battle = null;
        Message = "Expedition loaded.";
        return true;
    }

    public bool ExtractExpedition()
    {
        if (State != GameState.Exploration ||
            StateManager.ActiveExpedition.ExtractionState != "Active")
            return false;

        GridPosition leaderPosition = Party.LeaderPosition;
        DungeonNode? node = World.GetNodeAt(
            leaderPosition.X,
            leaderPosition.Y);

        if (node?.Type != DungeonNodeType.Extraction)
        {
            Message = "Extraction is only available at an extraction point.";
            return false;
        }

        int upkeep = StateManager.ActiveExpedition.Upkeep;

        if (!ExtractionSystem.Extract(StateManager))
            return false;

        State = GameState.Exploration;
        Battle = null;
        Message =
            $"Expedition extracted at depth {StateManager.ActiveExpedition.CurrentFloor}. " +
            $"Upkeep settled: {upkeep}.";

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

    public bool EquipGear(string gearId, string? partyMemberId = null)
    {
        string targetId = partyMemberId ?? Party.LeaderId;
        bool equipped = InventorySystem.EquipGear(
            StateManager.Campaign,
            StateManager.ActiveExpedition,
            targetId,
            gearId);

        if (equipped)
            ApplyGearBonuses();

        return equipped;
    }

    private void StartBattle(Character enemy)
    {
        SyncCompatibilityPlayer();
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
        SyncCompatibilityPlayer();
    }

    private void AdvanceToNextFloor()
    {
        PartyMember leader = GetLeaderState() ?? throw new InvalidOperationException("Expedition leader is missing.");

        StateManager.AdvanceFloor(
            leader.HP,
            leader.MaxHP);

        ExpeditionState expedition = StateManager.ActiveExpedition;
        World.RebuildFloor(
            expedition.FloorSeed,
            expedition.CurrentFloor);

        Party.ReformForFloor(
            expedition,
            new GridPosition(World.SpawnX, World.SpawnY));
        SyncCompatibilityPlayer();
        StateManager.SetExpeditionPosition(
            World.SpawnX,
            World.SpawnY);
        RecordNodeVisit(World.SpawnX, World.SpawnY);
        ApplyGearBonuses();
        Message = $"You descend to floor {expedition.CurrentFloor}.";
    }

    private void CompleteNode(int x, int y)
    {
        DungeonNode? node = World.GetNodeAt(x, y);
        if (node == null)
            return;

        World.CompleteNodeAt(x, y);
        if (!StateManager.ActiveExpedition.CompletedNodeIds.Contains(node.Id))
            StateManager.ActiveExpedition.CompletedNodeIds.Add(node.Id);

        if (node.Type == DungeonNodeType.Combat &&
            !StateManager.ActiveExpedition.DefeatedNodeIds.Contains(node.Id))
        {
            StateManager.ActiveExpedition.DefeatedNodeIds.Add(node.Id);
        }
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
        PartyMember? leader = GetLeaderState();
        if (leader == null)
            return;

        StateManager.SynchronizeExpedition(
            Party.LeaderPosition.X,
            Party.LeaderPosition.Y,
            World.Player.HP,
            World.Player.MAXHP);

        leader.HP = World.Player.HP;
        leader.MaxHP = World.Player.MAXHP;
        leader.Level = World.Player.Level;
    }

    private void SyncCompatibilityPlayer()
    {
        PartyMember? leader = GetLeaderState();
        if (leader == null)
            return;

        World.SyncPlayerFromPartyMember(
            leader,
            Party.LeaderPosition);
    }

    private void ApplyGearBonuses()
    {
        PartyMember? partyMember = GetLeaderState();

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

    private PartyMember? GetLeaderState() =>
        StateManager.ActiveExpedition.Party
            .FirstOrDefault(member => member.Id == Party.LeaderId);

    private static GridPosition GetAdjacentPosition(
        GridPosition position,
        CharacterDirection direction) => direction switch
        {
            CharacterDirection.Up => new GridPosition(position.X, position.Y - 1),
            CharacterDirection.Down => new GridPosition(position.X, position.Y + 1),
            CharacterDirection.Left => new GridPosition(position.X - 1, position.Y),
            _ => new GridPosition(position.X + 1, position.Y)
        };
}
