using Systemic.Engine.State;

public class GameSession
{
    private readonly GameLogic logic;
    private FeedbackEffect? feedback;
    private bool landmarkIntroduced;

    public GameWorld World { get; }
    public PartyController Party { get; }
    public GameState State { get; private set; }
    public BattleSystem? Battle { get; private set; }
    public string Message { get; private set; }
    public GameStateManager StateManager { get; }
    public ExtractionSummary? LastExtraction { get; private set; }
    public FeedbackEffect? Feedback => feedback;

    public string CurrentObjective =>
        StateManager.ActiveExpedition.CurrentFloor <= 1
            ? "Reach the extraction point with something worth bringing back."
            : $"Reach the extraction point on depth {StateManager.ActiveExpedition.CurrentFloor}.";

    public GameSession(GameWorld world)
    {
        World = world;
        logic = new GameLogic();
        State = GameState.Exploration;
        Message = string.Empty;
        StateManager = new GameStateManager();
        Party = new PartyController(World);
        StartNewExpeditionInternal();
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
        return direction.HasValue ? MoveLeader(direction.Value) : MoveResult.Blocked;
    }

    public MoveResult MoveLeader(CharacterDirection direction)
    {
        if (State != GameState.Exploration || StateManager.ActiveExpedition.ExtractionState != "Active")
            return MoveResult.Blocked;

        MoveResult result = Party.TryMoveLeader(direction, StateManager.ActiveExpedition);

        if (result == MoveResult.Moved)
        {
            SyncCompatibilityPlayer();
            StateManager.ActiveExpedition.TurnCount++;
            UpkeepSystem.ApplyTurn(StateManager.Campaign, StateManager.ActiveExpedition);

            GridPosition position = Party.LeaderPosition;
            RecordNodeVisit(position.X, position.Y);
            SynchronizeExpedition();
            UpdateVisibility();
            SetFeedback(FeedbackEffectType.Discovery, position);
            CheckLandmarkDiscovery(position);

            if (StateManager.ActiveExpedition.TurnCount % 12 == 0)
                TriggerExplorationEvent();
        }

        if (result == MoveResult.Encounter)
        {
            GridPosition target = GetAdjacentPosition(Party.LeaderPosition, direction);
            Character? enemy = World.GetEnemyAt(target.X, target.Y);
            if (enemy != null) StartBattle(enemy);
        }

        return result;
    }

    public void TriggerExplorationEvent()
    {
        if (State != GameState.Exploration || StateManager.ActiveExpedition.ExtractionState != "Active")
            return;

        ExpeditionEvent expeditionEvent = EventSystem.Roll(
            StateManager.ActiveExpedition.FloorSeed,
            StateManager.ActiveExpedition.CurrentFloor,
            StateManager.ActiveExpedition.TurnCount);

        EventSystem.Apply(
            StateManager.Campaign,
            StateManager.ActiveExpedition,
            expeditionEvent);

        Message = $"{expeditionEvent.Title}: {expeditionEvent.Description}";
        if (expeditionEvent.GoldDelta != 0)
            Message += $" +{expeditionEvent.GoldDelta} carried gold.";

        SetFeedback(
            expeditionEvent.MoraleDelta < 0
                ? FeedbackEffectType.Danger
                : FeedbackEffectType.Discovery,
            Party.LeaderPosition);

        SynchronizeExpedition();
    }

    public void Interact()
    {
        if (State != GameState.Exploration || StateManager.ActiveExpedition.ExtractionState != "Active")
            return;

        GridPosition leaderPosition = Party.LeaderPosition;
        InteractiveProp? prop = World.GetPropAt(leaderPosition.X, leaderPosition.Y);

        if (prop is Chest chest)
        {
            bool wasOpen = chest.IsOpen;
            Message = chest.Interact();
            if (!wasOpen && chest.IsOpen)
            {
                ExtractionSystem.ApplyReward(StateManager.Campaign, StateManager.ActiveExpedition, chest.Reward);
                MoraleSystem.ApplyEvent(StateManager.ActiveExpedition, MoraleEventType.LootFound);
                CompleteNode(chest.X, chest.Y);
                Message += " Supplies recovered.";
                SetFeedback(FeedbackEffectType.Loot, leaderPosition);
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
                ExtractionSystem.ApplyReward(StateManager.Campaign, StateManager.ActiveExpedition, terminalReward);
                PartyMember? leader = GetLeaderState();
                if (leader != null)
                    leader.HP = Math.Min(leader.MaxHP, leader.HP + terminal.HealAmount);
                SynchronizeExpedition();
                CompleteNode(terminal.X, terminal.Y);
                Message += $" Restored {terminal.HealAmount} HP.";
                SetFeedback(FeedbackEffectType.Heal, leaderPosition);
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

        TriggerExplorationEvent();
    }

    public void SelectPreviousBattleCommand() => Battle?.SelectPreviousCommand();
    public void SelectNextBattleCommand() => Battle?.SelectNextCommand();
    public void SelectNextBattleTarget() => Battle?.SelectNextTarget();

    public void CancelBattle()
    {
        if (State == GameState.Battle)
        {
            MoraleSystem.ApplyEvent(StateManager.ActiveExpedition, MoraleEventType.Retreat);
            EndBattle();
        }
    }

    public void PerformBattleCommand()
    {
        if (Battle == null || State != GameState.Battle) return;

        BattleResult result = Battle.PerformPlayerTurn(out _, out int enemyDamage);
        SynchronizeExpedition();
        Message = Battle.CommandMessage;

        if (result == BattleResult.EnemyDefeated)
        {
            IReadOnlyList<Character> defeatedEnemies = Battle.DefeatedEnemies.ToArray();

            foreach (Character defeatedEnemy in defeatedEnemies)
            {
                RewardBundle reward = ExpeditionRewardSystem.CreateCombatReward(defeatedEnemy, World.Floor);
                MoraleSystem.ApplyEvent(StateManager.ActiveExpedition, MoraleEventType.EnemyDefeated);

                foreach (PartyMember partyMember in StateManager.ActiveExpedition.Party.Where(member => member.HP > 0))
                    ProgressionSystem.ApplyExperience(partyMember, reward.Experience);

                ExtractionSystem.ApplyReward(StateManager.Campaign, StateManager.ActiveExpedition, reward);
                World.BeginEnemyDeath(defeatedEnemy);
                CompleteNode(defeatedEnemy.X, defeatedEnemy.Y);
            }

            int xp = defeatedEnemies.Sum(enemy => ExpeditionRewardSystem.CreateCombatReward(enemy, World.Floor).Experience);
            int gold = defeatedEnemies.Sum(enemy => ExpeditionRewardSystem.CreateCombatReward(enemy, World.Floor).Gold);
            int cores = defeatedEnemies.Sum(enemy => ExpeditionRewardSystem.CreateCombatReward(enemy, World.Floor).Cores.Sum(core => core.Quantity));

            EndBattle();
            Message = $"Victory. +{xp} XP, +{gold} gold, +{cores} core(s), and drops recovered.";
            ApplyGearBonuses();
            SynchronizeExpedition();
            UpdateVisibility();
            SetFeedback(FeedbackEffectType.Victory, Party.LeaderPosition);
            return;
        }

        if (result == BattleResult.PlayerDefeated)
        {
            Message = Battle.CommandMessage;
            State = GameState.GameOver;
            StateManager.ActiveExpedition.ExtractionState = "Defeated";
            Party.GetLeaderRuntime().IsDefeated = true;
            SynchronizeExpedition();
            SetFeedback(FeedbackEffectType.Danger, Party.LeaderPosition);
            return;
        }

        if (result == BattleResult.Escaped)
        {
            MoraleSystem.ApplyEvent(StateManager.ActiveExpedition, MoraleEventType.Retreat);
            EndBattle();
            return;
        }

        if (enemyDamage > 0)
            SetFeedback(FeedbackEffectType.Damage, Party.LeaderPosition);
    }

    public void DiscoverCell(int x, int y) => StateManager.DiscoverArea(x, y);
    public bool IsCellDiscovered(int x, int y) => StateManager.IsDiscovered(x, y);
    public bool IsCellVisible(int x, int y) =>
        VisibilitySystem.IsVisible(World, Party.LeaderPosition, new GridPosition(x, y));

    private bool IsCellVisibleFromLeader(int x, int y) =>
        VisibilitySystem.IsVisible(World, Party.LeaderPosition, new GridPosition(x, y));

    public bool Save(string path) => StateManager.Save(path);

    public bool Load(string path)
    {
        if (!StateManager.Load(path)) return false;
        ExpeditionState expedition = StateManager.ActiveExpedition;
        World.RebuildFloor(expedition.FloorSeed, expedition.CurrentFloor);
        World.RestoreExpeditionState(expedition);
        Party.Load(expedition, new GridPosition(expedition.PlayerGridPosition.X, expedition.PlayerGridPosition.Y));
        SyncCompatibilityPlayer();
        ApplyGearBonuses();
        State = GameState.Exploration;
        Battle = null;
        Message = "Expedition loaded.";
        UpdateVisibility();
        feedback = null;
        return true;
    }

    public bool ExtractExpedition()
    {
        if (State != GameState.Exploration || StateManager.ActiveExpedition.ExtractionState != "Active")
            return false;

        DungeonNode? node = World.GetNodeAt(Party.LeaderPosition.X, Party.LeaderPosition.Y);
        if (node?.Type != DungeonNodeType.Extraction)
        {
            Message = "Extraction is only available at an extraction point.";
            return false;
        }

        ExpeditionState expedition = StateManager.ActiveExpedition;
        int depth = expedition.CurrentFloor;
        int gold = expedition.CarriedGold;
        int cores = expedition.CarriedCores.Count;
        int materials = expedition.CarriedMaterials.Sum(material => material.Quantity);
        int items = expedition.CarriedInventory.Sum(item => item.Quantity);
        int gear = expedition.CarriedGear.Count;

        int upkeep = expedition.Upkeep;
        if (!ExtractionSystem.Extract(StateManager)) return false;

        LastExtraction = new ExtractionSummary(
            depth,
            gold,
            cores,
            materials,
            items,
            gear,
            StateManager.Campaign.Gold,
            StateManager.Campaign.RunsCompleted);

        Message = $"Expedition extracted at depth {depth}. Upkeep settled: {upkeep}.";
        State = GameState.ExtractionResults;
        Battle = null;
        SetFeedback(FeedbackEffectType.Extraction, Party.LeaderPosition);
        return true;
    }

    public void ReturnToCampaign()
    {
        if (State == GameState.ExtractionResults)
        {
            State = GameState.Campaign;
            Message = "Expedition complete. Prepare the next descent.";
        }
    }

    public void StartNewExpeditionFromCampaign()
    {
        if (State == GameState.Campaign || State == GameState.ExtractionResults || State == GameState.GameOver)
            StartNewExpeditionInternal();
    }

    public Gear? SynthesizeGear(string recipeId)
    {
        Recipe? recipe = StateManager.Campaign.Recipes.FirstOrDefault(candidate => candidate.Id == recipeId);
        return recipe == null ? null : SynthesisSystem.SynthesizeGear(StateManager.Campaign, recipe);
    }

    public bool EquipGear(string gearId, string? partyMemberId = null)
    {
        string targetId = partyMemberId ?? Party.LeaderId;
        bool equipped = InventorySystem.EquipGear(StateManager.Campaign, StateManager.ActiveExpedition, targetId, gearId);
        if (equipped) ApplyGearBonuses();
        return equipped;
    }

    private void StartNewExpeditionInternal()
    {
        World.RebuildFloor(Random.Shared.Next(), 1);
        StateManager.StartNewExpedition(
            World.SpawnX, World.SpawnY, 30, 30, World.Floor, World.FloorSeed,
            StateManager.Campaign.PartyRoster.Take(4).Select(member => member.Id).ToArray(),
            StateManager.Campaign.PartyRoster[0].Id,
            PartyFormationType.Column,
            (x, y) => VisibilitySystem.IsVisible(
                World,
                new GridPosition(World.SpawnX, World.SpawnY),
                new GridPosition(x, y)));

        Party.Initialize(StateManager.ActiveExpedition, new GridPosition(World.SpawnX, World.SpawnY));
        SyncCompatibilityPlayer();
        SynchronizeExpedition();
        RecordNodeVisit(World.SpawnX, World.SpawnY);
        UpdateVisibility();
        ApplyGearBonuses();

        State = GameState.Exploration;
        Battle = null;
        feedback = null;
        landmarkIntroduced = false;
        Message = "The expedition enters the ruins.";
    }

    private void StartBattle(Character enemy)
    {
        SyncCompatibilityPlayer();
        ApplyGearBonuses();

        List<Character> battleEnemies = World.Enemies
            .OrderBy(candidate => candidate == enemy ? 0 : 1)
            .ThenBy(candidate => Math.Abs(candidate.X - enemy.X) + Math.Abs(candidate.Y - enemy.Y))
            .ThenBy(candidate => candidate.Name, StringComparer.Ordinal)
            .Take(3)
            .ToList();

        Battle = new BattleSystem(
            StateManager.ActiveExpedition,
            battleEnemies,
            StateManager.Campaign);
        State = GameState.Battle;
        Message = Battle.CommandMessage;
        SetFeedback(FeedbackEffectType.Danger, new GridPosition(enemy.X, enemy.Y));
    }

    private void EndBattle()
    {
        State = GameState.Exploration;
        Battle = null;
        SyncCompatibilityPlayer();
        UpdateVisibility();
    }

    private void AdvanceToNextFloor()
    {
        PartyMember leader = GetLeaderState() ?? throw new InvalidOperationException("Expedition leader is missing.");
        StateManager.AdvanceFloor(leader.HP, leader.MaxHP);
        ExpeditionState expedition = StateManager.ActiveExpedition;
        World.RebuildFloor(expedition.FloorSeed, expedition.CurrentFloor);
        Party.ReformForFloor(expedition, new GridPosition(World.SpawnX, World.SpawnY));
        SyncCompatibilityPlayer();
        StateManager.SetExpeditionPosition(
            World.SpawnX,
            World.SpawnY,
            IsCellVisibleFromLeader);
        RecordNodeVisit(World.SpawnX, World.SpawnY);
        ApplyGearBonuses();
        Message = $"You descend to floor {expedition.CurrentFloor}.";
        UpdateVisibility();
        SetFeedback(FeedbackEffectType.Discovery, Party.LeaderPosition);
    }

    private void CompleteNode(int x, int y)
    {
        DungeonNode? node = World.GetNodeAt(x, y);
        if (node == null) return;
        World.CompleteNodeAt(x, y);
        if (!StateManager.ActiveExpedition.CompletedNodeIds.Contains(node.Id))
            StateManager.ActiveExpedition.CompletedNodeIds.Add(node.Id);
        if (node.Type == DungeonNodeType.Combat && !StateManager.ActiveExpedition.DefeatedNodeIds.Contains(node.Id))
            StateManager.ActiveExpedition.DefeatedNodeIds.Add(node.Id);
    }

    private void RecordNodeVisit(int x, int y)
    {
        DungeonNode? node = World.GetNodeAt(x, y);
        if (node == null) return;
        StateManager.ActiveExpedition.CurrentNode = node.Id;
        if (!StateManager.ActiveExpedition.NodeHistory.Contains(node.Id))
            StateManager.ActiveExpedition.NodeHistory.Add(node.Id);
        DiscoverCell(x, y);
    }

    private void SynchronizeExpedition()
    {
        PartyMember? leader = GetLeaderState();
        if (leader == null) return;

        SyncCompatibilityPlayer();

        StateManager.SynchronizeExpedition(
            Party.LeaderPosition.X,
            Party.LeaderPosition.Y,
            leader.HP,
            leader.MaxHP,
            IsCellVisibleFromLeader);

        leader.Level = Math.Max(1, leader.Level);
    }

    private void UpdateVisibility()
    {
        GridPosition leader = Party.LeaderPosition;
        int radius = 7;

        for (int y = Math.Max(0, leader.Y - radius); y <= Math.Min(GameWorld.Height - 1, leader.Y + radius); y++)
        for (int x = Math.Max(0, leader.X - radius); x <= Math.Min(GameWorld.Width - 1, leader.X + radius); x++)
            if (VisibilitySystem.IsVisible(World, leader, new GridPosition(x, y), radius))
                StateManager.MarkDiscovered(x, y);
    }

    private void SetFeedback(FeedbackEffectType type, GridPosition position, int durationMs = 900) =>
        feedback = new FeedbackEffect(type, position.X, position.Y, Environment.TickCount64, durationMs);

    private void CheckLandmarkDiscovery(GridPosition position)
    {
        if (landmarkIntroduced)
            return;

        WorldVisualFeature? landmark = World.VisualFeatures
            .FirstOrDefault(feature => feature.Type == WorldVisualFeatureType.Landmark);

        if (landmark == null)
            return;

        int distance = Math.Abs(landmark.X - position.X) + Math.Abs(landmark.Y - position.Y);
        if (distance > 3 || !IsCellVisible(landmark.X, landmark.Y))
            return;

        landmarkIntroduced = true;
        Message = "A dormant resonance cuts through the ruin. Something ancient is still listening.";
        SetFeedback(FeedbackEffectType.Discovery, new GridPosition(landmark.X, landmark.Y), 1400);
    }

    private void SyncCompatibilityPlayer()
    {
        PartyMember? leader = GetLeaderState();
        if (leader == null) return;
        World.SyncPlayerFromPartyMember(leader, Party.LeaderPosition);
    }

    private void ApplyGearBonuses()
    {
        PartyMember? partyMember = GetLeaderState();
        if (partyMember == null) { World.Player.SetEquipmentBonuses(0, 0); return; }

        int attackBonus = 0;
        int defenseBonus = 0;
        foreach (string gearId in partyMember.EquippedGearIds)
        {
            Gear? gear = StateManager.Campaign.Gear.FirstOrDefault(candidate => candidate.Id == gearId);
            if (gear == null) continue;
            if (gear.Slot.Equals("Weapon", StringComparison.OrdinalIgnoreCase)) attackBonus += gear.Power;
            else defenseBonus += gear.Power;
        }

        World.Player.SetEquipmentBonuses(attackBonus, defenseBonus);
    }

    private PartyMember? GetLeaderState() =>
        StateManager.ActiveExpedition.Party.FirstOrDefault(member => member.Id == Party.LeaderId);

    private static GridPosition GetAdjacentPosition(GridPosition position, CharacterDirection direction) => direction switch
    {
        CharacterDirection.Up => new GridPosition(position.X, position.Y - 1),
        CharacterDirection.Down => new GridPosition(position.X, position.Y + 1),
        CharacterDirection.Left => new GridPosition(position.X - 1, position.Y),
        _ => new GridPosition(position.X + 1, position.Y)
    };
}
