internal sealed class TikTokNetworkMarkupBuilder : INetworkMarkupBuilder
{
    public PlayableAdNetwork Network => PlayableAdNetwork.TikTok;

    public string BuildHeadMarkup(string escapedOrientation)
    {
        return NetworkMarkupTemplates.BuildTikTokHeadMarkup();
    }

    public string BuildTailScript()
    {
        return NetworkMarkupTemplates.BuildTikTokTailScript();
    }
}
