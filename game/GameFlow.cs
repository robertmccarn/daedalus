public sealed class GameFlow
{
    public GameState State { get; private set; }

    public GameFlow(GameState initialState)
    {
        State = initialState;
    }

    public bool CanTransitionTo(GameState nextState)
    {
        if (nextState == State)
            return true;

        return (State, nextState) switch
        {
            (GameState.Campaign, GameState.Exploration) => true,

            (GameState.Exploration, GameState.Battle) => true,
            (GameState.Exploration, GameState.ExtractionResults) => true,

            (GameState.Battle, GameState.Exploration) => true,
            (GameState.Battle, GameState.GameOver) => true,

            (GameState.ExtractionResults, GameState.Campaign) => true,
            (GameState.ExtractionResults, GameState.Exploration) => true,

            (GameState.GameOver, GameState.Exploration) => true,

            _ => false
        };
    }

    public void TransitionTo(GameState nextState)
    {
        if (!CanTransitionTo(nextState))
            throw new InvalidOperationException(
                $"Invalid game-flow transition: {State} -> {nextState}.");

        State = nextState;
    }
}
