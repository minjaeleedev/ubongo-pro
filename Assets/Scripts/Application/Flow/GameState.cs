namespace Ubongo.Application.Flow
{
    /// <summary>
    /// Application-level flow state for UI and round orchestration.
    /// </summary>
    public enum GameState
    {
        Menu,
        DifficultySelect,
        RoundStarting,
        Playing,
        Paused,
        RoundComplete,
        RoundFailed,
        SecondChance,
        GameComplete,
        Tiebreaker,
        TiebreakerComplete,
        GameOver
    }
}
