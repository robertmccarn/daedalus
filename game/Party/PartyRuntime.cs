public enum CharacterDirection
{
    Down,
    Up,
    Left,
    Right
}

public enum CharacterAnimationState
{
    Idle,
    Walk,
    Attack,
    Hurt,
    Contextual
}

public readonly record struct PartyHistoryEntry(
    long Step,
    GridPosition LeaderPosition,
    CharacterDirection Direction);

public sealed class PartyMemberRuntime
{
    public string MemberId { get; init; } = string.Empty;
    public GridPosition Position { get; set; }
    public GridPosition PreviousPosition { get; set; }
    public CharacterDirection Direction { get; set; } = CharacterDirection.Down;
    public CharacterAnimationState AnimationState { get; set; } = CharacterAnimationState.Idle;
    public int AnimationTick { get; set; }
    public bool IsMoving { get; set; }
    public bool IsDefeated { get; set; }
    public int StallSteps { get; set; }
}

public readonly record struct FollowerMovementIntent(
    string MemberId,
    GridPosition CurrentPosition,
    GridPosition DesiredPosition);

public readonly record struct PartyRenderData(
    string MemberId,
    string Name,
    GridPosition Position,
    CharacterDirection Direction,
    CharacterAnimationState AnimationState,
    int AnimationTick,
    string SpriteId,
    int PartySlot,
    bool IsLeader);
