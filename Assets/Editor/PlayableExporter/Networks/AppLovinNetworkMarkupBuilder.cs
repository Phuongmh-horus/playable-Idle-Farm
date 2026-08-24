internal sealed class AppLovinNetworkMarkupBuilder : INetworkMarkupBuilder
{
    public PlayableAdNetwork Network => PlayableAdNetwork.AppLovin;

    public string BuildHeadMarkup(string escapedOrientation)
    {
        return string.Empty;
    }

    public string BuildTailScript()
    {
        return NetworkMarkupTemplates.BuildMraidPlayableTailScript();
    }
}
