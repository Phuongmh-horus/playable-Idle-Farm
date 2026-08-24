internal sealed class FacebookNetworkMarkupBuilder : INetworkMarkupBuilder
{
    public PlayableAdNetwork Network => PlayableAdNetwork.Facebook;

    public string BuildHeadMarkup(string escapedOrientation)
    {
        return string.Empty;
    }

    public string BuildTailScript()
    {
        return NetworkMarkupTemplates.BuildFacebookPlayableTailScript();
    }
}
