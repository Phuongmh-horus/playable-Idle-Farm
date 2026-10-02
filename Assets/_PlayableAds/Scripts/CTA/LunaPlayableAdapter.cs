public sealed class LunaPlayableAdapter : IPlayablePlatformAdapter
{
    private bool _started;
    private bool _ended;

    public void GameStarted()
    {
        if (_started) return;
        _started = true;

        Luna.Unity.LifeCycle.GameStarted();
    }

    public void GameEnded()
    {
        if (_ended) return;
        _ended = true;

        Luna.Unity.LifeCycle.GameEnded();
    }

    public void InstallFullGame()
    {
        Luna.Unity.Playable.InstallFullGame();
    }
}

