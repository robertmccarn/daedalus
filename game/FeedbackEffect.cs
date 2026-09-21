public enum FeedbackEffectType
{
    Discovery,
    Loot,
    Heal,
    Damage,
    Victory,
    Extraction,
    Danger
}

public sealed record FeedbackEffect(
    FeedbackEffectType Type,
    int X,
    int Y,
    long StartedAt,
    int DurationMs)
{
    public bool IsActive(long now) => now - StartedAt < DurationMs;
}
