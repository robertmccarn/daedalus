using Systemic.Engine.State;

public sealed record ExpeditionEvent(
    string Id,
    string Title,
    string Description,
    int MoraleDelta,
    int GoldDelta,
    string Flag);

public static class EventSystem
{
    public static IReadOnlyList<ExpeditionEvent> DefaultEvents { get; } =
        new[]
        {
            new ExpeditionEvent("echoes", "Echoes in the Stone", "A broken inscription describes a place deeper below.", 2, 0, "heard-the-echoes"),
            new ExpeditionEvent("cache", "Forgotten Cache", "Someone hid supplies here long before your expedition.", 4, 8, "found-forgotten-cache"),
            new ExpeditionEvent("warning", "The Warning", "A fresh mark suggests that something else is moving through the ruins.", -3, 0, "saw-warning")
        };

    public static ExpeditionEvent Roll(int seed, int floor, int turn)
    {
        int index = Math.Abs(HashCode.Combine(seed, floor, turn)) % DefaultEvents.Count;
        return DefaultEvents[index];
    }

    public static void Apply(
        CampaignState campaign,
        ExpeditionState expedition,
        ExpeditionEvent expeditionEvent)
    {
        foreach (PartyMember member in expedition.Party)
            member.Morale = Math.Clamp(member.Morale + expeditionEvent.MoraleDelta, 0, 100);

        expedition.CarriedGold += expeditionEvent.GoldDelta;
        campaign.Flags.Add(expeditionEvent.Flag);
    }
}
