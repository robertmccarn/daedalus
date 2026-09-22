public enum GameplayEventType
{
    ExpeditionStarted,
    Movement,
    Discovery,
    Interaction,
    ExplorationEvent,
    BattleStarted,
    BattleCommand,
    AbilityUsed,
    DamageTaken,
    EnemyDefeated,
    RewardCollected,
    FloorDescended,
    ExtractionChosen,
    ExpeditionEnded,
    Defeat
}

public readonly record struct GameplayEvent(
    GameplayEventType Type,
    int Floor,
    int Turn,
    int X,
    int Y,
    string? ActorId = null,
    string? TargetId = null,
    int Value = 0,
    string? Context = null);
