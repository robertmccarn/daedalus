using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

public sealed class GameplayRecorder
{
    private readonly List<GameplayEvent> events = new();
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public IReadOnlyList<GameplayEvent> Events => events;

    public void Record(GameplayEvent gameplayEvent) => events.Add(gameplayEvent);

    public void Record(
        GameplayEventType type,
        int floor,
        int turn,
        int x,
        int y,
        string? actorId = null,
        string? targetId = null,
        int value = 0,
        string? context = null) =>
        Record(new GameplayEvent(
            type,
            floor,
            turn,
            x,
            y,
            actorId,
            targetId,
            value,
            context));

    public IReadOnlyDictionary<GameplayEventType, int> CountByType() =>
        events
            .GroupBy(item => item.Type)
            .ToDictionary(group => group.Key, group => group.Count());

    public string ToJsonLines()
    {
        StringBuilder builder = new();

        foreach (GameplayEvent item in events)
            builder
                .AppendLine(JsonSerializer.Serialize(item, JsonOptions));

        return builder.ToString();
    }

    public void Clear() => events.Clear();
}
