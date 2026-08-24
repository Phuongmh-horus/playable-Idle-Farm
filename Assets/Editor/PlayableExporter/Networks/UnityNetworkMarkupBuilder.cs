internal sealed class UnityNetworkMarkupBuilder : INetworkMarkupBuilder
{
    public PlayableAdNetwork Network => PlayableAdNetwork.Unity;

    public string BuildHeadMarkup(string escapedOrientation)
    {
        return string.Empty;
    }

    public string BuildTailScript()
    {
        return NetworkMarkupTemplates.BuildMraidPlayableTailScript();
    }
}
