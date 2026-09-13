public enum EnemyAnimationState
{
    Idle,
    Attack,
    Death
}

public class IsometricEnemyAnimator
{
    private const int FrameDurationMilliseconds = 140;

    public EnemyAnimationState State { get; private set; } = EnemyAnimationState.Idle;
    public int CurrentFrame { get; private set; }
    public bool IsFinished { get; private set; }

    private int elapsedMilliseconds;

    public void SetState(EnemyAnimationState state)
    {
        if (State == state && !IsFinished)
        {
            return;
        }

        State = state;
        CurrentFrame = GetFrameRange().Start;
        elapsedMilliseconds = 0;
        IsFinished = false;
    }

    public void Update(int elapsed)
    {
        if (IsFinished)
        {
            return;
        }

        elapsedMilliseconds += elapsed;
        if (elapsedMilliseconds < FrameDurationMilliseconds)
        {
            return;
        }

        elapsedMilliseconds = 0;
        (int start, int end) = GetFrameRange();

        if (CurrentFrame < end)
        {
            CurrentFrame++;
        }
        else if (State == EnemyAnimationState.Death)
        {
            IsFinished = true;
        }
        else
        {
            CurrentFrame = start;
        }
    }

    public AtlasRegion GetFrame(AtlasSlicer atlas, AtlasUnit unit, AtlasDirection direction)
    {
        return atlas.GetEnemyAnimationRegion(unit, direction, CurrentFrame);
    }

    private (int Start, int End) GetFrameRange()
    {
        return State switch
        {
            EnemyAnimationState.Idle => (0, 3),
            EnemyAnimationState.Attack => (4, 5),
            EnemyAnimationState.Death => (6, 7),
            _ => (0, 3)
        };
    }
}
