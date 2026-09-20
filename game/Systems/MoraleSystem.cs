using Systemic.Engine.State;

public enum MoraleEventType
{
    EnemyDefeated,
    LootFound,
    RareLootFound,
    AllyDefeated,
    AllyRevived,
    Retreat,
    PositiveEvent,
    NegativeEvent
}

public readonly record struct MoraleModifier(
    double AttackMultiplier,
    double DefenseMultiplier,
    double AgilityMultiplier);

public static class MoraleSystem
{
    public static void ApplyEvent(
        ExpeditionState expedition,
        MoraleEventType eventType)
    {
        int delta = eventType switch
        {
            MoraleEventType.EnemyDefeated => 3,
            MoraleEventType.LootFound => 2,
            MoraleEventType.RareLootFound => 5,
            MoraleEventType.AllyDefeated => -15,
            MoraleEventType.AllyRevived => 8,
            MoraleEventType.Retreat => -5,
            MoraleEventType.PositiveEvent => 5,
            MoraleEventType.NegativeEvent => -5,
            _ => 0
        };

        foreach (PartyMember member in expedition.Party)
            member.Morale = Math.Clamp(member.Morale + delta, 0, 100);
    }

    public static MoraleModifier GetModifier(PartyMember member)
    {
        double normalized = Math.Clamp(member.Morale, 0, 100) / 100.0;
        double swing = (normalized - 0.5) * 0.2;
        return new MoraleModifier(
            1.0 + swing,
            1.0 + swing,
            1.0 + swing);
    }
}
