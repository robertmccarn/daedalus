using Systemic.Engine.State;

public sealed class BattleState
{
    public List<PartyMember> Party { get; set; } = new();
    public List<BattleEnemyState> Enemies { get; set; } = new();
    public string SelectedActorId { get; set; } = string.Empty;
    public string SelectedCommand { get; set; } = "Attack";
    public string SelectedTargetId { get; set; } = string.Empty;
    public string Description { get; set; } = "Choose an action.";
    public int TurnIndex { get; set; }
    public List<string> TurnOrder { get; set; } = new();
    public Dictionary<string, List<string>> StatusEffects { get; set; } = new();
}

public sealed class BattleEnemyState
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = string.Empty;
    public int HP { get; set; }
    public int MaxHP { get; set; }
    public int Agility { get; set; }
    public string Family { get; set; } = "Unknown";
}

public sealed class BattlePartyController
{
    public BattleState State { get; }

    public BattlePartyController(BattleState state)
    {
        State = state;
        RebuildTurnOrder();
    }

    public void RebuildTurnOrder()
    {
        State.TurnOrder = State.Party
            .Select(member => (member.Id, Agility: member.Stats.Agility))
            .Concat(State.Enemies.Select(enemy => (enemy.Id, enemy.Agility)))
            .OrderByDescending(x => x.Agility)
            .ThenBy(x => x.Id, StringComparer.Ordinal)
            .Select(x => x.Id)
            .ToList();

        State.SelectedActorId = State.TurnOrder.FirstOrDefault() ?? string.Empty;
    }

    public void SelectTarget(string enemyId)
    {
        if (State.Enemies.Any(enemy => enemy.Id == enemyId))
        {
            State.SelectedTargetId = enemyId;
            State.Description = $"Target: {State.Enemies.First(enemy => enemy.Id == enemyId).Name}.";
        }
    }
}

public sealed class EnemyFormation
{
    public List<BattleEnemyState> Enemies { get; } = new();

    public EnemyFormation(IEnumerable<BattleEnemyState> enemies) =>
        Enemies.AddRange(enemies);
}
