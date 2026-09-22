using Systemic.Engine.State;

public class GameSession
{
    private readonly GameLogic logic;
    private FeedbackEffect? feedback;
    private bool landmarkIntroduced;
    private bool devMenuOpen;
    private int devSelectedFloor = 1;

    public GameWorld World { get; }
    public PartyController Party { get; }
    public GameState State { get; private set; }
    public BattleSystem? Battle { get; private set; }
    public string Message { get; private set; }
    public GameStateManager StateManager { get; }
    public ExtractionSummary? LastExtraction { get; private set; }
    public GameplayRecorder Recorder { get; }
    public FeedbackEffect? Feedback => feedback;
    public bool DevMenuOpen => devMenuOpen;
    public int DevSelectedFloor => devSelectedFloor;
    public int DevPartyLevel => DevMenuState.PartyLevelForFloor(devSelectedFloor);

    public string CurrentObjective =>
        StateManager.ActiveExpedition.CurrentFloor <= 1
            ? "Reach the extraction point with something worth bringing back."
            : $"Reach the extraction point on depth {StateManager.ActiveExpedition.CurrentFloor}.";

    public GameSession(GameWorld world, GameplayRecorder? recorder = null)
    {
        World = world;
        logic = new GameLogic();
        State = GameState.Exploration;
        Message = string.Empty;
        StateManager = new GameStateManager();
        Party = new PartyController(World);
        Recorder = recorder ?? new GameplayRecorder();
        StartNewExpeditionInternal(World.FloorSeed);
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
            Recorder.Record(
                GameplayEventType.Movement,
                StateManager.ActiveExpedition.CurrentFloor,
                StateManager.ActiveExpedition.TurnCount,
                position.X,
                position.Y,
                Party.LeaderId,
                value: 1,
                context: direction.ToString());
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
                Recorder.Record(
                    GameplayEventType.Interaction,
                    StateManager.ActiveExpedition.CurrentFloor,
                    StateManager.ActiveExpedition.TurnCount,
                    leaderPosition.X,
                    leaderPosition.Y,
                    Party.LeaderId,
                    value: chest.Reward.Gold,
                    context: "chest");
                Recorder.Record(
                    GameplayEventType.RewardCollected,
                    StateManager.ActiveExpedition.CurrentFloor,
                    StateManager.ActiveExpedition.TurnCount,
                    leaderPosition.X,
                    leaderPosition.Y,
                    Party.LeaderId,
                    value: chest.Reward.Gold,
                    context: "chest");
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
                Recorder.Record(
                    GameplayEventType.Interaction,
                    StateManager.ActiveExpedition.CurrentFloor,
                    StateManager.ActiveExpedition.TurnCount,
                    leaderPosition.X,
                    leaderPosition.Y,
                    Party.LeaderId,
                    value: terminal.HealAmount,
                    context: "terminal");
                Recorder.Record(
                    GameplayEventType.RewardCollected,
                    StateManager.ActiveExpedition.CurrentFloor,
                    StateManager.ActiveExpedition.TurnCount,
                    leaderPosition.X,
                    leaderPosition.Y,
                    Party.LeaderId,
                    value: terminal.CoreReward.Charge,
                    context: "terminal-core");
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

    public void ToggleDevMenu()
    {
        if (State == GameState.Battle)
            return;

        devMenuOpen = !devMenuOpen;
        if (devMenuOpen)
            devSelectedFloor = DevMenuState.NormalizeFloor(StateManager.ActiveExpedition.CurrentFloor);
    }

    public void CloseDevMenu() => devMenuOpen = false;

    public void AdjustDevFloor(int delta)
    {
        if (!devMenuOpen)
            return;

        devSelectedFloor = DevMenuState.NormalizeFloor(devSelectedFloor + delta);
    }

    public void JumpToDevFloor()
    {
        if (!devMenuOpen)
            return;

        int floor = DevMenuState.NormalizeFloor(devSelectedFloor);
        int level = DevMenuState.PartyLevelForFloor(floor);

        ConfigureDevRoster(level, floor);

        World.RebuildFloor(Random.Shared.Next(), floor);
        StateManager.StartNewExpedition(
            World.SpawnX,
            World.SpawnY,
            1,
            1,
            floor,
            World.FloorSeed,
            StateManager.Campaign.PartyRoster.Take(4).Select(member => member.Id).ToArray(),
            StateManager.Campaign.PartyRoster[0].Id,
            PartyFormationType.Column,
            (x, y) => VisibilitySystem.IsVisible(
                World,
                new GridPosition(World.SpawnX, World.SpawnY),
                new GridPosition(x, y)));

        foreach (PartyMember member in StateManager.ActiveExpedition.Party)
        {
            PartyMember source = StateManager.Campaign.PartyRoster.First(candidate => candidate.Id == member.Id);
            member.Level = source.Level;
            member.Experience = source.Experience;
            member.MaxHP = source.MaxHP;            member.HP = source.MaxHP;
            member.MaxMP = source.MaxMP;
            member.MP = source.MaxMP;
            member.Stats = new StatsData
            {
                Strength = source.Stats.Strength,
                Magic = source.Stats.Magic,
                Agility = source.Stats.Agility,
                Luck = source.Stats.Luck
            };
            member.Morale = 100;
            member.EquippedGearIds = new List<string>(source.EquippedGearIds);
        }

        StateManager.ActiveExpedition.Health = StateManager.ActiveExpedition.Party.First().HP;
        StateManager.ActiveExpedition.MaxHealth = StateManager.ActiveExpedition.Party.First().MaxHP;

        Party.Initialize(
            StateManager.ActiveExpedition,
            new GridPosition(World.SpawnX, World.SpawnY),
            StateManager.ActiveExpedition.LeaderId,
            PartyFormationType.Column);

        SyncCompatibilityPlayer();
        SynchronizeExpedition();
        RecordNodeVisit(World.SpawnX, World.SpawnY);
        UpdateVisibility();
        ApplyGearBonuses();

        State = GameState.Exploration;
        Battle = null;
        feedback = null;
        landmarkIntroduced = false;
        devMenuOpen = false;
        Message = $"DEV JUMP: depth {floor}, party level {level}.";
    }

    public void SelectPreviousBattleCommand() => Battle?.SelectPreviousCommand();
    public void SelectNextBattleCommand() => Battle?.SelectNextCommand();
    public void SelectPreviousBattleTarget() => Battle?.SelectPreviousTarget();
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

        string actorId = Battle.SelectedActor?.Id ?? string.Empty;
        string targetId = Battle.SelectedTarget == null
            ? string.Empty
            : Battle.GetEnemyPresentationId(Battle.SelectedTarget);
        BattleCommand command = Battle.SelectedCommand;

        Recorder.Record(
            GameplayEventType.BattleCommand,
            StateManager.ActiveExpedition.CurrentFloor,
            StateManager.ActiveExpedition.TurnCount,
            Party.LeaderPosition.X,
            Party.LeaderPosition.Y,
            actorId,
            string.IsNullOrEmpty(targetId) ? null : targetId,
            context: command.ToString());

        if (command == BattleCommand.Skill)
            Recorder.Record(
                GameplayEventType.AbilityUsed,
                StateManager.ActiveExpedition.CurrentFloor,
                StateManager.ActiveExpedition.TurnCount,
                Party.LeaderPosition.X,
                Party.LeaderPosition.Y,
                actorId,
                string.IsNullOrEmpty(targetId) ? null : targetId,
                context: "signature-skill");

        BattleResult result = Battle.PerformPlayerTurn(out _, out int enemyDamage);
        SynchronizeExpedition();
        Message = Battle.CommandMessage;

        if (enemyDamage > 0)
            Recorder.Record(
                GameplayEventType.DamageTaken,
                StateManager.ActiveExpedition.CurrentFloor,
                StateManager.ActiveExpedition.TurnCount,
                Party.LeaderPosition.X,
                Party.LeaderPosition.Y,
                value: enemyDamage,
                context: "battle");

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
                Recorder.Record(
                    GameplayEventType.EnemyDefeated,
                    StateManager.ActiveExpedition.CurrentFloor,
                    StateManager.ActiveExpedition.TurnCount,
                    defeatedEnemy.X,
                    defeatedEnemy.Y,
                    targetId: Battle.GetEnemyPresentationId(defeatedEnemy),
                    value: reward.Experience,
                    context: defeatedEnemy.Name);
                Recorder.Record(
                    GameplayEventType.RewardCollected,
                    StateManager.ActiveExpedition.CurrentFloor,
                    StateManager.ActiveExpedition.TurnCount,
                    defeatedEnemy.X,
                    defeatedEnemy.Y,
                    value: reward.Experience,
                    context: $"combat:{defeatedEnemy.Name}:gold={reward.Gold}:cores={reward.Cores.Sum(core => core.Quantity)}");
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
            Recorder.Record(
                GameplayEventType.Defeat,
                StateManager.ActiveExpedition.CurrentFloor,
                StateManager.ActiveExpedition.TurnCount,
                Party.LeaderPosition.X,
                Party.LeaderPosition.Y,
                Party.LeaderId,
                context: "battle");
            Recorder.Record(
                GameplayEventType.ExpeditionEnded,
                StateManager.ActiveExpedition.CurrentFloor,
                StateManager.ActiveExpedition.TurnCount,
                Party.LeaderPosition.X,
                Party.LeaderPosition.Y,
                Party.LeaderId,
                context: "defeated");
            return;
        }

        if (result == BattleResult.Escaped)
        {
            MoraleSystem.ApplyEvent(StateManager.ActiveExpedition, MoraleEventType.Retreat);
            Recorder.Record(
                GameplayEventType.ExtractionChosen,
                StateManager.ActiveExpedition.CurrentFloor,
                StateManager.ActiveExpedition.TurnCount,
                Party.LeaderPosition.X,
                Party.LeaderPosition.Y,
                Party.LeaderId,
                context: "battle-retreat");
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
        Recorder.Clear();
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

        Recorder.Record(
            GameplayEventType.ExtractionChosen,
            expedition.CurrentFloor,
            expedition.TurnCount,
            Party.LeaderPosition.X,
            Party.LeaderPosition.Y,
            Party.LeaderId,
            value: expedition.CarriedGold,
            context: $"gold={expedition.CarriedGold};cores={expedition.CarriedCores.Count};materials={expedition.CarriedMaterials.Sum(material => material.Quantity)}");
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
        Recorder.Record(
            GameplayEventType.ExpeditionEnded,
            depth,
            expedition.TurnCount,
            Party.LeaderPosition.X,
            Party.LeaderPosition.Y,
            Party.LeaderId,
            context: "extracted");
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

    private void ConfigureDevRoster(int level, int floor)
    {
        int levelDelta = Math.Max(0, level - 1);
        int weaponPower = 3 + levelDelta;
        int armorPower = 2 + levelDelta;
        int ringPower = Math.Max(1, 1 + levelDelta / 2);

        string[] slots = { "Weapon", "Armor", "Ring" };
        string prefix = "dev-";

        StateManager.Campaign.Gear.RemoveAll(gear =>
            gear.Id.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

        foreach (PartyMember member in StateManager.Campaign.PartyRoster.Take(4))
        {
            (int baseMaxHp, int baseStrength, int baseMagic, int baseAgility, int baseLuck) =
                member.SpriteId.ToLowerInvariant() switch
                {
                    "lyra" => (24, 4, 9, 7, 6),
                    "marek" => (36, 10, 2, 3, 4),
                    "sera" => (26, 6, 7, 9, 8),
                    _ => (30, 8, 3, 6, 5)
                };

            member.Level = level;
            member.Experience = 0;
            member.MaxHP = baseMaxHp + levelDelta * 4;
            member.HP = member.MaxHP;
            member.MaxMP = 10 + levelDelta;
            member.MP = member.MaxMP;
            member.Stats = new StatsData
            {
                Strength = baseStrength + levelDelta,
                Magic = baseMagic + levelDelta,
                Agility = baseAgility + levelDelta,
                Luck = baseLuck + Math.Min(levelDelta, 10)
            };
            member.Morale = 100;
            member.EquippedGearIds.Clear();

            AddDevGear(member, "Weapon", weaponPower, floor);
            AddDevGear(member, "Armor", armorPower, floor);
            AddDevGear(member, "Ring", ringPower, floor);
        }
    }

    private void AddDevGear(PartyMember member, string slot, int power, int floor)
    {
        string safeMember = member.Id.Replace(" ", string.Empty, StringComparison.Ordinal);
        string id = $"dev-{floor}-{safeMember}-{slot.ToLowerInvariant()}";
        string name = $"Dev {slot} +{power}";

        StateManager.Campaign.Gear.Add(new Gear
        {
            Id = id,
            Name = name,
            Slot = slot,
            Power = power
        });

        member.EquippedGearIds.Add(id);
    }

    private void StartNewExpeditionInternal(int? seed = null)
    {
        World.RebuildFloor(seed ?? Random.Shared.Next(), 1);
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
        Recorder.Record(
            GameplayEventType.ExpeditionStarted,
            StateManager.ActiveExpedition.CurrentFloor,
            StateManager.ActiveExpedition.TurnCount,
            World.SpawnX,
            World.SpawnY,
            StateManager.ActiveExpedition.LeaderId,
            value: World.FloorSeed,
            context: "new-expedition");
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
        Recorder.Record(
            GameplayEventType.BattleStarted,
            StateManager.ActiveExpedition.CurrentFloor,
            StateManager.ActiveExpedition.TurnCount,
            enemy.X,
            enemy.Y,
            Party.LeaderId,
            targetId: enemy.Name,
            context: $"enemies={battleEnemies.Count}");
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
        Recorder.Record(
            GameplayEventType.FloorDescended,
            expedition.CurrentFloor,
            expedition.TurnCount,
            World.SpawnX,
            World.SpawnY,
            Party.LeaderId,
            value: expedition.CurrentFloor);
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
        bool firstVisit = !StateManager.ActiveExpedition.NodeHistory.Contains(node.Id);
        if (firstVisit)
            StateManager.ActiveExpedition.NodeHistory.Add(node.Id);
        if (firstVisit)
            Recorder.Record(
                GameplayEventType.Discovery,
                StateManager.ActiveExpedition.CurrentFloor,
                StateManager.ActiveExpedition.TurnCount,
                x,
                y,
                Party.LeaderId,
                context: $"{node.Type}:{node.Id}");
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