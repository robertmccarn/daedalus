using Systemic.Engine.State;

public sealed class PartyController
{
    private const int HistoryLimit = 24;
    private const int FollowerDelay = 2;

    private readonly GameWorld world;
    private readonly List<PartyMemberRuntime> members = new();
    private readonly Queue<PartyHistoryEntry> history = new();
    private long step;

    public string LeaderId { get; private set; } = string.Empty;
    public PartyFormationType Formation { get; private set; } = PartyFormationType.Column;
    public IReadOnlyList<PartyMemberRuntime> Members => members;
    public GridPosition LeaderPosition => GetLeaderRuntime().Position;

    public PartyController(GameWorld world) { this.world = world; }

    public void Initialize(ExpeditionState expedition, GridPosition leaderPosition, string? leaderId = null, PartyFormationType? formation = null)
    {
        members.Clear();
        history.Clear();
        step = 0;
        if (expedition.Party.Count == 0) throw new InvalidOperationException("An expedition must contain at least one party member.");

        LeaderId = leaderId ?? expedition.LeaderId;
        if (string.IsNullOrWhiteSpace(LeaderId) || expedition.Party.All(member => member.Id != LeaderId))
            LeaderId = expedition.Party[0].Id;

        Formation = formation ?? expedition.Formation;
        if (!world.IsWalkable(leaderPosition.X, leaderPosition.Y))
            leaderPosition = new GridPosition(world.SpawnX, world.SpawnY);

        for (int i = 0; i < expedition.Party.Count; i++)
        {
            PartyMember member = expedition.Party[i];
            members.Add(new PartyMemberRuntime
            {
                MemberId = member.Id,
                Position = leaderPosition,
                PreviousPosition = leaderPosition,
                Direction = CharacterDirection.Down,
                AnimationState = CharacterAnimationState.Idle
            });
        }

        ReformAt(leaderPosition);
        SyncExpedition(expedition);
    }

    public void Load(ExpeditionState expedition, GridPosition leaderPosition) =>
        Initialize(expedition, leaderPosition, expedition.LeaderId, expedition.Formation);

    public MoveResult TryMoveLeader(CharacterDirection direction, ExpeditionState expedition)
    {
        PartyMemberRuntime leader = GetLeaderRuntime();
        leader.Direction = direction;
        leader.AnimationState = CharacterAnimationState.Idle;
        leader.IsMoving = false;

        GridPosition target = Step(leader.Position, direction);
        if (!world.IsWalkable(target.X, target.Y)) return MoveResult.Blocked;
        if (world.GetEnemyAt(target.X, target.Y) != null) return MoveResult.Encounter;

        leader.PreviousPosition = leader.Position;
        leader.Position = target;
        leader.AnimationState = CharacterAnimationState.Walk;
        leader.IsMoving = true;
        leader.AnimationTick = 0;

        step++;
        history.Enqueue(new PartyHistoryEntry(step, target, direction));
        while (history.Count > HistoryLimit) history.Dequeue();

        ResolveFollowerMovement();
        SyncExpedition(expedition);
        return MoveResult.Moved;
    }

    public void ReformForFloor(ExpeditionState expedition, GridPosition leaderPosition)
    {
        history.Clear();
        step = 0;
        ReformAt(leaderPosition);
        SyncExpedition(expedition);
    }

    public void AdvanceAnimation()
    {
        foreach (PartyMemberRuntime member in members)
        {
            if (member.IsMoving)
            {
                member.AnimationTick++;
                member.IsMoving = false;
            }
            else
            {
                member.AnimationState = CharacterAnimationState.Idle;
                member.AnimationTick = 0;
            }
        }
    }

    public PartyMemberRuntime GetLeaderRuntime() => members.First(member => member.MemberId == LeaderId);
    public PartyMemberRuntime? GetRuntime(string memberId) => members.FirstOrDefault(runtime => runtime.MemberId == memberId);

    public IReadOnlyList<PartyRenderData> GetRenderData(ExpeditionState expedition) =>
        members.Select((runtime, index) =>
        {
            PartyMember? member = expedition.Party.FirstOrDefault(candidate => candidate.Id == runtime.MemberId);
            return new PartyRenderData(
                runtime.MemberId,
                member?.Name ?? runtime.MemberId,
                runtime.Position,
                runtime.Direction,
                runtime.AnimationState,
                runtime.AnimationTick,
                member?.SpriteId ?? string.Empty,
                index,
                runtime.MemberId == LeaderId);
        }).ToList();

    private void ReformAt(GridPosition leaderPosition)
    {
        PartyMemberRuntime leader = GetLeaderRuntime();
        leader.Position = leaderPosition;
        leader.PreviousPosition = leaderPosition;
        leader.AnimationState = CharacterAnimationState.Idle;
        leader.IsMoving = false;

        HashSet<GridPosition> occupied = new() { leaderPosition };
        IReadOnlyList<GridPosition> offsets = FormationOffsets.GetOffsets(Formation, leader.Direction, Math.Max(0, members.Count - 1));
        IReadOnlyList<PartyMemberRuntime> followers = GetFollowers();

        for (int i = 0; i < followers.Count; i++)
        {
            PartyMemberRuntime follower = followers[i];
            GridPosition desired = Add(leaderPosition, offsets[i]);
            GridPosition resolved = ResolvePosition(follower, desired, leaderPosition, occupied, new HashSet<GridPosition>(), leaderPosition);

            follower.Direction = leader.Direction;
            follower.Position = resolved;
            follower.PreviousPosition = resolved;
            follower.AnimationState = CharacterAnimationState.Idle;
            follower.IsMoving = false;
            follower.StallSteps = resolved == desired ? 0 : follower.StallSteps + 1;
            occupied.Add(resolved);
        }
    }

    private void ResolveFollowerMovement()
    {
        PartyMemberRuntime leader = GetLeaderRuntime();
        List<FollowerMovementIntent> intents = new();
        IReadOnlyList<PartyHistoryEntry> historySnapshot = history.ToArray();
        IReadOnlyList<GridPosition> offsets = FormationOffsets.GetOffsets(Formation, leader.Direction, Math.Max(0, members.Count - 1));
        IReadOnlyList<PartyMemberRuntime> followers = GetFollowers();

        for (int i = 0; i < followers.Count; i++)
        {
            PartyMemberRuntime follower = followers[i];
            GridPosition historicalLeader = GetHistoricalLeaderPosition(historySnapshot, i);
            GridPosition desired = Add(historicalLeader, offsets[i]);
            intents.Add(new FollowerMovementIntent(follower.MemberId, follower.Position, desired));
        }

        HashSet<GridPosition> occupied = new() { leader.Position };
        HashSet<GridPosition> reservedCurrentPositions = followers.Select(follower => follower.Position).Where(position => position != leader.Position).ToHashSet();

        foreach (FollowerMovementIntent intent in intents)
        {
            PartyMemberRuntime follower = members.First(member => member.MemberId == intent.MemberId);
            reservedCurrentPositions.Remove(follower.Position);

            GridPosition resolved = ResolvePosition(follower, intent.DesiredPosition, leader.Position, occupied, reservedCurrentPositions, intent.CurrentPosition);
            follower.Direction = leader.Direction;
            follower.PreviousPosition = follower.Position;
            follower.Position = resolved;
            follower.IsMoving = resolved != follower.PreviousPosition;
            follower.AnimationState = follower.IsMoving ? CharacterAnimationState.Walk : CharacterAnimationState.Idle;
            follower.AnimationTick = 0;
            follower.StallSteps = resolved == intent.DesiredPosition ? 0 : follower.StallSteps + 1;
            occupied.Add(resolved);
        }
    }

    private IReadOnlyList<PartyMemberRuntime> GetFollowers() => members.Where(member => member.MemberId != LeaderId).ToList();

    private GridPosition ResolvePosition(PartyMemberRuntime follower, GridPosition desired, GridPosition leaderPosition, HashSet<GridPosition> occupied, HashSet<GridPosition> reservedCurrentPositions, GridPosition fallbackOrigin)
    {
        foreach (GridPosition candidate in CandidatePositions(desired, fallbackOrigin))
        {
            if (candidate == leaderPosition || occupied.Contains(candidate) || reservedCurrentPositions.Contains(candidate)) continue;
            if (IsValidPartyPosition(candidate)) return candidate;
        }

        if (follower.Position != leaderPosition && !occupied.Contains(follower.Position) && IsValidPartyPosition(follower.Position))
            return follower.Position;

        return FindNearestValidPosition(fallbackOrigin, leaderPosition, occupied, reservedCurrentPositions);
    }

    private bool IsValidPartyPosition(GridPosition position) =>
        world.IsWalkable(position.X, position.Y) && world.GetEnemyAt(position.X, position.Y) == null;

    private GridPosition FindNearestValidPosition(GridPosition origin, GridPosition leaderPosition, HashSet<GridPosition> occupied, HashSet<GridPosition> reservedCurrentPositions)
    {
        const int maxRadius = 4;
        for (int radius = 1; radius <= maxRadius; radius++)
            foreach (GridPosition candidate in RingPositions(origin, radius))
            {
                if (candidate == leaderPosition || occupied.Contains(candidate) || reservedCurrentPositions.Contains(candidate)) continue;
                if (IsValidPartyPosition(candidate)) return candidate;
            }
        return origin;
    }

    private static IEnumerable<GridPosition> CandidatePositions(GridPosition desired, GridPosition fallbackOrigin)
    {
        yield return desired;
        yield return fallbackOrigin;
        foreach (GridPosition candidate in RingPositions(desired, 1)) yield return candidate;
        foreach (GridPosition candidate in RingPositions(fallbackOrigin, 1)) yield return candidate;
        foreach (GridPosition candidate in RingPositions(desired, 2)) yield return candidate;
    }

    private static IEnumerable<GridPosition> RingPositions(GridPosition center, int radius)
    {
        for (int dx = -radius; dx <= radius; dx++)
        {
            int dy = radius - Math.Abs(dx);
            yield return new GridPosition(center.X + dx, center.Y - dy);
            if (dy != 0) yield return new GridPosition(center.X + dx, center.Y + dy);
        }
    }

    private static GridPosition GetHistoricalLeaderPosition(IReadOnlyList<PartyHistoryEntry> entries, int followerSlot)
    {
        if (entries.Count == 0) return default;
        int delay = Math.Max(1, followerSlot * FollowerDelay);
        int index = Math.Max(0, entries.Count - delay);
        return entries[index].LeaderPosition;
    }

    private void SyncExpedition(ExpeditionState expedition)
    {
        expedition.LeaderId = LeaderId;
        expedition.Formation = Formation;
        expedition.PlayerGridPosition = (LeaderPosition.X, LeaderPosition.Y);
    }

    private static GridPosition Step(GridPosition position, CharacterDirection direction) => direction switch
    {
        CharacterDirection.Up => new GridPosition(position.X, position.Y - 1),
        CharacterDirection.Down => new GridPosition(position.X, position.Y + 1),
        CharacterDirection.Left => new GridPosition(position.X - 1, position.Y),
        _ => new GridPosition(position.X + 1, position.Y)
    };

    private static GridPosition Add(GridPosition position, GridPosition offset) => new(position.X + offset.X, position.Y + offset.Y);
}

public enum PartyFormationType
{
    Column,
    Wedge,
    Line,
    Defensive
}

public static class FormationOffsets
{
    public static IReadOnlyList<GridPosition> GetOffsets(PartyFormationType formation, CharacterDirection direction, int count)
    {
        List<GridPosition> offsets = new();
        for (int i = 0; i < count; i++)
        {
            int rank = i / 2 + 1;
            int side = i % 2 == 0 ? -1 : 1;
            offsets.Add(GetOffset(formation, direction, rank, side, i));
        }
        return offsets;
    }

    private static GridPosition GetOffset(PartyFormationType formation, CharacterDirection direction, int rank, int side, int index)
    {
        (GridPosition forward, GridPosition right) = direction switch
        {
            CharacterDirection.Up => (new GridPosition(0, -1), new GridPosition(1, 0)),
            CharacterDirection.Down => (new GridPosition(0, 1), new GridPosition(-1, 0)),
            CharacterDirection.Left => (new GridPosition(-1, 0), new GridPosition(0, -1)),
            _ => (new GridPosition(1, 0), new GridPosition(0, 1))
        };
        GridPosition back = new(-forward.X, -forward.Y);

        return formation switch
        {
            PartyFormationType.Wedge => Add(Multiply(back, rank), Multiply(right, side * rank)),
            PartyFormationType.Line => Multiply(right, index + 1),
            PartyFormationType.Defensive => Add(Multiply(back, rank), Multiply(right, side * Math.Min(rank, 1))),
            _ => Multiply(back, index + 1)
        };
    }

    private static GridPosition Add(GridPosition a, GridPosition b) => new(a.X + b.X, a.Y + b.Y);
    private static GridPosition Multiply(GridPosition a, int value) => new(a.X * value, a.Y * value);
}
