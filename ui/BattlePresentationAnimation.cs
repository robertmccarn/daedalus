public enum BattleAnimationKind
{
    Attack,
    Skill,
    EnemyAttack,
    Defend,
    Item,
    PoisonTick,
    Hit,
    Expose,
    Defeat,
    Victory
}

public readonly record struct BattleAnimationEvent(
    long Sequence,
    BattleAnimationKind Kind,
    string ActorId,
    string? TargetId,
    int Damage,
    long DurationMs,
    long StartedAt);

public sealed class BattleAnimationQueue
{
    private readonly Queue<BattleAnimationEvent> pending = new();
    private BattleAnimationEvent? active;

    public bool HasPending => active.HasValue || pending.Count > 0;

    public void Enqueue(
        BattleAnimationKind kind,
        string actorId,
        string? targetId,
        int damage,
        int durationMs)
    {
        pending.Enqueue(new BattleAnimationEvent(
            Sequence: DateTime.UtcNow.Ticks,
            Kind: kind,
            ActorId: actorId,
            TargetId: targetId,
            Damage: damage,
            DurationMs: durationMs,
            StartedAt: 0));
    }

    public BattleAnimationEvent? GetActive(long now)
    {
        while (true)
        {
            if (!active.HasValue)
            {
                if (pending.Count == 0)
                    return null;

                BattleAnimationEvent next = pending.Dequeue();
                active = next with { StartedAt = now };
            }

            BattleAnimationEvent current = active.Value;
            if (!AnimationClock.IsComplete(now, current.StartedAt, (int)current.DurationMs))
                return current;

            active = null;
        }
    }
}
