internal sealed class MolocoNetworkMarkupBuilder : INetworkMarkupBuilder
{
    public PlayableAdNetwork Network => PlayableAdNetwork.Moloco;

    public string BuildHeadMarkup(string escapedOrientation)
    {
        return string.Empty;
    }

    public string BuildTailScript()
    {
        return NetworkMarkupTemplates.BuildFacebookPlayableTailScript();
    }
}
