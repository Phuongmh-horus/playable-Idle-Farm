public static class PlayablePlatformAdapterFactory
{
    public static IPlayablePlatformAdapter Create(CTAConfig config)
    {
        var useLuna = config == null || config.useLunaAdapter;
        if (useLuna) return new LunaPlayableAdapter();
        return new NoOpPlayableAdapter();
    }
}
