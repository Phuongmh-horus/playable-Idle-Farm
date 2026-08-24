internal static class NetworkMarkupBuilderFactory
{
    private static readonly INetworkMarkupBuilder[] Builders =
    {
        new GoogleAdsNetworkMarkupBuilder(),
        new MintegralNetworkMarkupBuilder(),
        new AppLovinNetworkMarkupBuilder(),
        new FacebookNetworkMarkupBuilder(),
        new IronSourceNetworkMarkupBuilder(),
        new MolocoNetworkMarkupBuilder(),
        new TikTokNetworkMarkupBuilder(),
        new UnityNetworkMarkupBuilder(),
        new VungleNetworkMarkupBuilder()
    };

    public static INetworkMarkupBuilder Get(PlayableAdNetwork network)
    {
        for (var index = 0; index < Builders.Length; index++)
        {
            var builder = Builders[index];
            if (builder.Network == network)
            {
                return builder;
            }
        }

        return null;
    }
}
