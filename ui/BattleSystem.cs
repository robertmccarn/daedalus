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
    private readonly CampaignState? campaign;
    private readonly List<Character> defeatedEnemies = new();
    private readonly Dictionary<Character, string> enemyIds = new();
    private readonly Dictionary<string, int> poisonTurns = new(StringComparer.Ordinal);
    private readonly HashSet<string> guardedActors = new(StringComparer.Ordinal);
    private readonly BattleAnimationQueue animationQueue = new();
    private int selectedCommandIndex;

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

    public BattleAnimationEvent? GetActiveAnimation(long now) =>
        animationQueue.GetActive(now);

    public PartyMember? GetPartyMember(string id) =>
        State.Party.FirstOrDefault(member => member.Id == id);

    public Character? GetEnemyByPresentationId(string id) =>
        Enemies.FirstOrDefault(enemy => GetEnemyId(enemy) == id);

    public string GetEnemyPresentationId(Character enemy) => GetEnemyId(enemy);

    public PartyMember? SelectedActor =>
        IsCurrentPartyActor
            ? State.Party.FirstOrDefault(member =>
                member.Id == State.SelectedActorId && member.HP > 0)
            : null;

    public Character? SelectedTarget =>
        Enemies.FirstOrDefault(enemy =>
            GetEnemyId(enemy) == State.SelectedTargetId && enemy.HP > 0);

    private bool IsCurrentPartyActor =>
        State.Party.Any(member => member.Id == State.SelectedActorId && member.HP > 0);

    public BattleSystem(
        ExpeditionState expeditionState,
        IReadOnlyList<Character> enemies,
        CampaignState? campaignState = null)
    {
        expedition = expeditionState;
        campaign = campaignState;

        if (expeditionState.Party.Count == 0)
            throw new InvalidOperationException("Battle requires at least one party member.");
        if (enemies.Count == 0)
            throw new InvalidOperationException("Battle requires at least one enemy.");

        Enemies = enemies.ToList();

        for (int index = 0; index < Enemies.Count; index++)
            enemyIds[Enemies[index]] =
                $"{Enemies[index].Name}:{Enemies[index].X}:{Enemies[index].Y}:{index}";

        State = new BattleState
        {
            Party = expeditionState.Party.ToList(),
            Enemies = Enemies.Select(ToEnemyState).ToList(),
            SelectedCommand = BattleCommand.Attack.ToString(),
            Description = "Choose an action.",
            Round = 1
        };

        BattlePartyController controller = new(State);
        State.TurnIndex = Math.Max(0, State.TurnOrder.IndexOf(State.SelectedActorId));
        EnsureValidTarget();
        SynchronizeCurrentActor();

        CommandMessage = $"{SelectedActor?.Name ?? "Party"} is ready.";
        State.Description = CommandMessage;
    }

    public void SelectNextCommand()
    {
        if (IsFinished || !IsCurrentPartyActor || CurrentPhase != BattlePhase.PlayerCommand)
            return;

        selectedCommandIndex = (selectedCommandIndex + 1) % Enum.GetValues<BattleCommand>().Length;
        SelectedCommand = (BattleCommand)selectedCommandIndex;
        State.SelectedCommand = SelectedCommand.ToString();
        CommandMessage = GetCommandDescription();
        State.Description = CommandMessage;
    }

    public void SelectPreviousCommand()
    {
        if (IsFinished || !IsCurrentPartyActor || CurrentPhase != BattlePhase.PlayerCommand)
            return;

        selectedCommandIndex--;
        if (selectedCommandIndex < 0)
            selectedCommandIndex = Enum.GetValues<BattleCommand>().Length - 1;

        SelectedCommand = (BattleCommand)selectedCommandIndex;
        State.SelectedCommand = SelectedCommand.ToString();
        CommandMessage = GetCommandDescription();
        State.Description = CommandMessage;
    }

    public void SelectNextTarget()
    {
        if (IsFinished || !IsCurrentPartyActor)
            return;

        List<Character> living = LivingEnemies();
        if (living.Count == 0)
            return;

        int current = living.FindIndex(enemy => GetEnemyId(enemy) == State.SelectedTargetId);
        int next = current < 0 ? 0 : (current + 1) % living.Count;
        State.SelectedTargetId = GetEnemyId(living[next]);
        CommandMessage = $"Target: {living[next].Name}.";
        State.Description = CommandMessage;
    }

    public void SelectPreviousTarget()
    {
        if (IsFinished || !IsCurrentPartyActor)
            return;

        List<Character> living = LivingEnemies();
        if (living.Count == 0)
            return;

        int current = living.FindIndex(enemy => GetEnemyId(enemy) == State.SelectedTargetId);
        int previous = current <= 0 ? living.Count - 1 : current - 1;
        State.SelectedTargetId = GetEnemyId(living[previous]);
        CommandMessage = $"Target: {living[previous].Name}.";
        State.Description = CommandMessage;
    }

    public BattleResult PerformPlayerTurn(out int playerDamage, out int enemyDamage)
    {
        playerDamage = 0;
        enemyDamage = 0;

        if (IsFinished)
            return GetBattleResult();

        if (!IsCurrentPartyActor || CurrentPhase != BattlePhase.PlayerCommand)
        {
            CommandMessage = "It is not the party's turn.";
            return BattleResult.Continue;
        }

        PartyMember actor = State.Party.First(member =>
            member.Id == State.SelectedActorId && member.HP > 0);

        EnsureValidTarget();
        Character? target = SelectedTarget;

        if (SelectedCommand is BattleCommand.Attack or BattleCommand.Skill && target == null)
        {
            CommandMessage = "No living target remains.";
            return BattleResult.Continue;
        }

        CurrentPhase = BattlePhase.PlayerAction;
        CurrentTurn = BattleTurn.Player;

        BattleResult actionResult;
        switch (SelectedCommand)
        {
            case BattleCommand.Attack:
                playerDamage = ApplyEnemyDamage(
                    actor,
                    target!,
                    GetBasePhysicalDamage(actor, 5),
                    "strikes");
                QueuePlayerAnimation(BattleAnimationKind.Attack, actor, target!, playerDamage);
                if (target!.HP <= 0)
                    QueueAnimation(BattleAnimationKind.Defeat, actor.Id, GetEnemyId(target), playerDamage, 420);
                actionResult = EvaluateBattleOutcome();
                break;

            case BattleCommand.Skill:
                actionResult = PerformSkill(actor, target!, out playerDamage);
                break;

            case BattleCommand.Item:
                if (!TryUseHealingItem(actor))
                {
                    CommandMessage = "No healing item is available.";
                    return BattleResult.Continue;
                }

                CommandMessage = $"{actor.Name} uses a healing item.";
                State.Description = CommandMessage;
                animationQueue.Enqueue(
                    BattleAnimationKind.Item,
                    actor.Id,
                    actor.Id,
                    0,
                    420);
                actionResult = BattleResult.Continue;
                break;

            case BattleCommand.Interact:
                CommandMessage = "The battle space offers no interaction.";
                State.Description = CommandMessage;
                return BattleResult.Continue;

            case BattleCommand.Defend:
                guardedActors.Add(actor.Id);
                SetStatus(actor.Id, "Guarded");
                CommandMessage = $"{actor.Name} braces for the incoming attack.";
                State.Description = CommandMessage;
                animationQueue.Enqueue(
                    BattleAnimationKind.Defend,
                    actor.Id,
                    actor.Id,
                    0,
                    360);
                actionResult = BattleResult.Continue;
                break;

            case BattleCommand.Run:
                IsFinished = true;
                PlayerWon = false;
                CurrentPhase = BattlePhase.Escaped;
                CommandMessage = $"{actor.Name} withdrew from the encounter.";
                State.Description = CommandMessage;
                CurrentTurn = BattleTurn.Player;
                return BattleResult.Escaped;

            default:
                actionResult = BattleResult.Continue;
                break;
        }

        SyncEnemyState();

        if (actionResult != BattleResult.Continue || IsFinished)
            return actionResult;

        AdvanceToNextActor();

        while (!IsFinished && IsCurrentEnemyActor)
        {
            int actionDamage = PerformEnemyTurn();
            enemyDamage += actionDamage;

            BattleResult enemyResult = EvaluateBattleOutcome();
            if (enemyResult != BattleResult.Continue)
                return enemyResult;

            AdvanceToNextActor();
        }

        if (IsFinished)
            return GetBattleResult();

        CurrentPhase = BattlePhase.PlayerCommand;
        CurrentTurn = BattleTurn.Player;
        selectedCommandIndex = 0;
        SelectedCommand = BattleCommand.Attack;
        State.SelectedCommand = SelectedCommand.ToString();
        EnsureValidTarget();
        CommandMessage = $"{SelectedActor?.Name ?? "Party"} is ready.";
        State.Description = CommandMessage;

        return BattleResult.Continue;
    }

    private BattleResult PerformSkill(
        PartyMember actor,
        Character target,
        out int damage)
    {
        if (actor.MP < 2)
        {
            CommandMessage = $"{actor.Name} lacks the MP to use a skill.";
            State.Description = CommandMessage;
            damage = 0;
            return BattleResult.Continue;
        }

        actor.MP -= 2;

        SkillDefinition skill = GetSkillDefinition(actor);
        int rawDamage = skill.UseMagic
            ? GetBaseMagicDamage(actor, skill.Power)
            : GetBasePhysicalDamage(actor, skill.Power);

        damage = ApplyEnemyDamage(actor, target, rawDamage, skill.Verb);

        BattleAnimationKind animationKind = skill.AppliesExposed
            ? BattleAnimationKind.Expose
            : BattleAnimationKind.Skill;
        QueuePlayerAnimation(animationKind, actor, target, damage);

        if (target.HP <= 0)
            QueueAnimation(BattleAnimationKind.Defeat, actor.Id, GetEnemyId(target), damage, 420);

        if (target.HP > 0 && skill.AppliesPoison)
        {
            ApplyStatus(target, "Poisoned", 2);
            QueueAnimation(
                BattleAnimationKind.PoisonTick,
                actor.Id,
                GetEnemyId(target),
                0,
                320);
        }

        if (target.HP > 0 && skill.AppliesExposed)
            ApplyStatus(target, "Exposed", 1);

        return EvaluateBattleOutcome();
    }

    private int PerformEnemyTurn()
    {
        CurrentPhase = BattlePhase.EnemyAction;
        CurrentTurn = BattleTurn.Enemy;

        Character? enemy = GetCurrentEnemy();
        if (enemy == null)
            return 0;

        int statusDamage = ApplyEnemyStartOfTurnEffects(enemy);
        if (statusDamage > 0)
            QueueAnimation(
                BattleAnimationKind.PoisonTick,
                GetEnemyId(enemy),
                GetEnemyId(enemy),
                statusDamage,
                320);

        if (enemy.HP <= 0)
        {
            QueueAnimation(
                BattleAnimationKind.Defeat,
                GetEnemyId(enemy),
                GetEnemyId(enemy),
                statusDamage,
                420);
            SyncEnemyState();
            return statusDamage;
        }

        PartyMember? target = SelectEnemyTarget(enemy);
        if (target == null)
            return statusDamage;

        int basePower = GetEnemyPower(enemy);
        int damage = CalculateEnemyDamage(enemy, target, basePower);
        ApplyPartyDamage(enemy, target, damage);

        QueueAnimation(
            BattleAnimationKind.EnemyAttack,
            GetEnemyId(enemy),
            target.Id,
            damage,
            340);

        if (statusDamage > 0)
            CommandMessage += $" Poison deals {statusDamage} damage.";

        if (target.HP <= 0)
            QueueAnimation(
                BattleAnimationKind.Defeat,
                GetEnemyId(enemy),
                target.Id,
                damage,
                420);

        return statusDamage + damage;
    }

    private void ApplyPartyDamage(Character enemy, PartyMember target, int damage)
    {
        int previousHp = target.HP;
        target.HP = Math.Max(0, target.HP - damage);

        string guardText = guardedActors.Contains(target.Id) ? " through guard" : string.Empty;
        CommandMessage = $"{enemy.Name} hits {target.Name} for {damage} damage{guardText}.";

        if (previousHp > 0 && target.HP == 0)
        {
            ClearStatus(target.Id, "Guarded");
            guardedActors.Remove(target.Id);
            MoraleSystem.ApplyEvent(expedition, MoraleEventType.AllyDefeated);
            CommandMessage += $" {target.Name} is down.";
        }

        if (guardedActors.Contains(target.Id))
        {
            ClearStatus(target.Id, "Guarded");
            guardedActors.Remove(target.Id);
        }
    }

    private int ApplyEnemyDamage(
        PartyMember actor,
        Character target,
        int baseDamage,
        string verb)
    {
        int damage = Math.Max(1, baseDamage);

        if (HasStatus(target, "Exposed"))
        {
            damage += 4;
            ClearStatus(GetEnemyId(target), "Exposed");
        }

        target.TakeDamage(damage);
        CommandMessage = $"{actor.Name} {verb} {target.Name} for {damage} damage.";

        if (target.HP <= 0)
        {
            RegisterEnemyDefeat(target);
            CommandMessage += $" {target.Name} falls.";
        }

        return damage;
    }

    private void RegisterEnemyDefeat(Character enemy)
    {
        if (defeatedEnemies.Contains(enemy))
            return;

        defeatedEnemies.Add(enemy);
        poisonTurns.Remove(GetEnemyId(enemy));
        ClearStatus(GetEnemyId(enemy), "Poisoned");
        ClearStatus(GetEnemyId(enemy), "Exposed");
    }

    private int ApplyEnemyStartOfTurnEffects(Character enemy)
    {
        string id = GetEnemyId(enemy);
        if (!poisonTurns.TryGetValue(id, out int turns) || turns <= 0)
            return 0;

        int damage = 2;
        enemy.TakeDamage(damage);
        turns--;

        if (enemy.HP <= 0)
            RegisterEnemyDefeat(enemy);

        if (turns <= 0)
        {
            poisonTurns.Remove(id);
            ClearStatus(id, "Poisoned");
        }
        else
        {
            poisonTurns[id] = turns;
        }

        SyncEnemyState();
        return damage;
    }

    private void QueuePlayerAnimation(
        BattleAnimationKind kind,
        PartyMember actor,
        Character target,
        int damage)
    {
        animationQueue.Enqueue(
            kind,
            actor.Id,
            GetEnemyId(target),
            damage,
            kind == BattleAnimationKind.Skill || kind == BattleAnimationKind.Expose ? 560 : 340);
    }

    private void QueueAnimation(
        BattleAnimationKind kind,
        string actorId,
        string? targetId,
        int damage,
        int durationMs)
    {
        animationQueue.Enqueue(kind, actorId, targetId, damage, durationMs);
    }

    private void ApplyStatus(Character enemy, string status, int turns)
    {
        string id = GetEnemyId(enemy);

        if (status == "Poisoned")
            poisonTurns[id] = Math.Max(
                turns,
                poisonTurns.TryGetValue(id, out int current) ? current : 0);

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

    private void AdvanceToNextActor()
    {
        if (State.TurnOrder.Count == 0)
            return;

        int currentIndex = State.TurnIndex;

        for (int offset = 1; offset <= State.TurnOrder.Count; offset++)
        {
            int nextIndex = (currentIndex + offset) % State.TurnOrder.Count;
            string actorId = State.TurnOrder[nextIndex];

            if (!IsLivingCombatant(actorId))
                continue;

            if (nextIndex <= currentIndex)
                State.Round++;

            State.TurnIndex = nextIndex;
            State.SelectedActorId = actorId;
            SynchronizeCurrentActor();
            return;
        }
    }

    private void SynchronizeCurrentActor()
    {
        if (IsCurrentPartyActor)
        {
            CurrentTurn = BattleTurn.Player;
            return;
        }

        if (IsCurrentEnemyActor)
        {
            CurrentTurn = BattleTurn.Enemy;
            return;
        }

        CurrentTurn = BattleTurn.Player;
    }

    private bool IsLivingCombatant(string id) =>
        State.Party.Any(member => member.Id == id && member.HP > 0) ||
        Enemies.Any(enemy => GetEnemyId(enemy) == id && enemy.HP > 0);

    private bool IsCurrentEnemyActor =>
        Enemies.Any(enemy => GetEnemyId(enemy) == State.SelectedActorId && enemy.HP > 0);

    private Character? GetCurrentEnemy() =>
        Enemies.FirstOrDefault(enemy =>
            GetEnemyId(enemy) == State.SelectedActorId && enemy.HP > 0);

    private PartyMember? SelectEnemyTarget(Character enemy)
    {
        BattleEnemyState? state = State.Enemies.FirstOrDefault(candidate =>
            candidate.Id == GetEnemyId(enemy));

        IEnumerable<PartyMember> living = LivingParty();
        if (!living.Any())
            return null;

        return state?.Behavior switch
        {
            "Guard" => living
                .OrderByDescending(GetEffectiveDefense)
                .ThenByDescending(member => member.HP)
                .ThenBy(member => member.Id, StringComparer.Ordinal)
                .First(),

            "Brute" => living
                .OrderByDescending(member => member.MaxHP)
                .ThenBy(member => member.Id, StringComparer.Ordinal)
                .First(),

            _ => living
                .OrderBy(member => member.HP)
                .ThenBy(member => member.Id, StringComparer.Ordinal)
                .First()
        };
    }

    private int CalculateEnemyDamage(Character enemy, PartyMember target, int power)
    {
        int defense = GetEffectiveDefense(target);
        int damage = Math.Max(1, enemy.Stats.Strength + power - defense);

        if (guardedActors.Contains(target.Id))
            damage = Math.Max(0, damage - 4);

        return damage;
    }

    private int GetEffectiveDefense(PartyMember member)
    {
        int gearDefense = GetGearPower(member, "Armor", "Accessory");
        MoraleModifier morale = MoraleSystem.GetModifier(member);

        return Math.Max(
            0,
            (int)Math.Round((1 + gearDefense) * morale.DefenseMultiplier));
    }

    private int GetBasePhysicalDamage(PartyMember member, int power)
    {
        MoraleModifier morale = MoraleSystem.GetModifier(member);
        int weaponPower = GetGearPower(member, "Weapon");

        return Math.Max(
            1,
            (int)Math.Round(member.Stats.Strength * morale.AttackMultiplier)
            + power
            + weaponPower);
    }

    private int GetBaseMagicDamage(PartyMember member, int power)
    {
        MoraleModifier morale = MoraleSystem.GetModifier(member);

        return Math.Max(
            1,
            (int)Math.Round(member.Stats.Magic * morale.AttackMultiplier) + power);
    }

    private int GetGearPower(PartyMember member, params string[] slots)
    {
        if (campaign == null)
            return 0;

        int power = 0;
        foreach (string gearId in member.EquippedGearIds)
        {
            Gear? gear = campaign.Gear.FirstOrDefault(candidate => candidate.Id == gearId);
            if (gear == null)
                continue;

            if (slots.Any(slot =>
                gear.Slot.Equals(slot, StringComparison.OrdinalIgnoreCase)))
                power += gear.Power;
        }

        return power;
    }

    private SkillDefinition GetSkillDefinition(PartyMember member) =>
        member.SpriteId.ToLowerInvariant() switch
        {
            "arden" => new SkillDefinition("Break", 9, false, false, "breaks"),
            "lyra" => new SkillDefinition("Resonance", 8, true, true, "channels"),
            "marek" => new SkillDefinition("Crush", 11, false, false, "crushes"),
            "sera" => new SkillDefinition("Expose", 5, false, true, "exposes"),
            _ => new SkillDefinition("Skill", 7, false, false, "channels")
        };

    private int GetEnemyPower(Character enemy)
    {
        return State.Enemies.FirstOrDefault(state =>
            state.Id == GetEnemyId(enemy))?.Behavior switch
        {
            "Guard" => 3,
            "Stalker" => 4,
            "Brute" => 7,
            _ => 3
        };
    }

    private string InferEnemyBehavior(Character enemy)
    {
        string name = enemy.Name.ToLowerInvariant();

        if (name.Contains("brute"))
            return "Brute";
        if (name.Contains("stalker") || name.Contains("hound") || name.Contains("moth"))
            return "Stalker";
        if (name.Contains("guard") || name.Contains("warden") || name.Contains("sentinel"))
            return "Guard";

        return "Stalker";
    }

    private BattleResult EvaluateBattleOutcome()
    {
        if (LivingEnemies().Count == 0)
        {
            IsFinished = true;
            PlayerWon = true;
            CurrentPhase = BattlePhase.Victory;
            CurrentTurn = BattleTurn.Player;
            CommandMessage = string.IsNullOrWhiteSpace(CommandMessage)
                ? "The hostile formation collapses."
                : CommandMessage + " The hostile formation collapses.";
            State.Description = CommandMessage;
            return BattleResult.EnemyDefeated;
        }

        if (LivingParty().Count == 0)
        {
            IsFinished = true;
            PlayerWon = false;
            CurrentPhase = BattlePhase.Defeat;
            CommandMessage += " The expedition is overwhelmed.";
            State.Description = CommandMessage;
            return BattleResult.PlayerDefeated;
        }

        return BattleResult.Continue;
    }

    private void EnsureValidTarget()
    {
        List<Character> living = LivingEnemies();
        if (living.Count == 0)
        {
            State.SelectedTargetId = string.Empty;
            return;
        }

        if (living.Any(enemy => GetEnemyId(enemy) == State.SelectedTargetId))
            return;

        State.SelectedTargetId = GetEnemyId(living[0]);
    }

    private List<PartyMember> LivingParty() =>
        State.Party.Where(member => member.HP > 0).ToList();

    private List<Character> LivingEnemies() =>
        Enemies.Where(enemy => enemy.HP > 0).ToList();

    private BattleEnemyState ToEnemyState(Character enemy) =>
        new()
        {
            Id = GetEnemyId(enemy),
            Name = enemy.Name,
            HP = enemy.HP,
            MaxHP = enemy.MAXHP,
            Agility = enemy.Stats.Agility,
            Family = InferEnemyFamily(enemy),
            Behavior = InferEnemyBehavior(enemy)
        };

    private string InferEnemyFamily(Character enemy) =>
        enemy.Name.Contains("Ash", StringComparison.OrdinalIgnoreCase) ? "Ash" :
        enemy.Name.Contains("Crystal", StringComparison.OrdinalIgnoreCase) ? "Crystal" :
        enemy.Name.Contains("Verdant", StringComparison.OrdinalIgnoreCase) ? "Verdant" :
        "Hollow";

    private string GetEnemyId(Character enemy) => enemyIds[enemy];

    private void SyncEnemyState()
    {
        foreach (BattleEnemyState enemyState in State.Enemies)
        {
            Character? enemy = Enemies.FirstOrDefault(candidate =>
                GetEnemyId(candidate) == enemyState.Id);

            if (enemy == null)
                continue;

            enemyState.HP = enemy.HP;
            enemyState.MaxHP = enemy.MAXHP;
        }
    }

    private string GetCommandDescription() => SelectedCommand switch
    {
        BattleCommand.Attack => "Strike the selected hostile.",
        BattleCommand.Skill => $"Use {SelectedActor?.Name ?? "the actor"}'s signature skill.",
        BattleCommand.Item => "Consume a healing field item.",
        BattleCommand.Interact => "Check the battle space.",
        BattleCommand.Defend => "Brace to reduce incoming damage.",
        BattleCommand.Run => "Withdraw from the encounter.",
        _ => "Choose an action."
    };

    private BattleResult GetBattleResult()
    {
        if (!IsFinished)
            return BattleResult.Continue;

        return PlayerWon
            ? BattleResult.EnemyDefeated
            : BattleResult.PlayerDefeated;
    }

    private readonly record struct SkillDefinition(
        string Name,
        int Power,
        bool UseMagic,
        bool AppliesPoison,
        string Verb)
    {
        public bool AppliesExposed => Name.Equals("Expose", StringComparison.OrdinalIgnoreCase);
    }
}
