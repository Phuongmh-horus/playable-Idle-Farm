internal sealed class GoogleAdsNetworkMarkupBuilder : INetworkMarkupBuilder
{
    public PlayableAdNetwork Network => PlayableAdNetwork.GoogleAds;

    public string BuildHeadMarkup(string escapedOrientation)
    {
        return string.Concat(
            "<meta name=\"ad.orientation\" content=\"",
            escapedOrientation,
            "\"><script src=\"https://tpc.googlesyndication.com/pagead/gadgets/html5/api/exitapi.js\"></script>");
    }

    public string BuildTailScript()
    {
        return "<script>window.addEventListener('luna:build',function(){window.pi&&window.pi.logLoaded&&window.pi.logLoaded();window.dispatchEvent(new Event('luna:start'))});window.addEventListener('luna:build',function(){var bind=function(){if(!window.Luna||!Luna.Unity||!Luna.Unity.Playable){return}Luna.Unity.Playable.InstallFullGame=function(){if(window.pi&&window.pi.logCta){window.pi.logCta()}if(window.ExitApi&&typeof window.ExitApi.exit==='function'){window.ExitApi.exit()}}};window.Bridge&&typeof Bridge.ready==='function'?Bridge.ready(bind):bind()});</script>";
    }
}
