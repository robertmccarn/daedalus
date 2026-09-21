using Systemic.Engine.State;

public enum BattleResult
{
    Continue,
    EnemyDefeated,
    PlayerDefeated,
    Escaped
}

public enum BattlePhase
{
    PlayerCommand,
    PlayerAction,
    EnemyAction,
    Victory,
    Defeat,
    Escaped
}

public sealed class BattleSystem
{
    private readonly ExpeditionState expedition;
    private readonly HashSet<string> defendingActors = new(StringComparer.Ordinal);
    private readonly List<Character> defeatedEnemies = new();
    private readonly Dictionary<Character, string> enemyIds = new();
    private readonly Dictionary<string, int> poisonTurns = new(StringComparer.Ordinal);
    private int turnCursor;

    public BattleState State { get; }
    public IReadOnlyList<PartyMember> Party => State.Party;
    public IReadOnlyList<Character> Enemies { get; }
    public IReadOnlyList<Character> DefeatedEnemies => defeatedEnemies;

    public BattleTurn CurrentTurn { get; private set; } = BattleTurn.Player;
    public BattleCommand SelectedCommand { get; private set; } = BattleCommand.Attack;
    public BattlePhase CurrentPhase { get; private set; } = BattlePhase.PlayerCommand;
    public bool IsFinished { get; private set; }
    public bool PlayerWon { get; private set; }
    public string CommandMessage { get; private set; } = "Choose an action.";

    public PartyMember? SelectedActor =>
        State.Party.FirstOrDefault(member =>
            member.Id == State.SelectedActorId && member.HP > 0);

    public Character? SelectedTarget =>
        Enemies.FirstOrDefault(enemy =>
            GetEnemyId(enemy) == State.SelectedTargetId && enemy.HP > 0);

    public BattleSystem(ExpeditionState expeditionState, IReadOnlyList<Character> enemies)
    {
        expedition = expeditionState;

        if (expeditionState.Party.Count == 0)
            throw new InvalidOperationException("Battle requires at least one party member.");
        if (enemies.Count == 0)
            throw new InvalidOperationException("Battle requires at least one enemy.");

        Enemies = enemies.ToList();

        for (int index = 0; index < Enemies.Count; index++)
            enemyIds[Enemies[index]] = $"{Enemies[index].Name}:{Enemies[index].X}:{Enemies[index].Y}:{index}";

        State = new BattleState
        {
            Party = expeditionState.Party.ToList(),
            Enemies = Enemies.Select(ToEnemyState).ToList(),
            SelectedCommand = BattleCommand.Attack.ToString(),
            Description = "Choose an action."
        };

        BattlePartyController controller = new(State);
        State.SelectedActorId = controller.State.TurnOrder
            .FirstOrDefault(IsLivingPartyMember) ?? State.Party[0].Id;

        State.SelectedTargetId = GetEnemyId(Enemies[0]);
        turnCursor = Math.Max(0, State.TurnOrder.IndexOf(State.SelectedActorId));
        CommandMessage = $"{SelectedActor?.Name ?? "Party"} is ready.";
    }

    public void SelectNextCommand()
    {
        int next = ((int)SelectedCommand + 1) % 6;
        SelectedCommand = (BattleCommand)next;
        State.SelectedCommand = SelectedCommand.ToString();
        CommandMessage = GetCommandDescription();
        State.Description = CommandMessage;
    }

    public void SelectPreviousCommand()
    {
        int previous = (int)SelectedCommand - 1;
        if (previous < 0) previous = 5;
        SelectedCommand = (BattleCommand)previous;
        State.SelectedCommand = SelectedCommand.ToString();
        CommandMessage = GetCommandDescription();
        State.Description = CommandMessage;
    }

    public void SelectNextTarget()
    {
        List<Character> living = Enemies.Where(enemy => enemy.HP > 0).ToList();
        if (living.Count == 0) return;

        int current = living.FindIndex(enemy => GetEnemyId(enemy) == State.SelectedTargetId);
        int next = current < 0 ? 0 : (current + 1) % living.Count;
        State.SelectedTargetId = GetEnemyId(living[next]);
        CommandMessage = $"Target: {living[next].Name}.";
        State.Description = CommandMessage;
    }

    public BattleResult PerformPlayerTurn(out int playerDamage, out int enemyDamage)
    {
        playerDamage = 0;
        enemyDamage = 0;

        if (IsFinished)
            return GetBattleResult();

        PartyMember? actor = SelectedActor;
        Character? target = SelectedTarget;
        if (actor == null || target == null)
        {
            CommandMessage = "No valid combatant or target remains.";
            return BattleResult.Continue;
        }

        CurrentPhase = BattlePhase.PlayerAction;
        CurrentTurn = BattleTurn.Player;

        switch (SelectedCommand)
        {
            case BattleCommand.Attack:
                playerDamage = DealPlayerDamage(actor, target, 5, "strikes");
                break;

            case BattleCommand.Skill:
                if (actor.MP < 2)
                {
                    CommandMessage = $"{actor.Name} lacks the MP to use a skill.";
                    return BattleResult.Continue;
                }

                actor.MP -= 2;
                playerDamage = DealPlayerDamage(actor, target, 7, "channels");
                if (target.HP > 0)
                    ApplyStatus(target, "Poisoned", 2);
                break;

            case BattleCommand.Item:
                if (!TryUseHealingItem(actor))
                {
                    CommandMessage = "No healing item is available.";
                    return BattleResult.Continue;
                }
                CommandMessage = $"{actor.Name} uses a healing item.";
                break;

            case BattleCommand.Interact:
                CommandMessage = "The battle space offers no interaction.";
                return BattleResult.Continue;

            case BattleCommand.Defend:
                defendingActors.Add(actor.Id);
                SetStatus(actor.Id, "Guarded");
                CommandMessage = $"{actor.Name} braces for the incoming attack.";
                break;

            case BattleCommand.Run:
                IsFinished = true;
                PlayerWon = false;
                CurrentPhase = BattlePhase.Escaped;
                CommandMessage = $"{actor.Name} withdrew from the encounter.";
                return BattleResult.Escaped;
        }

        SyncEnemyState();

        if (LivingEnemies().Count == 0)
        {
            IsFinished = true;
            PlayerWon = true;
            CurrentPhase = BattlePhase.Victory;
            CurrentTurn = BattleTurn.Player;
            CommandMessage += " The hostile formation collapses.";
            return BattleResult.EnemyDefeated;
        }

        enemyDamage = PerformEnemyPhase();

        if (LivingEnemies().Count == 0)
        {
            IsFinished = true;
            PlayerWon = true;
            CurrentPhase = BattlePhase.Victory;
            CurrentTurn = BattleTurn.Player;
            CommandMessage += " The last hostile succumbs.";
            return BattleResult.EnemyDefeated;
        }

        if (LivingParty().Count == 0)
        {
            IsFinished = true;
            PlayerWon = false;
            CurrentPhase = BattlePhase.Defeat;
            CommandMessage += " The expedition is overwhelmed.";
            return BattleResult.PlayerDefeated;
        }

        AdvanceToNextPartyActor();
        CurrentPhase = BattlePhase.PlayerCommand;
        CurrentTurn = BattleTurn.Player;
        return BattleResult.Continue;
    }

    private int DealPlayerDamage(PartyMember actor, Character target, int power, string verb)
    {
        int damage = Math.Max(1, actor.Stats.Strength + power);
        target.TakeDamage(damage);
        CommandMessage = $"{actor.Name} {verb} {target.Name} for {damage} damage.";

        if (target.HP <= 0)
        {
            if (!defeatedEnemies.Contains(target))
                defeatedEnemies.Add(target);
            CommandMessage += $" {target.Name} falls.";
        }

        return damage;
    }

    private int PerformEnemyPhase()
    {
        CurrentPhase = BattlePhase.EnemyAction;
        CurrentTurn = BattleTurn.Enemy;
        int totalDamage = 0;
        int statusDamage = ApplyPoisonDamage();

        foreach (string actorId in State.TurnOrder)
        {
            if (IsLivingPartyMember(actorId))
                continue;

            Character? enemy = Enemies.FirstOrDefault(candidate =>
                GetEnemyId(candidate) == actorId && candidate.HP > 0);

            if (enemy == null)
                continue;

            PartyMember? target = LivingParty()
                .OrderBy(member => member.HP)
                .ThenBy(member => member.Id, StringComparer.Ordinal)
                .FirstOrDefault();

            if (target == null)
                break;

            int defense = defendingActors.Contains(target.Id) ? 2 : 0;
            int damage = Math.Max(0, enemy.Stats.Strength + 3 - defense);
            target.HP = Math.Max(0, target.HP - damage);
            totalDamage += damage;

            CommandMessage = $"{enemy.Name} hits {target.Name} for {damage} damage.";
            if (target.HP == 0)
                CommandMessage += $" {target.Name} is down.";
        }

        if (statusDamage > 0)
            CommandMessage += $" Poison deals {statusDamage} damage.";

        foreach (string actorId in defendingActors)
            ClearStatus(actorId, "Guarded");

        defendingActors.Clear();
        return totalDamage + statusDamage;
    }

    private void AdvanceToNextPartyActor()
    {
        if (State.TurnOrder.Count == 0) return;

        for (int offset = 1; offset <= State.TurnOrder.Count; offset++)
        {
            int index = (turnCursor + offset) % State.TurnOrder.Count;
            string id = State.TurnOrder[index];
            if (IsLivingPartyMember(id))
            {
                turnCursor = index;
                State.TurnIndex = index;
                State.SelectedActorId = id;
                return;
            }
        }
    }

    private bool IsLivingPartyMember(string id) =>
        State.Party.Any(member => member.Id == id && member.HP > 0);

    private List<PartyMember> LivingParty() =>
        State.Party.Where(member => member.HP > 0).ToList();

    private List<Character> LivingEnemies() =>
        Enemies.Where(enemy => enemy.HP > 0).ToList();

    private int ApplyPoisonDamage()
    {
        int damage = 0;

        foreach (Character enemy in LivingEnemies().ToList())
        {
            string id = GetEnemyId(enemy);
            if (!poisonTurns.TryGetValue(id, out int turns) || turns <= 0)
                continue;

            enemy.TakeDamage(2);
            damage += 2;
            turns--;
            if (turns == 0)
            {
                poisonTurns.Remove(id);
                ClearStatus(id, "Poisoned");
            }
            else
            {
                poisonTurns[id] = turns;
            }
        }

        SyncEnemyState();
        return damage;
    }

    private void ApplyStatus(Character enemy, string status, int turns)
    {
        string id = GetEnemyId(enemy);
        if (status == "Poisoned")
            poisonTurns[id] = Math.Max(turns, poisonTurns.TryGetValue(id, out int current) ? current : 0);

        if (!State.StatusEffects.TryGetValue(id, out List<string>? statuses))
            State.StatusEffects[id] = statuses = new List<string>();

        if (!statuses.Contains(status, StringComparer.Ordinal))
            statuses.Add(status);
    }

    private void SetStatus(string actorId, string status)
    {
        if (!State.StatusEffects.TryGetValue(actorId, out List<string>? statuses))
            State.StatusEffects[actorId] = statuses = new List<string>();

        if (!statuses.Contains(status, StringComparer.Ordinal))
            statuses.Add(status);
    }

    private void ClearStatus(string actorId, string status)
    {
        if (!State.StatusEffects.TryGetValue(actorId, out List<string>? statuses))
            return;

        statuses.Remove(status);
        if (statuses.Count == 0)
            State.StatusEffects.Remove(actorId);
    }

    public bool HasStatus(string combatantId, string status) =>
        State.StatusEffects.TryGetValue(combatantId, out List<string>? statuses) &&
        statuses.Contains(status, StringComparer.Ordinal);

    public bool HasStatus(PartyMember member, string status) =>
        HasStatus(member.Id, status);

    public bool HasStatus(Character enemy, string status) =>
        HasStatus(GetEnemyId(enemy), status);

    private bool TryUseHealingItem(PartyMember actor)
    {
        string[] preferred = { "healing-tonic", "field-ration" };
        foreach (string itemId in preferred)
        {
            if (!InventorySystem.RemoveItem(expedition, itemId))
                continue;

            int amount = itemId == "healing-tonic" ? 14 : 8;
            actor.HP = Math.Min(actor.MaxHP, actor.HP + amount);
            return true;
        }

        return false;
    }

    private BattleEnemyState ToEnemyState(Character enemy) =>
        new()
        {
            Id = GetEnemyId(enemy),
            Name = enemy.Name,
            HP = enemy.HP,
            MaxHP = enemy.MAXHP,
            Agility = enemy.Stats.Agility,
            Family = "Hollow"
        };

    private string GetEnemyId(Character enemy) =>
        enemyIds[enemy];

    private void SyncEnemyState()
    {
        foreach (BattleEnemyState enemyState in State.Enemies)
        {
            Character? enemy = Enemies.FirstOrDefault(candidate => GetEnemyId(candidate) == enemyState.Id);
            if (enemy == null) continue;
            enemyState.HP = enemy.HP;
            enemyState.MaxHP = enemy.MAXHP;
        }
    }

    private string GetCommandDescription() => SelectedCommand switch
    {
        BattleCommand.Attack => "Strike the selected hostile.",
        BattleCommand.Skill => "Spend 2 MP for a stronger attack.",
        BattleCommand.Item => "Consume a healing field item.",
        BattleCommand.Interact => "Check the battle space.",
        BattleCommand.Defend => "Brace to reduce incoming damage.",
        BattleCommand.Run => "Withdraw from the encounter.",
        _ => "Choose an action."
    };

    private BattleResult GetBattleResult()
    {
        if (!IsFinished) return BattleResult.Continue;
        return PlayerWon ? BattleResult.EnemyDefeated : BattleResult.PlayerDefeated;
    }
}
